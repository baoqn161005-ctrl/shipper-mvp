namespace ShipperMvp.Domain.Entities;

using ShipperMvp.Domain.Enums;

public class OrderStatusLog
{
    public long Id { get; set; }
    public int OrderId { get; set; }
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public int ChangedByUserId { get; set; }
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; }

    // Navigation properties
    public Order Order { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}
