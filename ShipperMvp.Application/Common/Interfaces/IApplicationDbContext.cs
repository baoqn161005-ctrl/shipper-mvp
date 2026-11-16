namespace ShipperMvp.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;
using ShipperMvp.Domain.Entities;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Driver> Drivers { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderStatusLog> OrderStatusLogs { get; }
    DbSet<Review> Reviews { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
