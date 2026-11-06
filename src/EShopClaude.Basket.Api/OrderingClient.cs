namespace EShopClaude.Basket.Api;

public record CreateOrderRequest(string BuyerId, List<CreateOrderLine> Lines);

public record CreateOrderLine(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record CreateOrderResponse(int Id);

public class OrderingClient(HttpClient http)
{
    public async Task<int> CreateOrderAsync(Basket basket)
    {
        var request = new CreateOrderRequest(
            basket.BuyerId,
            basket.Items.Select(i => new CreateOrderLine(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity)).ToList());

        using var response = await http.PostAsJsonAsync("/orders", request);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        return created!.Id;
    }
}
