namespace EShopClaude.Web;

public record AddItemRequest(int ProductId, int Quantity);

public record BasketDto(string BuyerId, List<BasketItemDto> Items);

public record BasketItemDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public class BasketApiClient(HttpClient http)
{
    public async Task<BasketDto> GetBasketAsync(string buyerId, CancellationToken ct = default)
        => await http.GetFromJsonAsync<BasketDto>($"/baskets/{buyerId}", ct) ?? new BasketDto(buyerId, []);

    public async Task AddItemAsync(string buyerId, int productId, int quantity, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"/baskets/{buyerId}/items", new AddItemRequest(productId, quantity), ct);
        response.EnsureSuccessStatusCode();
    }
}
