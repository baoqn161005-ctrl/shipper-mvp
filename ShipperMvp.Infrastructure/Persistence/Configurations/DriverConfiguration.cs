namespace ShipperMvp.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShipperMvp.Domain.Entities;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("Drivers", "dbo");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.UserId)
            .IsRequired();

        builder.HasIndex(d => d.UserId)
            .IsUnique()
            .HasDatabaseName("UQ_Drivers_UserId");

        builder.Property(d => d.LicensePlate)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnType("varchar(20)");

        builder.Property(d => d.VehicleType)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("nvarchar(50)")
            .HasDefaultValue("Xe máy");

        builder.Property(d => d.IsOnline)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(d => d.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(d => d.User)
            .WithOne(u => u.Driver)
            .HasForeignKey<Driver>(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Drivers_Users");

        builder.HasIndex(d => d.IsOnline)
            .HasDatabaseName("IX_Drivers_IsOnline");
    }
}
