using EShopClaude.Basket.Api;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisClient("basket-cache");

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<BasketStore>();
builder.Services.AddHttpClient<CatalogClient>(c => c.BaseAddress = new("https+http://catalog-api"));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapGet("/", () => "Basket API is running.");

app.MapGet("/baskets/{buyerId}", async (string buyerId, BasketStore store) =>
    Results.Ok(await store.GetAsync(buyerId) ?? new Basket { BuyerId = buyerId }));

app.MapPost("/baskets/{buyerId}/items", async (string buyerId, AddItemRequest request, CatalogClient catalog, BasketStore store) =>
{
    var product = await catalog.GetProductAsync(request.ProductId);
    if (product is null) return Results.NotFound();

    var basket = await store.GetAsync(buyerId) ?? new Basket { BuyerId = buyerId };
    var line = basket.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
    if (line is not null)
    {
        line.Quantity += request.Quantity;
    }
    else
    {
        basket.Items.Add(new BasketItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.Price,
            Quantity = request.Quantity
        });
    }

    await store.SaveAsync(basket);
    return Results.Ok(basket);
});

app.MapDelete("/baskets/{buyerId}/items/{productId:int}", async (string buyerId, int productId, BasketStore store) =>
{
    var basket = await store.GetAsync(buyerId);
    if (basket is null) return Results.Ok(new Basket { BuyerId = buyerId });

    if (basket.Items.RemoveAll(i => i.ProductId == productId) == 0) return Results.Ok(basket);

    await store.SaveAsync(basket);
    return Results.Ok(basket);
});

app.MapDefaultEndpoints();

app.Run();
