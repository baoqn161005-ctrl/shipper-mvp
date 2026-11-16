using Microsoft.EntityFrameworkCore;
using Moq;
using ShipperMvp.Application.Common.Exceptions;
using ShipperMvp.Application.Common.Interfaces;
using ShipperMvp.Application.Orders;
using ShipperMvp.Domain.Entities;
using ShipperMvp.Domain.Enums;
using ShipperMvp.Infrastructure.Persistence.DbContexts;
using Xunit;

namespace ShipperMvp.Tests.Orders;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task UpdateStatusAsync_AcceptedToDelivered_RejectsSkippedTransition()
    {
        await using var db = CreateDatabase();
        await SeedShipperAndOrder(db, OrderStatus.Accepted);
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.UpdateStatusAsync(orderId: 100, driverUserId: 2, OrderStatus.Completed));

        Assert.IsType<BadRequestException>(exception);
        Assert.Equal(OrderStatus.Accepted, (await db.Orders.SingleAsync(o => o.Id == 100)).Status);
        Assert.Empty(await db.OrderStatusLogs.ToListAsync());
    }

    [Fact]
    public async Task AcceptAsync_WhenTwoRequestsCompete_SecondRequestIsRejected()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var seedDb = CreateDatabase(databaseName))
            await SeedShipperAndOrder(seedDb, OrderStatus.Pending);

        // Two independently scoped services share the same database, matching two API requests.
        await using var firstRequestDb = CreateDatabase(databaseName);
        await using var secondRequestDb = CreateDatabase(databaseName);
        var firstService = CreateService(firstRequestDb);
        var secondService = CreateService(secondRequestDb);

        await firstService.AcceptAsync(orderId: 100, driverUserId: 2);
        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            secondService.AcceptAsync(orderId: 100, driverUserId: 2));

        Assert.IsType<BadRequestException>(exception);
        var acceptedOrder = await firstRequestDb.Orders.AsNoTracking().SingleAsync(o => o.Id == 100);
        Assert.Equal(OrderStatus.Accepted, acceptedOrder.Status);
        Assert.Equal(10, acceptedOrder.DriverId);
    }

    [Fact]
    public async Task AcceptAsync_WhenNewOrderWouldExceedFiveMillionCodLimit_IsRejected()
    {
        await using var db = CreateDatabase();
        await SeedShipperAndOrder(db, OrderStatus.Pending);
        db.Orders.Add(new Order
        {
            Id = 101,
            OrderCode = "ORD-101",
            CustomerId = 1,
            DriverId = 10,
            PickupAddress = "Pickup",
            PickupPhone = "0900000001",
            DeliveryAddress = "Delivery",
            ReceiverName = "Receiver",
            ReceiverPhone = "0900000003",
            ItemDescription = "Existing COD parcel",
            ShippingFee = 25000,
            CodAmount = 5_000_000m,
            Status = OrderStatus.Accepted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        var pendingOrder = await db.Orders.SingleAsync(o => o.Id == 100);
        pendingOrder.CodAmount = 1m;
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.AcceptAsync(orderId: 100, driverUserId: 2));

        Assert.Contains("5.000.000", exception.Message);
        Assert.Equal(OrderStatus.Pending, (await db.Orders.SingleAsync(o => o.Id == 100)).Status);
        Assert.Equal(10, (await db.Orders.SingleAsync(o => o.Id == 101)).DriverId);
    }

    private static ShipperMvpDbContext CreateDatabase(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ShipperMvpDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
        return new ShipperMvpDbContext(options);
    }

    private static OrderService CreateService(ShipperMvpDbContext db)
    {
        var context = new Mock<IApplicationDbContext>();
        context.SetupGet(x => x.Users).Returns(db.Users);
        context.SetupGet(x => x.Drivers).Returns(db.Drivers);
        context.SetupGet(x => x.Orders).Returns(db.Orders);
        context.SetupGet(x => x.OrderStatusLogs).Returns(db.OrderStatusLogs);
        context.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct => db.SaveChangesAsync(ct));
        return new OrderService(context.Object);
    }

    private static async Task SeedShipperAndOrder(ShipperMvpDbContext db, OrderStatus status)
    {
        var customer = new User { Id = 1, FullName = "Customer", PhoneNumber = "0900000001", Role = "Customer" };
        var shipper = new User { Id = 2, FullName = "Shipper", PhoneNumber = "0900000002", Role = "Shipper" };
        var driver = new Driver { Id = 10, UserId = 2, LicensePlate = "59A-00000", User = shipper };
        db.Users.AddRange(customer, shipper);
        db.Drivers.Add(driver);
        db.Orders.Add(new Order
        {
            Id = 100,
            OrderCode = "ORD-100",
            CustomerId = customer.Id,
            Customer = customer,
            DriverId = status == OrderStatus.Pending ? null : driver.Id,
            Driver = status == OrderStatus.Pending ? null : driver,
            PickupAddress = "Pickup",
            PickupPhone = "0900000001",
            DeliveryAddress = "Delivery",
            ReceiverName = "Receiver",
            ReceiverPhone = "0900000003",
            ItemDescription = "Parcel",
            ShippingFee = 25000,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
