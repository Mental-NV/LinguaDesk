using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LinguaDesk.Api.Infrastructure.Persistence;

public sealed class LinguaDeskDbContext(DbContextOptions<LinguaDeskDbContext> options)
    : IdentityUserContext<IdentityUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<IdentityUser>()
            .HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique();
    }
}
