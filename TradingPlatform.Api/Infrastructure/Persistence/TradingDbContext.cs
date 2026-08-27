using Microsoft.EntityFrameworkCore;
using TradingPlatform.Api.Domain;

namespace TradingPlatform.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for the trading platform.
/// Scope: Trade persistence only. Price state is intentionally NOT here —
/// it lives in an in-memory store fed by the WebSocket service (Phase 3/4);
/// SQL Server is for trades only (see docs/assumptions.md D2).
/// </summary>
public class TradingDbContext : DbContext
{
    public TradingDbContext(DbContextOptions<TradingDbContext> options)
        : base(options)
    {
    }

    /// <summary>Executed trades (assignment §8 storage requirements).</summary>
    public DbSet<Trade> Trades => Set<Trade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up all IEntityTypeConfiguration<T> classes in this assembly
        // (Infrastructure/Persistence/Configurations). Keeps per-entity config isolated.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingDbContext).Assembly);
    }
}