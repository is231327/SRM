using Microsoft.EntityFrameworkCore;
using SRMAuth.Data;
using SRMIntegrationTests.TestHelpers;

namespace SRMIntegrationTests.Data;

[TestFixture]
public class SrmAuthSchemaUpgradeIntegrationTests
{
    [Test]
    public void Apply_ShouldAddMfaSchemaToExistingAuthDatabaseAndRemainIdempotent()
    {
        using var context = AuthSqlServerDbContextFactory.CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.Database.ExecuteSqlRaw("""
            DROP TABLE [dbo].[MfaRecoveryCodes];
            ALTER TABLE [dbo].[Users] DROP COLUMN [MfaLastUsedTimeStep], [MfaSecretProtected], [MfaEnabled];
            """);

        Assert.DoesNotThrow(() =>
        {
            SrmAuthSchemaUpgrade.Apply(context);
            SrmAuthSchemaUpgrade.Apply(context);
        });

        var connection = context.Database.GetDbConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CASE WHEN
                COL_LENGTH('dbo.Users', 'MfaEnabled') IS NOT NULL AND
                COL_LENGTH('dbo.Users', 'MfaSecretProtected') IS NOT NULL AND
                COL_LENGTH('dbo.Users', 'MfaLastUsedTimeStep') IS NOT NULL AND
                OBJECT_ID(N'dbo.MfaRecoveryCodes', N'U') IS NOT NULL
            THEN 1 ELSE 0 END
            """;
        Assert.That(Convert.ToInt32(command.ExecuteScalar()), Is.EqualTo(1));
    }
}
