namespace ShipperMvp.Infrastructure.Persistence.DbContexts;

using Microsoft.EntityFrameworkCore;
using ShipperMvp.Application.Common.Interfaces;
using ShipperMvp.Domain.Entities;
using System.Reflection;

public class ShipperMvpDbContext : DbContext, IApplicationDbContext
{
    public ShipperMvpDbContext(DbContextOptions<ShipperMvpDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderStatusLog> OrderStatusLogs => Set<OrderStatusLog>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
