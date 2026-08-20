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

        var pendingLinks = await dbContext.TicketLinks
            .Include(x => x.Incident)
                .ThenInclude(x => x!.ServerRoom)
            .Include(x => x.Incident)
                .ThenInclude(x => x!.ShellyDevice)
            .Include(x => x.Incident)
                .ThenInclude(x => x!.MonitoredDevice)
            .Where(x => x.SyncStatus == TicketSyncStatus.PendingCreate || x.SyncStatus == TicketSyncStatus.PendingComment)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var ticketLink in pendingLinks)
        {
            try
            {
                if (ticketLink.Incident is null)
                {
                    ticketLink.SyncStatus = TicketSyncStatus.Failed;
                    ticketLink.LastErrorMessage = "Missing related incident.";
                    ticketLink.LastSyncAttemptAtUtc = DateTime.UtcNow;
                    continue;
                }

                if (ticketLink.SyncStatus == TicketSyncStatus.PendingCreate)
                {
                    var created = await redmineClient.CreateIssueAsync(ticketLink.Incident, cancellationToken);
                    ticketLink.ExternalTicketId = created.ExternalTicketId;
                    ticketLink.ExternalTicketUrl = created.ExternalTicketUrl;
                    ticketLink.CreatedInExternalSystemAtUtc = DateTime.UtcNow;
                    ticketLink.LastSyncAttemptAtUtc = DateTime.UtcNow;

                    if (ticketLink.Incident.Status == IncidentStatus.Resolved)
                    {
                        var resolutionComment = BuildResolutionComment(ticketLink);
                        await redmineClient.AddCommentAsync(ticketLink.ExternalTicketId, resolutionComment, cancellationToken);
                        ticketLink.SyncStatus = TicketSyncStatus.Commented;
                        ticketLink.LastCommentedAtUtc = DateTime.UtcNow;
                        ticketLink.LastErrorMessage = string.Empty;
                    }
                    else
                    {
                        ticketLink.SyncStatus = TicketSyncStatus.Created;
                        ticketLink.LastErrorMessage = string.Empty;
                    }
                }
                else if (ticketLink.SyncStatus == TicketSyncStatus.PendingComment)
                {
                    if (string.IsNullOrWhiteSpace(ticketLink.ExternalTicketId))
                    {
                        ticketLink.SyncStatus = TicketSyncStatus.PendingCreate;
                        ticketLink.LastSyncAttemptAtUtc = DateTime.UtcNow;
                    }
                    else
                    {
                        var comment = BuildResolutionComment(ticketLink);

                        await redmineClient.AddCommentAsync(ticketLink.ExternalTicketId, comment, cancellationToken);
                        ticketLink.SyncStatus = TicketSyncStatus.Commented;
                        ticketLink.LastCommentedAtUtc = DateTime.UtcNow;
                        ticketLink.LastSyncAttemptAtUtc = DateTime.UtcNow;
                        ticketLink.LastErrorMessage = string.Empty;
                    }
                }
            }
            catch (Exception exception)
            {
                ticketLink.SyncStatus = TicketSyncStatus.Failed;
                ticketLink.LastErrorMessage = exception.Message;
                ticketLink.LastSyncAttemptAtUtc = DateTime.UtcNow;
                logger.LogError(exception, "Failed to synchronize incident {IncidentId} with Redmine.", ticketLink.IncidentId);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string BuildResolutionComment(TicketLink ticketLink)
    {
        if (!string.IsNullOrWhiteSpace(ticketLink.LastErrorMessage))
        {
            return ticketLink.LastErrorMessage;
        }

        var resolvedAt = ticketLink.Incident?.ResolvedAtUtc ?? DateTime.UtcNow;
        return $"Condition cleared at {resolvedAt:O}.";
    }
}
