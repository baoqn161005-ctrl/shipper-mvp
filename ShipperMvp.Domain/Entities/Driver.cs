namespace ShipperMvp.Domain.Entities;

public class Driver
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleType { get; set; } = "Xe máy";
    public bool IsOnline { get; set; } = false;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Order> DriverOrders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
