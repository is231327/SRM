using Microsoft.EntityFrameworkCore;
using SRMCore.Data;
using SRMIntegrationTests.TestHelpers;
using SRMShared.Entities;

namespace SRMIntegrationTests.Data;

[TestFixture]
public class SrmCoreSchemaUpgradeIntegrationTests
{
    [SetUp]
    public void ResetDatabase()
    {
        using var context = SqlServerDbContextFactory.CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Test]
    public void Apply_ShouldAddMissingTicketRetryColumnsAndPreserveExistingLinks()
    {
        using var context = SqlServerDbContextFactory.CreateContext();
        var customer = new Customer { Name = "Legacy customer" };
        var serverRoom = new ServerRoom { Name = "Legacy room", Customer = customer };
        var incident = new Incident
        {
            ServerRoom = serverRoom,
            CorrelationKey = "legacy-incident",
            Summary = "Legacy incident"
        };
        var ticketLink = new TicketLink
        {
            Incident = incident,
            ProviderName = "Redmine",
            ExternalTicketId = "42"
        };
        context.TicketLinks.Add(ticketLink);
        context.SaveChanges();

        context.Database.ExecuteSqlRaw("""
            ALTER TABLE [dbo].[TicketLinks]
            DROP COLUMN [PendingComment], [SyncAttemptCount], [NextSyncAttemptAtUtc];
            """);

        SrmCoreSchemaUpgrade.Apply(context);
        context.ChangeTracker.Clear();

        var upgraded = context.TicketLinks.Single(x => x.Id == ticketLink.Id);
        Assert.Multiple(() =>
        {
            Assert.That(upgraded.PendingComment, Is.Empty);
            Assert.That(upgraded.SyncAttemptCount, Is.Zero);
            Assert.That(upgraded.NextSyncAttemptAtUtc, Is.Null);
            Assert.That(upgraded.ExternalTicketId, Is.EqualTo("42"));
        });
    }
}
