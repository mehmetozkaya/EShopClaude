namespace EShopClaude.Ordering.Api;

public class Order
{
    public int Id { get; set; }
    public string BuyerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal Total { get; set; }
    public List<OrderLine> Lines { get; set; } = [];
}

public class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public record CreateOrderRequest(string BuyerId, List<CreateOrderLine> Lines);

public record CreateOrderLine(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record CreateOrderResponse(int Id);

public record OrderSummary(int Id, DateTime CreatedAt, decimal Total, int ItemCount);
