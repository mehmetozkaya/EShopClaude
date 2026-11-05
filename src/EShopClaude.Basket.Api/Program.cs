using EShopClaude.Basket.Api;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisClient("basket-cache");

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<BasketStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapGet("/", () => "Basket API is running.");

app.MapGet("/baskets/{buyerId}", async (string buyerId, BasketStore store) =>
    Results.Ok(await store.GetAsync(buyerId) ?? new Basket { BuyerId = buyerId }));

app.MapDefaultEndpoints();

app.Run();
