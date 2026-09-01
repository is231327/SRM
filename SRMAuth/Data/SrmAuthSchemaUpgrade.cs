using Microsoft.EntityFrameworkCore;
using SRMShared.Entities;

namespace SRMAuth.Data;

public static class SrmAuthSchemaUpgrade
{
    public static void Apply(SrmAuthDbContext dbContext)
        => dbContext.Database.ExecuteSqlRaw(SecurityAuditSchema.CreateTableSql);
}
