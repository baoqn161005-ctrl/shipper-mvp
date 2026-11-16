namespace ShipperMvp.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShipperMvp.Domain.Entities;

public class OrderStatusLogConfiguration : IEntityTypeConfiguration<OrderStatusLog>
{
    public void Configure(EntityTypeBuilder<OrderStatusLog> builder)
    {
        builder.ToTable("OrderStatusLogs", "dbo");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.OrderId)
            .IsRequired();

        builder.Property(l => l.FromStatus)
            .HasConversion<byte?>()
            .HasColumnType("tinyint");

        builder.Property(l => l.ToStatus)
            .IsRequired()
            .HasConversion<byte>()
            .HasColumnType("tinyint");

        builder.Property(l => l.ChangedByUserId)
            .IsRequired();

        builder.Property(l => l.Note)
            .HasMaxLength(255)
            .HasColumnType("nvarchar(255)");

        builder.Property(l => l.ChangedAt)
            .IsRequired()
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(l => l.Order)
            .WithMany(o => o.StatusLogs)
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_OrderStatusLogs_Orders");

        builder.HasOne(l => l.ChangedByUser)
            .WithMany(u => u.OrderStatusLogs)
            .HasForeignKey(l => l.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_OrderStatusLogs_Users");

        builder.HasIndex(l => new { l.OrderId, l.ChangedAt })
            .HasDatabaseName("IX_OrderStatusLogs_OrderId_ChangedAt");
    }
}
