using EShopClaude.Catalog.Api;

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

app.MapDefaultEndpoints();

app.Run();
