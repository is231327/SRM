using Microsoft.EntityFrameworkCore;
using SRMShared.Entities;

namespace SRMCore.Data;

public class AuthTokenStateDbContext(DbContextOptions<AuthTokenStateDbContext> options) : DbContext(options)
{
    public DbSet<RevokedAccessToken> RevokedAccessTokens => Set<RevokedAccessToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RevokedAccessToken>(entity =>
        {
            entity.ToTable("RevokedAccessTokens");
            entity.Property(x => x.TokenJti).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500);
            entity.HasIndex(x => x.TokenJti).IsUnique();
        });
    }
}
