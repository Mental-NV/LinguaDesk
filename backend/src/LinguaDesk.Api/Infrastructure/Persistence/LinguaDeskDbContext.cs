using LinguaDesk.Api.Features.Operations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LinguaDesk.Api.Infrastructure.Persistence;

public sealed class LinguaDeskDbContext(DbContextOptions<LinguaDeskDbContext> options)
    : IdentityUserContext<IdentityUser>(options)
{
    public DbSet<OperationSubmission> OperationSubmissions => Set<OperationSubmission>();

    public DbSet<CharacterLedgerEntry> CharacterLedgerEntries => Set<CharacterLedgerEntry>();

    public DbSet<LedgerRevision> LedgerRevisions => Set<LedgerRevision>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<IdentityUser>()
            .HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique();
        builder.Entity<OperationSubmission>(entity =>
        {
            entity.HasIndex(submission => new { submission.AccountId, submission.OperationId }).IsUnique();
            entity.Property(submission => submission.AccountId).HasMaxLength(450).IsRequired();
            entity.Property(submission => submission.OperationId).HasMaxLength(36).IsRequired();
            entity.Property(submission => submission.Family).HasMaxLength(32).IsRequired();
            entity.Property(submission => submission.Fingerprint).HasMaxLength(64).IsRequired();
            entity.Property(submission => submission.AdmissionDay).HasMaxLength(10).IsRequired();
            entity.Property(submission => submission.State).HasMaxLength(16).IsRequired();
        });
        builder.Entity<CharacterLedgerEntry>(entity =>
        {
            entity.HasKey(entry => new { entry.Scope, entry.AccountId, entry.Day });
            entity.Property(entry => entry.Scope).HasMaxLength(16);
            entity.Property(entry => entry.AccountId).HasMaxLength(450);
            entity.Property(entry => entry.Day).HasMaxLength(10);
        });
        builder.Entity<LedgerRevision>(entity =>
        {
            entity.HasKey(revision => revision.Id);
            entity.HasData(new LedgerRevision { Id = LedgerRevision.SingletonId, Value = 0 });
        });
    }
}
