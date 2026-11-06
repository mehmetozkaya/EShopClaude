namespace EShopClaude.Basket.Api;

public class Basket
{
    public string BuyerId { get; set; } = string.Empty;
    public List<BasketItem> Items { get; set; } = [];
}

public class BasketItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public record AddItemRequest(int ProductId, int Quantity);

public record CheckoutResponse(int OrderId);
