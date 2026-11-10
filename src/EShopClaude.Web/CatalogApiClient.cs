namespace EShopClaude.Web;

public record ProductDto(int Id, string Name, string Description, decimal Price);

public class CatalogApiClient(HttpClient http)
{
    public async Task<ProductDto[]> GetProductsAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<ProductDto[]>("/products", ct) ?? [];
}
