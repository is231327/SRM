using Microsoft.EntityFrameworkCore;
using SRMShared.Entities;

namespace SRMAuth.Data;

public class SrmAuthDbContext(DbContextOptions<SrmAuthDbContext> options) : DbContext(options)
{
    public DbSet<AuthUser> Users => Set<AuthUser>();
    public DbSet<AuthRole> Roles => Set<AuthRole>();
    public DbSet<AuthUserRole> UserRoles => Set<AuthUserRole>();
    public DbSet<CustomerUser> CustomerUsers => Set<CustomerUser>();
    public DbSet<AgentCredential> AgentCredentials => Set<AgentCredential>();
    public DbSet<SecurityAuditRecord> SecurityAuditRecords => Set<SecurityAuditRecord>();
    public DbSet<MfaRecoveryCode> MfaRecoveryCodes => Set<MfaRecoveryCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuthUser>(entity =>
        {
            entity.Property(x => x.Username).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.FirstName).HasMaxLength(200);
            entity.Property(x => x.LastName).HasMaxLength(200);
            entity.Property(x => x.PhoneNumber).HasMaxLength(100);
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.MfaSecretProtected).HasMaxLength(2000);
        });

        modelBuilder.Entity<AuthRole>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<AuthUserRole>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.RoleId });

            entity.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerUser>(entity =>
        {
            entity.HasIndex(x => x.UserId).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.CustomerUsers)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

        });

        modelBuilder.Entity<MfaRecoveryCode>(entity =>
        {
            entity.Property(x => x.CodeHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.CodeHash }).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany(x => x.MfaRecoveryCodes)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentCredential>(entity =>
        {
            entity.Property(x => x.ClientIdentifier).HasMaxLength(200).IsRequired();
            entity.Property(x => x.SecretHash).HasMaxLength(1000).IsRequired();
            entity.HasIndex(x => x.ClientIdentifier).IsUnique();
        });

        ConfigureSecurityAuditRecord(modelBuilder.Entity<SecurityAuditRecord>());
    }

    private static void ConfigureSecurityAuditRecord(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<SecurityAuditRecord> entity)
    {
        entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Outcome).HasMaxLength(32).IsRequired();
        entity.Property(x => x.ActorIdentifier).HasMaxLength(200);
        entity.Property(x => x.SourceAddress).HasMaxLength(128);
        entity.Property(x => x.TargetType).HasMaxLength(100);
        entity.Property(x => x.Description).HasMaxLength(1000);
        entity.HasIndex(x => x.OccurredAtUtc);
        entity.HasIndex(x => x.ActorId);
    }
}
