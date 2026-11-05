using System.Text.Json;
using StackExchange.Redis;

namespace EShopClaude.Basket.Api;

public class BasketStore(IConnectionMultiplexer redis)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static string Key(string buyerId) => $"basket:{buyerId}";

    public async Task<Basket?> GetAsync(string buyerId)
    {
        var value = await redis.GetDatabase().StringGetAsync(Key(buyerId));
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<Basket>(value.ToString(), JsonOptions);
    }
}
