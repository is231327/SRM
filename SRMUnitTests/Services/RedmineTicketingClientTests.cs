using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using SRMCore.Configuration;
using SRMCore.Services;
using SRMShared.Entities;

namespace SRMUnitTests.Services;

public class RedmineTicketingClientTests
{
    [Test]
    public async Task GetIssueAsync_ReturnsRedmineStatusAndPriorityNames()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {"issue":{"id":2,"status":{"id":2,"name":"In Progress"},"priority":{"id":5,"name":"Immediate"}}}
            """));
        var client = CreateClient(handler);

        var result = await client.GetIssueAsync("2");

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusName, Is.EqualTo("In Progress"));
            Assert.That(result.PriorityName, Is.EqualTo("Immediate"));
        });
    }

    [TestCase(IncidentSeverity.Warning, 3)]
    [TestCase(IncidentSeverity.Major, 4)]
    [TestCase(IncidentSeverity.Critical, 5)]
    public async Task CreateIssueAsync_MapsSeverityToConfiguredRedminePriority(
        IncidentSeverity severity,
        int expectedPriorityId)
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{"issue":{"id":12}}"""));
        var client = CreateClient(handler);

        await client.CreateIssueAsync(new Incident
        {
            Severity = severity,
            Summary = "Test incident",
            Description = "Test description"
        });

        Assert.That(handler.LastRequestBody, Does.Contain($"\"priority_id\":{expectedPriorityId}"));
    }

    [TestCase(IncidentSeverity.Warning, 3)]
    [TestCase(IncidentSeverity.Major, 4)]
    [TestCase(IncidentSeverity.Critical, 5)]
    public async Task UpdatePriorityAsync_MapsSeverityToConfiguredRedminePriority(
        IncidentSeverity severity,
        int expectedPriorityId)
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(handler);

        await client.UpdatePriorityAsync("12", severity);

        Assert.That(handler.LastRequestBody, Does.Contain($"\"priority_id\":{expectedPriorityId}"));
    }

    private static RedmineTicketingClient CreateClient(HttpMessageHandler handler)
    {
        var options = Options.Create(new RedmineOptions
        {
            BaseUrl = "http://redmine/",
            PublicBaseUrl = "http://localhost:3000/",
            ApiKey = "test-key",
            ProjectIdentifier = "1",
            WarningPriorityId = 3,
            MajorPriorityId = 4,
            CriticalPriorityId = 5
        });

        return new RedmineTicketingClient(new HttpClient(handler), options);
    }

    private static HttpResponseMessage JsonResponse(string content)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        public string LastRequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return responseFactory(request);
        }
    }
}
