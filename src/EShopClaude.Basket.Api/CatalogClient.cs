using System.Net;

namespace EShopClaude.Basket.Api;

public record CatalogProduct(int Id, string Name, decimal Price);

public class CatalogClient(HttpClient http)
{
    public async Task<CatalogProduct?> GetProductAsync(int id)
    {
        using var response = await http.GetAsync($"/products/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CatalogProduct>();
    }
}
