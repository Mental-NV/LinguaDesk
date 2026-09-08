using Microsoft.EntityFrameworkCore;

namespace LinguaDesk.Api.Infrastructure.Persistence;

public sealed class LinguaDeskDbContext(DbContextOptions<LinguaDeskDbContext> options) : DbContext(options);
