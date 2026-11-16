namespace ShipperMvp.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // 'Customer' | 'Shipper'
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Driver? Driver { get; set; }
    public ICollection<Order> CustomerOrders { get; set; } = new List<Order>();
    public ICollection<OrderStatusLog> OrderStatusLogs { get; set; } = new List<OrderStatusLog>();
    public ICollection<Review> CustomerReviews { get; set; } = new List<Review>();
}
