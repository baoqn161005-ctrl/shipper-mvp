namespace ShipperMvp.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShipperMvp.Domain.Entities;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews", "dbo", table => table.HasCheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5"));
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Comment).HasMaxLength(1000).HasColumnType("nvarchar(1000)");
        builder.Property(r => r.CreatedAt).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(r => r.OrderId).IsUnique().HasDatabaseName("UQ_Reviews_OrderId");
        builder.HasIndex(r => new { r.DriverId, r.CreatedAt }).HasDatabaseName("IX_Reviews_DriverId_CreatedAt");
        builder.HasOne(r => r.Order).WithOne(o => o.Review).HasForeignKey<Review>(r => r.OrderId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Reviews_Orders");
        builder.HasOne(r => r.Customer).WithMany(u => u.CustomerReviews).HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Reviews_Customers");
        builder.HasOne(r => r.Driver).WithMany(d => d.Reviews).HasForeignKey(r => r.DriverId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Reviews_Drivers");
    }
}
