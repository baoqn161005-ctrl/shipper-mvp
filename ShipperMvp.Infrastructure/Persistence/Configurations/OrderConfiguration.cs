namespace ShipperMvp.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShipperMvp.Domain.Entities;
using ShipperMvp.Domain.Enums;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        // The database state-machine trigger requires EF Core to avoid SQL OUTPUT.
        builder.ToTable("Orders", "dbo", table => table.UseSqlOutputClause(false));

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderCode)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnType("varchar(20)");

        builder.HasIndex(o => o.OrderCode)
            .IsUnique()
            .HasDatabaseName("UQ_Orders_OrderCode");

        builder.Property(o => o.CustomerId)
            .IsRequired();

        builder.Property(o => o.DriverId)
            .IsRequired(false);

        builder.Property(o => o.PickupAddress)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnType("nvarchar(255)");

        builder.Property(o => o.PickupPhone)
            .IsRequired()
            .HasMaxLength(15)
            .HasColumnType("varchar(15)");

        builder.Property(o => o.PickupLatitude).HasPrecision(9, 6);
        builder.Property(o => o.PickupLongitude).HasPrecision(9, 6);

        builder.Property(o => o.DeliveryAddress)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnType("nvarchar(255)");

        builder.Property(o => o.DeliveryLatitude).HasPrecision(9, 6);
        builder.Property(o => o.DeliveryLongitude).HasPrecision(9, 6);

        builder.Property(o => o.ReceiverName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(o => o.ReceiverPhone)
            .IsRequired()
            .HasMaxLength(15)
            .HasColumnType("varchar(15)");

        builder.Property(o => o.ItemDescription)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnType("nvarchar(500)");

        builder.Property(o => o.ShippingFee)
            .IsRequired()
            .HasPrecision(18, 0);

        builder.Property(o => o.CodAmount)
            .IsRequired()
            .HasPrecision(18, 0)
            .HasDefaultValue(0m);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .HasDefaultValue(OrderStatus.Pending);

        builder.Property(o => o.RowVersion)
            .IsRequired()
            .IsRowVersion();

        builder.Property(o => o.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(o => o.UpdatedAt)
            .IsRequired()
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Relationships
        builder.HasOne(o => o.Customer)
            .WithMany(u => u.CustomerOrders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Orders_Customer");

        builder.HasOne(o => o.Driver)
            .WithMany(d => d.DriverOrders)
            .HasForeignKey(o => o.DriverId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Orders_Driver");

        // Indexes matching database script
        builder.HasIndex(o => new { o.Status, o.CreatedAt })
            .HasDatabaseName("IX_Orders_Status_CreatedAt");

        builder.HasIndex(o => new { o.CustomerId, o.CreatedAt })
            .HasDatabaseName("IX_Orders_CustomerId_CreatedAt");

        builder.HasIndex(o => new { o.DriverId, o.Status, o.UpdatedAt })
            .HasDatabaseName("IX_Orders_DriverId_Status");
    }
}
