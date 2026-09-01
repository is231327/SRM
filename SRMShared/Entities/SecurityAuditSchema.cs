namespace SRMShared.Entities;

public static class SecurityAuditSchema
{
    public const string CreateTableSql = """
        IF OBJECT_ID(N'[dbo].[SecurityAuditRecords]', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[SecurityAuditRecords] (
                [Id] uniqueidentifier NOT NULL,
                [OccurredAtUtc] datetime2 NOT NULL,
                [EventType] nvarchar(100) NOT NULL,
                [Outcome] nvarchar(32) NOT NULL,
                [ActorId] uniqueidentifier NULL,
                [ActorIdentifier] nvarchar(200) NOT NULL,
                [SourceAddress] nvarchar(128) NOT NULL,
                [TargetType] nvarchar(100) NOT NULL,
                [TargetId] uniqueidentifier NULL,
                [CustomerId] uniqueidentifier NULL,
                [Description] nvarchar(1000) NOT NULL,
                [CreatedAtUtc] datetime2 NOT NULL,
                [UpdatedAtUtc] datetime2 NOT NULL,
                CONSTRAINT [PK_SecurityAuditRecords] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_SecurityAuditRecords_OccurredAtUtc] ON [dbo].[SecurityAuditRecords] ([OccurredAtUtc]);
            CREATE INDEX [IX_SecurityAuditRecords_ActorId] ON [dbo].[SecurityAuditRecords] ([ActorId]);
        END;
        """;
}
