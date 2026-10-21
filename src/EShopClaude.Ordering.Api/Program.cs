using EShopClaude.Ordering.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<OrderingDbContext>("orderingdb");

// Add services to the container.
builder.Services.AddProblemDetails();

var app = builder.Build();

// Create the schema at startup (no migrations, no seed; data resets with the AppHost).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapGet("/", () => "Ordering API is running.");

app.MapDefaultEndpoints();

app.Run();
