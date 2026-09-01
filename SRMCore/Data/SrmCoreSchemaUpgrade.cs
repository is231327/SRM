using Microsoft.EntityFrameworkCore;

namespace SRMCore.Data;

public static class SrmCoreSchemaUpgrade
{
    public static void Apply(SrmCoreDbContext dbContext)
    {
        dbContext.Database.ExecuteSqlRaw("""
            IF COL_LENGTH('dbo.TicketLinks', 'ExternalStatusName') IS NULL
            BEGIN
                ALTER TABLE [dbo].[TicketLinks]
                ADD [ExternalStatusName] nvarchar(64) NOT NULL
                    CONSTRAINT [DF_TicketLinks_ExternalStatusName] DEFAULT N'';
            END;

            IF COL_LENGTH('dbo.TicketLinks', 'ExternalPriorityName') IS NULL
            BEGIN
                ALTER TABLE [dbo].[TicketLinks]
                ADD [ExternalPriorityName] nvarchar(64) NOT NULL
                    CONSTRAINT [DF_TicketLinks_ExternalPriorityName] DEFAULT N'';
            END;

            IF COL_LENGTH('dbo.TicketLinks', 'ExternalDataSynchronizedAtUtc') IS NULL
            BEGIN
                ALTER TABLE [dbo].[TicketLinks]
                ADD [ExternalDataSynchronizedAtUtc] datetime2 NULL;
            END;

            IF COL_LENGTH('dbo.TicketLinks', 'PendingComment') IS NULL
            BEGIN
                ALTER TABLE [dbo].[TicketLinks]
                ADD [PendingComment] nvarchar(max) NOT NULL
                    CONSTRAINT [DF_TicketLinks_PendingComment] DEFAULT N'';
            END;

            IF COL_LENGTH('dbo.TicketLinks', 'PriorityUpdatePending') IS NULL
            BEGIN
                ALTER TABLE [dbo].[TicketLinks]
                ADD [PriorityUpdatePending] bit NOT NULL
                    CONSTRAINT [DF_TicketLinks_PriorityUpdatePending] DEFAULT 0;
            END;

            IF COL_LENGTH('dbo.TicketLinks', 'SyncAttemptCount') IS NULL
            BEGIN
                ALTER TABLE [dbo].[TicketLinks]
                ADD [SyncAttemptCount] int NOT NULL
                    CONSTRAINT [DF_TicketLinks_SyncAttemptCount] DEFAULT 0;
            END;

            IF COL_LENGTH('dbo.TicketLinks', 'NextSyncAttemptAtUtc') IS NULL
            BEGIN
                ALTER TABLE [dbo].[TicketLinks]
                ADD [NextSyncAttemptAtUtc] datetime2 NULL;
            END;

            -- Migrate the former five-state enum to PendingCreate (1), Created (2), Error (3).
            UPDATE [dbo].[TicketLinks]
            SET [SyncStatus] = CASE
                WHEN [ExternalTicketId] <> N'' THEN 2
                WHEN [SyncStatus] = 5 THEN 3
                ELSE 1
            END
            WHERE [SyncStatus] NOT IN (1, 2, 3)
               OR ([SyncStatus] = 3 AND [ExternalTicketId] <> N'');
            """);
    }
}
