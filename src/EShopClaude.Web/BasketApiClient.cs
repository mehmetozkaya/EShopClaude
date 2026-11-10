namespace EShopClaude.Web;

public record AddItemRequest(int ProductId, int Quantity);

public class BasketApiClient(HttpClient http)
{
    public async Task AddItemAsync(string buyerId, int productId, int quantity, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"/baskets/{buyerId}/items", new AddItemRequest(productId, quantity), ct);
        response.EnsureSuccessStatusCode();
    }
}
