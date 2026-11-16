namespace ShipperMvp.Application.Orders;

using Microsoft.EntityFrameworkCore;
using ShipperMvp.Application.Common.Exceptions;
using ShipperMvp.Application.Common.Interfaces;
using ShipperMvp.Domain.Entities;
using ShipperMvp.Domain.Enums;

public sealed class OrderService(IApplicationDbContext db) : IOrderService
{
    private const decimal MaxOutstandingCodAmount = 5_000_000m;

    public async Task<OrderResponse> CreateAsync(int customerId, CreateOrderRequest request, CancellationToken ct = default)
    {
        ValidateText(request.PickupAddress, 255, nameof(request.PickupAddress));
        ValidateText(request.PickupPhone, 15, nameof(request.PickupPhone));
        ValidateText(request.DeliveryAddress, 255, nameof(request.DeliveryAddress));
        ValidateText(request.ReceiverName, 100, nameof(request.ReceiverName));
        ValidateText(request.ReceiverPhone, 15, nameof(request.ReceiverPhone));
        ValidateText(request.ItemDescription, 500, nameof(request.ItemDescription));
        ValidateCoordinate(request.PickupLatitude, -90, 90, nameof(request.PickupLatitude));
        ValidateCoordinate(request.PickupLongitude, -180, 180, nameof(request.PickupLongitude));
        ValidateCoordinate(request.DeliveryLatitude, -90, 90, nameof(request.DeliveryLatitude));
        ValidateCoordinate(request.DeliveryLongitude, -180, 180, nameof(request.DeliveryLongitude));
        if (request.ShippingFee < 0 || request.CodAmount < 0) throw new BadRequestException("Phí giao hàng và COD không được âm.");
        if (!await db.Users.AnyAsync(u => u.Id == customerId && u.IsActive && u.Role == "Customer", ct))
            throw new ForbiddenAccessException("Tài khoản không phải khách hàng đang hoạt động.");

        var now = DateTime.UtcNow;
        var order = new Order { OrderCode = $"OD{Guid.NewGuid():N}"[..20].ToUpperInvariant(), CustomerId = customerId,
            PickupAddress = request.PickupAddress.Trim(), PickupLatitude = request.PickupLatitude, PickupLongitude = request.PickupLongitude,
            PickupPhone = request.PickupPhone.Trim(), DeliveryAddress = request.DeliveryAddress.Trim(),
            DeliveryLatitude = request.DeliveryLatitude, DeliveryLongitude = request.DeliveryLongitude, ReceiverName = request.ReceiverName.Trim(),
            ReceiverPhone = request.ReceiverPhone.Trim(), ItemDescription = request.ItemDescription.Trim(),
            ShippingFee = request.ShippingFee, CodAmount = request.CodAmount, Status = OrderStatus.Pending,
            CreatedAt = now, UpdatedAt = now };
        db.Orders.Add(order);
        db.OrderStatusLogs.Add(new OrderStatusLog { Order = order, FromStatus = null, ToStatus = OrderStatus.Pending,
            ChangedByUserId = customerId, Note = "Tạo đơn hàng", ChangedAt = now });
        await db.SaveChangesAsync(ct);
        return await Project(db.Orders.Where(o => o.Id == order.Id)).SingleAsync(ct);
    }

    public async Task<IReadOnlyList<OrderResponse>> GetCustomerOrdersAsync(int customerId, CancellationToken ct = default) =>
        await Project(db.Orders.Where(o => o.CustomerId == customerId).OrderByDescending(o => o.CreatedAt)).ToListAsync(ct);

    public async Task<IReadOnlyList<OrderResponse>> GetAvailableAsync(CancellationToken ct = default) =>
        await Project(db.Orders.Where(o => o.Status == OrderStatus.Pending && o.DriverId == null).OrderBy(o => o.CreatedAt)).ToListAsync(ct);

    public async Task<IReadOnlyList<OrderResponse>> GetDriverOrdersAsync(int driverUserId, bool history, CancellationToken ct = default)
    {
        var driverId = await db.Drivers.Where(d => d.UserId == driverUserId).Select(d => (int?)d.Id).SingleOrDefaultAsync(ct)
            ?? throw new ForbiddenAccessException("Không tìm thấy hồ sơ shipper.");
        var statuses = history ? new[] { OrderStatus.Completed } : new[] { OrderStatus.Accepted, OrderStatus.PickedUp, OrderStatus.Delivering };
        return await Project(db.Orders.Where(o => o.DriverId == driverId && statuses.Contains(o.Status)).OrderByDescending(o => o.UpdatedAt)).ToListAsync(ct);
    }

    public async Task<OrderResponse> GetByIdAsync(int orderId, int userId, bool isShipper, CancellationToken ct = default)
    {
        var query = db.Orders.Where(o => o.Id == orderId);
        if (!isShipper) query = query.Where(o => o.CustomerId == userId);
        var result = await Project(query).SingleOrDefaultAsync(ct);
        return result ?? throw new NotFoundException("Không tìm thấy đơn hàng.");
    }

    public async Task CancelAsync(int orderId, int customerId, CancellationToken ct = default)
    {
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == orderId, ct) ?? throw new NotFoundException("Không tìm thấy đơn hàng.");
        if (order.CustomerId != customerId) throw new ForbiddenAccessException("Bạn không có quyền hủy đơn hàng này.");
        if (order.Status != OrderStatus.Pending) throw new BadRequestException("Chỉ có thể hủy đơn đang chờ shipper.");
        await ChangeStatusAsync(order, OrderStatus.Cancelled, customerId, "Khách hàng hủy đơn", ct);
    }

    public async Task<OrderResponse> AcceptAsync(int orderId, int driverUserId, CancellationToken ct = default)
    {
        var driver = await db.Drivers.SingleOrDefaultAsync(d => d.UserId == driverUserId && d.User.IsActive && d.User.Role == "Shipper", ct)
            ?? throw new ForbiddenAccessException("Tài khoản chưa có hồ sơ shipper hợp lệ.");
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == orderId, ct) ?? throw new NotFoundException("Không tìm thấy đơn hàng.");
        if (order.Status != OrderStatus.Pending || order.DriverId is not null)
            throw new ConcurrencyException("Đơn hàng không còn ở trạng thái chờ nhận.");
        // Until COD reconciliation is modeled, active orders represent COD currently held by the shipper.
        var outstandingCod = await db.Orders
            .Where(o => o.DriverId == driver.Id &&
                        (o.Status == OrderStatus.Accepted || o.Status == OrderStatus.PickedUp || o.Status == OrderStatus.Delivering))
            .SumAsync(o => (decimal?)o.CodAmount, ct) ?? 0m;
        if (outstandingCod + order.CodAmount > MaxOutstandingCodAmount)
            throw new BadRequestException("Shipper đã vượt hạn mức COD 5.000.000 VNĐ.");
        await ChangeStatusAsync(order, OrderStatus.Accepted, driverUserId, "Shipper nhận đơn", ct, driver.Id);
        return await Project(db.Orders.Where(o => o.Id == orderId)).SingleAsync(ct);
    }

    public async Task<OrderResponse> UpdateStatusAsync(int orderId, int driverUserId, OrderStatus newStatus, CancellationToken ct = default)
    {
        var driver = await db.Drivers.SingleOrDefaultAsync(d => d.UserId == driverUserId, ct) ?? throw new ForbiddenAccessException("Không tìm thấy hồ sơ shipper.");
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == orderId, ct) ?? throw new NotFoundException("Không tìm thấy đơn hàng.");
        if (order.DriverId != driver.Id) throw new ForbiddenAccessException("Đơn hàng này không thuộc shipper hiện tại.");
        var expected = order.Status switch { OrderStatus.Accepted => OrderStatus.PickedUp, OrderStatus.PickedUp => OrderStatus.Delivering,
            OrderStatus.Delivering => OrderStatus.Completed, _ => (OrderStatus?)null };
        if (expected is null || newStatus != expected || !Enum.IsDefined(newStatus)) throw new BadRequestException("Chuyển trạng thái đơn hàng không hợp lệ.");
        await ChangeStatusAsync(order, newStatus, driverUserId, null, ct);
        return await Project(db.Orders.Where(o => o.Id == orderId)).SingleAsync(ct);
    }

    private async Task ChangeStatusAsync(Order order, OrderStatus status, int userId, string? note, CancellationToken ct, int? driverId = null)
    {
        var previous = order.Status;
        if (driverId.HasValue) order.DriverId = driverId;
        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        db.OrderStatusLogs.Add(new OrderStatusLog { OrderId = order.Id, FromStatus = previous, ToStatus = status, ChangedByUserId = userId, Note = note, ChangedAt = order.UpdatedAt });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException ex) { throw new ConcurrencyException("Đơn hàng vừa được cập nhật bởi người khác. Hãy tải lại danh sách rồi thử lại.", ex); }
    }

    private static IQueryable<OrderResponse> Project(IQueryable<Order> query) => query.Select(o => new OrderResponse(
        o.Id, o.OrderCode, o.CustomerId, o.DriverId, o.PickupAddress, o.PickupPhone, o.DeliveryAddress, o.ReceiverName,
        o.ReceiverPhone, o.ItemDescription, o.ShippingFee, o.CodAmount, o.Status, o.CreatedAt, o.UpdatedAt,
        o.Customer.FullName, o.Customer.PhoneNumber, o.Driver == null ? null : o.Driver.User.FullName,
        o.Driver == null ? null : o.Driver.User.PhoneNumber, o.PickupLatitude, o.PickupLongitude, o.DeliveryLatitude, o.DeliveryLongitude));

    private static void ValidateText(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength) throw new BadRequestException($"Trường {name} là bắt buộc và tối đa {maxLength} ký tự.");
    }

    private static void ValidateCoordinate(decimal? value, decimal min, decimal max, string name)
    {
        if (value.HasValue && (value.Value < min || value.Value > max)) throw new BadRequestException($"Trường {name} phải nằm trong khoảng {min} đến {max}.");
    }
}
