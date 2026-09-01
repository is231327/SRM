using SRMCore.Configuration;
using SRMCore.Services;
using SRMShared.Entities;

namespace SRMUnitTests.Services;

public class RedmineTicketSynchronizationTests
{
    [Test]
    public void RepairPublicUrl_ReplacesInternalContainerUrlWithConfiguredPublicUrl()
    {
        var ticketLink = new TicketLink
        {
            ExternalTicketId = "4",
            ExternalTicketUrl = "http://test-aca-srm-redmine/issues/4"
        };
        var options = new RedmineOptions
        {
            BaseUrl = "http://test-aca-srm-redmine",
            PublicBaseUrl = "https://test-aca-srm-redmine.example.azurecontainerapps.io"
        };

        var changed = RedmineTicketSynchronization.RepairPublicUrl(ticketLink, options);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(
                ticketLink.ExternalTicketUrl,
                Is.EqualTo("https://test-aca-srm-redmine.example.azurecontainerapps.io/issues/4"));
        });
    }

    [TestCase("New", IncidentStatus.New)]
    [TestCase("In Progress", IncidentStatus.InProgress)]
    [TestCase("Resolved", IncidentStatus.Resolved)]
    [TestCase("Feedback", IncidentStatus.Feedback)]
    [TestCase("Closed", IncidentStatus.Closed)]
    [TestCase("Rejected", IncidentStatus.Rejected)]
    public void ApplyIssueDetails_UpdatesTicketPriorityAndAllRedmineStatuses(
        string redmineStatus,
        IncidentStatus expectedIncidentStatus)
    {
        var incident = new Incident { Status = IncidentStatus.New };
        var ticketLink = new TicketLink { Incident = incident };
        var synchronizedAtUtc = new DateTime(2026, 9, 1, 9, 30, 0, DateTimeKind.Utc);

        RedmineTicketSynchronization.ApplyIssueDetails(
            ticketLink,
            new RedmineIssueDetails { StatusName = redmineStatus, PriorityName = "Immediate" },
            synchronizedAtUtc);

        Assert.Multiple(() =>
        {
            Assert.That(ticketLink.ExternalStatusName, Is.EqualTo(redmineStatus));
            Assert.That(ticketLink.ExternalPriorityName, Is.EqualTo("Immediate"));
            Assert.That(ticketLink.ExternalDataSynchronizedAtUtc, Is.EqualTo(synchronizedAtUtc));
            Assert.That(incident.Status, Is.EqualTo(expectedIncidentStatus));
            Assert.That(
                incident.ClosedAtUtc.HasValue,
                Is.EqualTo(expectedIncidentStatus is IncidentStatus.Closed or IncidentStatus.Rejected));
        });
    }
}
