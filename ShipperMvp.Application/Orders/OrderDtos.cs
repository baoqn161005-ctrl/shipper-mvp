namespace ShipperMvp.Application.Orders;

using ShipperMvp.Domain.Enums;

public sealed record CreateOrderRequest(
    string PickupAddress,
    string PickupPhone,
    string DeliveryAddress,
    string ReceiverName,
    string ReceiverPhone,
    string ItemDescription,
    decimal ShippingFee,
    decimal CodAmount = 0,
    decimal? PickupLatitude = null,
    decimal? PickupLongitude = null,
    decimal? DeliveryLatitude = null,
    decimal? DeliveryLongitude = null);

public sealed record UpdateOrderStatusRequest(OrderStatus NewStatus);

public sealed record OrderResponse(
    int Id,
    string OrderCode,
    int CustomerId,
    int? DriverId,
    string PickupAddress,
    string PickupPhone,
    string DeliveryAddress,
    string ReceiverName,
    string ReceiverPhone,
    string ItemDescription,
    decimal ShippingFee,
    decimal CodAmount,
    OrderStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? CustomerName,
    string? CustomerPhone,
    string? DriverName,
    string? DriverPhone,
    decimal? PickupLatitude,
    decimal? PickupLongitude,
    decimal? DeliveryLatitude,
    decimal? DeliveryLongitude);
