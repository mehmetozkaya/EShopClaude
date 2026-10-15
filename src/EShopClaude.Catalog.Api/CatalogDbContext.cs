using Microsoft.EntityFrameworkCore;

namespace EShopClaude.Catalog.Api;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(product =>
        {
            product.Property(p => p.Name).IsRequired().HasMaxLength(100);
            product.Property(p => p.Description).IsRequired().HasMaxLength(500);
            product.Property(p => p.Price).HasPrecision(18, 2);
        });
    }

    // Seeds the demo products once; skipped when the table already has rows.
    public static async Task SeedAsync(CatalogDbContext db)
    {
        if (await db.Products.AnyAsync())
        {
            return;
        }

        db.Products.AddRange(
            new Product { Name = "Wireless Mouse", Description = "Ergonomic 2.4 GHz wireless mouse with silent clicks.", Price = 24.99m },
            new Product { Name = "Mechanical Keyboard", Description = "Tenkeyless keyboard with tactile brown switches.", Price = 89.00m },
            new Product { Name = "USB-C Hub", Description = "7-in-1 hub with HDMI, USB-A, SD and 100 W pass-through.", Price = 39.50m },
            new Product { Name = "27\" Monitor", Description = "27-inch QHD IPS monitor, 75 Hz, adjustable stand.", Price = 249.00m },
            new Product { Name = "Noise-Cancelling Headphones", Description = "Over-ear Bluetooth headphones with 30-hour battery.", Price = 179.99m },
            new Product { Name = "Laptop Stand", Description = "Aluminium stand that raises the screen to eye level.", Price = 34.95m },
            new Product { Name = "Webcam HD", Description = "1080p webcam with dual microphones and privacy cover.", Price = 59.00m },
            new Product { Name = "Desk Mat", Description = "Large 90 × 40 cm water-resistant desk mat.", Price = 19.90m });

        await db.SaveChangesAsync();
    }
}
