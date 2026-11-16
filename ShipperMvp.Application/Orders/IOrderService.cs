namespace ShipperMvp.Application.Orders;

using ShipperMvp.Domain.Enums;

public interface IOrderService
{
    Task<OrderResponse> CreateAsync(int customerId, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderResponse>> GetCustomerOrdersAsync(int customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderResponse>> GetAvailableAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderResponse>> GetDriverOrdersAsync(int driverUserId, bool history, CancellationToken cancellationToken = default);
    Task<OrderResponse> GetByIdAsync(int orderId, int userId, bool isShipper, CancellationToken cancellationToken = default);
    Task CancelAsync(int orderId, int customerId, CancellationToken cancellationToken = default);
    Task<OrderResponse> AcceptAsync(int orderId, int driverUserId, CancellationToken cancellationToken = default);
    Task<OrderResponse> UpdateStatusAsync(int orderId, int driverUserId, OrderStatus newStatus, CancellationToken cancellationToken = default);
}
