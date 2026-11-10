namespace EShopClaude.Web;

public record AddItemRequest(int ProductId, int Quantity);

public record BasketDto(string BuyerId, List<BasketItemDto> Items);

public record BasketItemDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record CheckoutResultDto(int OrderId);

public class BasketApiClient(HttpClient http)
{
    public async Task<BasketDto> GetBasketAsync(string buyerId, CancellationToken ct = default)
        => await http.GetFromJsonAsync<BasketDto>($"/baskets/{buyerId}", ct) ?? new BasketDto(buyerId, []);

    public async Task AddItemAsync(string buyerId, int productId, int quantity, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"/baskets/{buyerId}/items", new AddItemRequest(productId, quantity), ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<BasketDto> RemoveItemAsync(string buyerId, int productId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"/baskets/{buyerId}/items/{productId}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BasketDto>(ct) ?? new BasketDto(buyerId, []);
    }

    public async Task<int> CheckoutAsync(string buyerId, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"/baskets/{buyerId}/checkout", null, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CheckoutResultDto>(ct);
        return result!.OrderId;
    }
}
