namespace ShipperMvp.Domain.Entities;

using ShipperMvp.Domain.Enums;

public class Order
{
    public int Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int? DriverId { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public decimal? PickupLatitude { get; set; }
    public decimal? PickupLongitude { get; set; }
    public string PickupPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal? DeliveryLatitude { get; set; }
    public decimal? DeliveryLongitude { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal ShippingFee { get; set; }
    public decimal CodAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User Customer { get; set; } = null!;
    public Driver? Driver { get; set; }
    public ICollection<OrderStatusLog> StatusLogs { get; set; } = new List<OrderStatusLog>();
    public Review? Review { get; set; }
}
