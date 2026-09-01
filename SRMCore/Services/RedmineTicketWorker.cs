using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SRMCore.Configuration;
using SRMCore.Data;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class RedmineTicketWorker(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RedmineOptions> options,
    ILogger<RedmineTicketWorker> logger) : BackgroundService
{
    private static readonly TimeSpan MaximumRetryDelay = TimeSpan.FromMinutes(15);
    private readonly RedmineOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Redmine ticket worker is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCycleAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Redmine ticket worker cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, _options.PollIntervalSeconds)), stoppingToken);
        }
    }

    private async Task ProcessCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SrmCoreDbContext>();
        var redmineClient = scope.ServiceProvider.GetRequiredService<IRedmineTicketingClient>();

        await RepairPublicTicketUrlsAsync(dbContext, cancellationToken);
        await RefreshExternalTicketDataAsync(dbContext, redmineClient, cancellationToken);

        var pendingLinks = await dbContext.TicketLinks
            .Include(x => x.Incident)
                .ThenInclude(x => x!.ServerRoom)
                    .ThenInclude(x => x!.Customer)
            .Include(x => x.Incident)
                .ThenInclude(x => x!.ShellyDevice)
            .Include(x => x.Incident)
                .ThenInclude(x => x!.MonitoredDevice)
            .Where(x => (!x.NextSyncAttemptAtUtc.HasValue || x.NextSyncAttemptAtUtc <= DateTime.UtcNow)
                && ((x.ExternalTicketId == string.Empty
                        && (x.SyncStatus == TicketSyncStatus.PendingCreate || x.SyncStatus == TicketSyncStatus.Error))
                    || (x.ExternalTicketId != string.Empty
                        && (x.PriorityUpdatePending || x.PendingComment != string.Empty))))
            .OrderBy(x => x.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var ticketLink in pendingLinks)
        {
            try
            {
                if (ticketLink.Incident is null)
                {
                    ticketLink.SyncStatus = string.IsNullOrWhiteSpace(ticketLink.ExternalTicketId)
                        ? TicketSyncStatus.Error
                        : TicketSyncStatus.Created;
                    ticketLink.LastErrorMessage = "Missing related incident.";
                    ticketLink.LastSyncAttemptAtUtc = DateTime.UtcNow;
                    ticketLink.SyncAttemptCount++;
                    ticketLink.NextSyncAttemptAtUtc = DateTime.UtcNow.Add(CalculateRetryDelay(ticketLink.SyncAttemptCount));
                    continue;
                }

                var shouldCreate = string.IsNullOrWhiteSpace(ticketLink.ExternalTicketId);
                if (shouldCreate)
                {
                    var created = await redmineClient.CreateIssueAsync(ticketLink.Incident, cancellationToken);
                    ticketLink.ExternalTicketId = created.ExternalTicketId;
                    ticketLink.ExternalTicketUrl = created.ExternalTicketUrl;
                    ticketLink.CreatedInExternalSystemAtUtc = DateTime.UtcNow;
                    ticketLink.LastSyncAttemptAtUtc = DateTime.UtcNow;
                    ticketLink.SyncAttemptCount = 0;
                    ticketLink.NextSyncAttemptAtUtc = null;
                    ticketLink.PriorityUpdatePending = false;
                    ticketLink.SyncStatus = TicketSyncStatus.Created;
                    ticketLink.ExternalDataSynchronizedAtUtc = null;

                    if (!string.IsNullOrWhiteSpace(ticketLink.PendingComment)
                        || ticketLink.Incident.Status == IncidentStatus.Resolved)
                    {
                        var resolutionComment = BuildResolutionComment(ticketLink);
                        await redmineClient.AddCommentAsync(ticketLink.ExternalTicketId, resolutionComment, cancellationToken);
                        ticketLink.LastCommentedAtUtc = DateTime.UtcNow;
                        ticketLink.PendingComment = string.Empty;
                    }

                    ticketLink.LastErrorMessage = string.Empty;
                }
                else
                {
                    if (ticketLink.PriorityUpdatePending)
                    {
                        await redmineClient.UpdatePriorityAsync(
                            ticketLink.ExternalTicketId,
                            ticketLink.Incident.Severity,
                            cancellationToken);
                        ticketLink.PriorityUpdatePending = false;
                        ticketLink.ExternalDataSynchronizedAtUtc = null;
                    }

                    if (!string.IsNullOrWhiteSpace(ticketLink.PendingComment))
                    {
                        var comment = BuildResolutionComment(ticketLink);

                        await redmineClient.AddCommentAsync(ticketLink.ExternalTicketId, comment, cancellationToken);
                        ticketLink.LastCommentedAtUtc = DateTime.UtcNow;
                        ticketLink.PendingComment = string.Empty;
                    }

                    ticketLink.SyncStatus = TicketSyncStatus.Created;
                    ticketLink.LastSyncAttemptAtUtc = DateTime.UtcNow;
                    ticketLink.SyncAttemptCount = 0;
                    ticketLink.NextSyncAttemptAtUtc = null;
                    ticketLink.LastErrorMessage = string.Empty;
                }
            }
            catch (Exception exception)
            {
                ticketLink.SyncStatus = string.IsNullOrWhiteSpace(ticketLink.ExternalTicketId)
                    ? TicketSyncStatus.Error
                    : TicketSyncStatus.Created;
                ticketLink.LastErrorMessage = exception.Message;
                ticketLink.LastSyncAttemptAtUtc = DateTime.UtcNow;
                ticketLink.SyncAttemptCount++;
                ticketLink.NextSyncAttemptAtUtc = DateTime.UtcNow.Add(CalculateRetryDelay(ticketLink.SyncAttemptCount));
                logger.LogError(exception, "Failed to synchronize incident {IncidentId} with Redmine.", ticketLink.IncidentId);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RepairPublicTicketUrlsAsync(SrmCoreDbContext dbContext, CancellationToken cancellationToken)
    {
        var publicIssuePrefix = _options.BuildPublicIssueUrl(string.Empty);
        var ticketLinks = await dbContext.TicketLinks
            .Include(x => x.Incident)
            .Where(x => x.ExternalTicketId != string.Empty
                && !x.ExternalTicketUrl.StartsWith(publicIssuePrefix))
            .OrderBy(x => x.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var ticketLink in ticketLinks)
        {
            RedmineTicketSynchronization.RepairPublicUrl(ticketLink, _options);
        }
    }

    private async Task RefreshExternalTicketDataAsync(
        SrmCoreDbContext dbContext,
        IRedmineTicketingClient redmineClient,
        CancellationToken cancellationToken)
    {
        var refreshBefore = DateTime.UtcNow.AddSeconds(-Math.Max(15, _options.IssueRefreshIntervalSeconds));
        var ticketLinks = await dbContext.TicketLinks
            .Include(x => x.Incident)
            .Where(x => x.ExternalTicketId != string.Empty
                && (!x.ExternalDataSynchronizedAtUtc.HasValue || x.ExternalDataSynchronizedAtUtc <= refreshBefore))
            .OrderBy(x => x.ExternalDataSynchronizedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var ticketLink in ticketLinks)
        {
            try
            {
                var details = await redmineClient.GetIssueAsync(ticketLink.ExternalTicketId, cancellationToken);
                RedmineTicketSynchronization.ApplyIssueDetails(ticketLink, details, DateTime.UtcNow);
            }
            catch (Exception exception)
            {
                ticketLink.ExternalDataSynchronizedAtUtc = DateTime.UtcNow;
                logger.LogWarning(
                    exception,
                    "Could not refresh Redmine issue {ExternalTicketId} for incident {IncidentId}.",
                    ticketLink.ExternalTicketId,
                    ticketLink.IncidentId);
            }
        }
    }

    private static TimeSpan CalculateRetryDelay(int attemptCount)
    {
        var exponent = Math.Min(Math.Max(0, attemptCount - 1), 10);
        var delay = TimeSpan.FromSeconds(5 * Math.Pow(2, exponent));
        return delay <= MaximumRetryDelay ? delay : MaximumRetryDelay;
    }

    private static string BuildResolutionComment(TicketLink ticketLink)
    {
        if (!string.IsNullOrWhiteSpace(ticketLink.PendingComment))
        {
            return ticketLink.PendingComment;
        }

        var resolvedAt = ticketLink.Incident?.ResolvedAtUtc ?? DateTime.UtcNow;
        return $"Condition cleared at {resolvedAt:O}.";
    }
}
