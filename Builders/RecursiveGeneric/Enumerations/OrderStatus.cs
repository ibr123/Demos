namespace Builders.RecursiveGeneric.Enumerations;

public sealed class OrderStatus : Enumeration<OrderStatus>
{
    public static readonly OrderStatus Pending   = new(1, "Pending");
    public static readonly OrderStatus Paid      = new(2, "Paid");
    public static readonly OrderStatus Shipped   = new(3, "Shipped");
    public static readonly OrderStatus Delivered = new(4, "Delivered");
    public static readonly OrderStatus Cancelled = new(5, "Cancelled");

    private OrderStatus(int id, string name) : base(id, name) { }
}
