using Microsoft.EntityFrameworkCore;

namespace EShopClaude.Ordering.Api;

public class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
            order.Property(o => o.BuyerId).IsRequired().HasMaxLength(100);
            order.Property(o => o.Total).HasPrecision(18, 2);
            order.HasMany(o => o.Lines)
                .WithOne()
                .HasForeignKey(l => l.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderLine>(line =>
        {
            line.Property(l => l.ProductName).IsRequired().HasMaxLength(100);
            line.Property(l => l.UnitPrice).HasPrecision(18, 2);
        });
    }
}
