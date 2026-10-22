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

app.MapPost("/orders", async (CreateOrderRequest request, OrderingDbContext db) =>
{
    var order = new Order
    {
        BuyerId = request.BuyerId,
        CreatedAt = DateTime.UtcNow,
        Lines = request.Lines.Select(l => new OrderLine
        {
            ProductId = l.ProductId,
            ProductName = l.ProductName,
            UnitPrice = l.UnitPrice,
            Quantity = l.Quantity
        }).ToList()
    };
    order.Total = order.Lines.Sum(l => l.UnitPrice * l.Quantity);

    db.Orders.Add(order);
    await db.SaveChangesAsync();

    return Results.Created($"/orders/{order.Id}", new CreateOrderResponse(order.Id));
});

app.MapGet("/orders/{id:int}", async (int id, OrderingDbContext db) =>
    await db.Orders.AsNoTracking()
        .Include(o => o.Lines.OrderBy(l => l.Id))
        .FirstOrDefaultAsync(o => o.Id == id) is { } order
        ? Results.Ok(order)
        : Results.NotFound());

app.MapDefaultEndpoints();

app.Run();
