namespace ShipperMvp.Infrastructure.Persistence.Migrations;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ShipperMvp.Infrastructure.Persistence.DbContexts;

[DbContext(typeof(ShipperMvpDbContext))]
[Migration("202611150001_AddReviewsAndOrderCoordinates")]
public sealed class AddReviewsAndOrderCoordinates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(name: "PickupLatitude", schema: "dbo", table: "Orders", type: "decimal(9,6)", precision: 9, scale: 6, nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "PickupLongitude", schema: "dbo", table: "Orders", type: "decimal(9,6)", precision: 9, scale: 6, nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "DeliveryLatitude", schema: "dbo", table: "Orders", type: "decimal(9,6)", precision: 9, scale: 6, nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "DeliveryLongitude", schema: "dbo", table: "Orders", type: "decimal(9,6)", precision: 9, scale: 6, nullable: true);

        migrationBuilder.CreateTable(name: "Reviews", schema: "dbo",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                OrderId = table.Column<int>(type: "int", nullable: false),
                CustomerId = table.Column<int>(type: "int", nullable: false),
                DriverId = table.Column<int>(type: "int", nullable: false),
                Rating = table.Column<int>(type: "int", nullable: false),
                Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Reviews", x => x.Id);
                table.CheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5");
                table.ForeignKey(name: "FK_Reviews_Orders", column: x => x.OrderId, principalTable: "Orders", principalColumn: "Id", principalSchema: "dbo", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "FK_Reviews_Customers", column: x => x.CustomerId, principalTable: "Users", principalColumn: "Id", principalSchema: "dbo", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "FK_Reviews_Drivers", column: x => x.DriverId, principalTable: "Drivers", principalColumn: "Id", principalSchema: "dbo", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "UQ_Reviews_OrderId", schema: "dbo", table: "Reviews", column: "OrderId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Reviews_CustomerId", schema: "dbo", table: "Reviews", column: "CustomerId");
        migrationBuilder.CreateIndex(name: "IX_Reviews_DriverId_CreatedAt", schema: "dbo", table: "Reviews", columns: new[] { "DriverId", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Reviews", schema: "dbo");
        migrationBuilder.DropColumn(name: "PickupLatitude", schema: "dbo", table: "Orders");
        migrationBuilder.DropColumn(name: "PickupLongitude", schema: "dbo", table: "Orders");
        migrationBuilder.DropColumn(name: "DeliveryLatitude", schema: "dbo", table: "Orders");
        migrationBuilder.DropColumn(name: "DeliveryLongitude", schema: "dbo", table: "Orders");
    }
}
