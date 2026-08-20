using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SRMCore.Configuration;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class RedmineTicketingClient : IRedmineTicketingClient
{
    private readonly HttpClient _httpClient;
    private readonly RedmineOptions _options;
    private int? _resolvedProjectId;

    public RedmineTicketingClient(HttpClient httpClient, IOptions<RedmineOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        EnsureConfigured();

        _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("X-Redmine-API-Key", _options.ApiKey);
    }

    public async Task<RedmineTicketCreateResult> CreateIssueAsync(Incident incident, CancellationToken cancellationToken = default)
    {
        var projectId = await ResolveProjectIdAsync(cancellationToken);

        var payload = new RedmineCreateIssueRequest
        {
            Issue = new RedmineCreateIssueBody
            {
                ProjectId = projectId,
                TrackerId = _options.TrackerId,
                StatusId = _options.StatusId,
                PriorityId = MapPriorityId(incident.Severity),
                Subject = incident.Summary,
                Description = incident.Description
            }
        };

        using var response = await _httpClient.PostAsJsonAsync("issues.json", payload, cancellationToken);
        await EnsureSuccessWithDetailsAsync(response, cancellationToken);

        var created = await response.Content.ReadFromJsonAsync<RedmineCreateIssueResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Redmine did not return a valid issue response.");

        return new RedmineTicketCreateResult
        {
            ExternalTicketId = created.Issue.Id.ToString(),
            ExternalTicketUrl = BuildIssueUrl(created.Issue.Id)
        };
    }

    public async Task AddCommentAsync(string externalTicketId, string comment, CancellationToken cancellationToken = default)
    {
        var payload = new RedmineUpdateIssueRequest
        {
            Issue = new RedmineUpdateIssueBody
            {
                Notes = comment
            }
        };

        using var response = await _httpClient.PutAsJsonAsync($"issues/{externalTicketId}.json", payload, cancellationToken);
        await EnsureSuccessWithDetailsAsync(response, cancellationToken);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("Redmine BaseUrl is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Redmine ApiKey is not configured.");
        }
    }

    private int MapPriorityId(IncidentSeverity severity)
    {
        return severity switch
        {
            IncidentSeverity.Warning => _options.WarningPriorityId,
            IncidentSeverity.Major => _options.MajorPriorityId,
            IncidentSeverity.Critical => _options.CriticalPriorityId,
            _ => _options.WarningPriorityId
        };
    }

    private string BuildIssueUrl(int issueId)
    {
        return $"{_options.BaseUrl.TrimEnd('/')}/issues/{issueId}";
    }

    private async Task<int> ResolveProjectIdAsync(CancellationToken cancellationToken)
    {
        if (_resolvedProjectId.HasValue)
        {
            return _resolvedProjectId.Value;
        }

        if (int.TryParse(_options.ProjectIdentifier, out var numericProjectId))
        {
            _resolvedProjectId = numericProjectId;
            return numericProjectId;
        }

        using var response = await _httpClient.GetAsync($"projects/{_options.ProjectIdentifier}.json", cancellationToken);
        await EnsureSuccessWithDetailsAsync(response, cancellationToken);

        var projectResponse = await response.Content.ReadFromJsonAsync<RedmineProjectResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Redmine did not return a valid project response.");

        _resolvedProjectId = projectResponse.Project.Id;
        return projectResponse.Project.Id;
    }

    private static async Task EnsureSuccessWithDetailsAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            response.EnsureSuccessStatusCode();
            return;
        }

        var message = TryExtractRedmineError(content);
        throw new InvalidOperationException(message ?? $"Redmine request failed with status {(int)response.StatusCode}: {content}");
    }

    private static string? TryExtractRedmineError(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Array)
            {
                var errors = errorsElement.EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();

                if (errors.Length > 0)
                {
                    return string.Join(" | ", errors!);
                }
            }

            if (root.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.String)
            {
                return errorElement.GetString();
            }
        }
        catch
        {
        }

        return null;
    }

    private sealed class RedmineCreateIssueRequest
    {
        [JsonPropertyName("issue")]
        public RedmineCreateIssueBody Issue { get; set; } = new();
    }

    private sealed class RedmineCreateIssueBody
    {
        [JsonPropertyName("project_id")]
        public int ProjectId { get; set; }

        [JsonPropertyName("tracker_id")]
        public int TrackerId { get; set; }

        [JsonPropertyName("priority_id")]
        public int PriorityId { get; set; }

        [JsonPropertyName("status_id")]
        public int StatusId { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }

    private sealed class RedmineCreateIssueResponse
    {
        [JsonPropertyName("issue")]
        public RedmineIssueReference Issue { get; set; } = new();
    }

    private sealed class RedmineIssueReference
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    private sealed class RedmineProjectResponse
    {
        [JsonPropertyName("project")]
        public RedmineProjectReference Project { get; set; } = new();
    }

    private sealed class RedmineProjectReference
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    private sealed class RedmineUpdateIssueRequest
    {
        [JsonPropertyName("issue")]
        public RedmineUpdateIssueBody Issue { get; set; } = new();
    }

    private sealed class RedmineUpdateIssueBody
    {
        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;
    }
}
