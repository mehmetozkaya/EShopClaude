using EShopClaude.Catalog.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

// Add services to the container.
builder.Services.AddProblemDetails();

var app = builder.Build();

// Create the schema and seed the products at startup (no migrations; data resets with the AppHost).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.EnsureCreatedAsync();
    await CatalogDbContext.SeedAsync(db);
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapGet("/", () => "Catalog API is running.");

app.MapGet("/products", async (CatalogDbContext db) =>
    await db.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync());

app.MapGet("/products/{id:int}", async (int id, CatalogDbContext db) =>
    await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id) is { } product
        ? Results.Ok(product)
        : Results.NotFound());

app.MapDefaultEndpoints();

app.Run();
