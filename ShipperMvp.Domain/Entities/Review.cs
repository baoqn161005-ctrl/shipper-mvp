namespace ShipperMvp.Domain.Entities;

public class Review
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public int DriverId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    public Order Order { get; set; } = null!;
    public User Customer { get; set; } = null!;
    public Driver Driver { get; set; } = null!;
}
