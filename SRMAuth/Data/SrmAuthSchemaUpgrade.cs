using Microsoft.EntityFrameworkCore;
using SRMShared.Entities;

namespace SRMAuth.Data;

public static class SrmAuthSchemaUpgrade
{
    public static void Apply(SrmAuthDbContext dbContext)
    {
        dbContext.Database.ExecuteSqlRaw(SecurityAuditSchema.CreateTableSql);
        dbContext.Database.ExecuteSqlRaw(MfaSchemaSql);
    }

    private const string MfaSchemaSql = """
        IF COL_LENGTH('dbo.Users', 'MfaEnabled') IS NULL
            ALTER TABLE [dbo].[Users] ADD [MfaEnabled] bit NOT NULL CONSTRAINT [DF_Users_MfaEnabled] DEFAULT 0;
        IF COL_LENGTH('dbo.Users', 'MfaSecretProtected') IS NULL
            ALTER TABLE [dbo].[Users] ADD [MfaSecretProtected] nvarchar(2000) NOT NULL CONSTRAINT [DF_Users_MfaSecretProtected] DEFAULT N'';
        IF COL_LENGTH('dbo.Users', 'MfaLastUsedTimeStep') IS NULL
            ALTER TABLE [dbo].[Users] ADD [MfaLastUsedTimeStep] bigint NULL;

        IF OBJECT_ID(N'[dbo].[MfaRecoveryCodes]', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[MfaRecoveryCodes] (
                [Id] uniqueidentifier NOT NULL,
                [UserId] uniqueidentifier NOT NULL,
                [CodeHash] nvarchar(64) NOT NULL,
                [UsedAtUtc] datetime2 NULL,
                [CreatedAtUtc] datetime2 NOT NULL,
                [UpdatedAtUtc] datetime2 NOT NULL,
                CONSTRAINT [PK_MfaRecoveryCodes] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_MfaRecoveryCodes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX [IX_MfaRecoveryCodes_UserId_CodeHash] ON [dbo].[MfaRecoveryCodes] ([UserId], [CodeHash]);
        END;
        """;
}
