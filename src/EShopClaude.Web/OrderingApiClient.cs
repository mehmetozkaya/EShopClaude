using System.Net;

namespace EShopClaude.Web;

public record OrderDto(int Id, string BuyerId, DateTime CreatedAt, decimal Total, List<OrderLineDto> Lines);

public record OrderLineDto(int Id, int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record OrderSummaryDto(int Id, DateTime CreatedAt, decimal Total, int ItemCount);

public class OrderingApiClient(HttpClient http)
{
    public async Task<OrderDto?> GetOrderAsync(int id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/orders/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>(ct);
    }

    public async Task<OrderSummaryDto[]> GetOrdersAsync(string buyerId, CancellationToken ct = default)
        => await http.GetFromJsonAsync<OrderSummaryDto[]>($"/orders?buyerId={Uri.EscapeDataString(buyerId)}", ct) ?? [];
}
