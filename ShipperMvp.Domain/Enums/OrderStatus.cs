namespace ShipperMvp.Domain.Enums;

public enum OrderStatus : byte
{
    Pending = 0,    // Đang tìm Shipper
    Accepted = 1,   // Đã có Shipper nhận
    PickedUp = 2,   // Đã lấy hàng
    Delivering = 3, // Đang giao hàng
    Completed = 4,  // Giao thành công
    Cancelled = 5   // Đã hủy
}
