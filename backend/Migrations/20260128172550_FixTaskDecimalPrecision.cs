using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class FixTaskDecimalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PickupLongitude",
                table: "Tasks",
                type: "decimal(11,8)",
                precision: 10,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,8)",
                oldPrecision: 10,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "PickupLatitude",
                table: "Tasks",
                type: "decimal(11,8)",
                precision: 10,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,8)",
                oldPrecision: 10,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "DeliveryLongitude",
                table: "Tasks",
                type: "decimal(11,8)",
                precision: 10,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,8)",
                oldPrecision: 10,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "DeliveryLatitude",
                table: "Tasks",
                type: "decimal(11,8)",
                precision: 10,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,8)",
                oldPrecision: 10,
                oldScale: 8);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PickupLongitude",
                table: "Tasks",
                type: "decimal(10,8)",
                precision: 10,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(11,8)",
                oldPrecision: 10,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "PickupLatitude",
                table: "Tasks",
                type: "decimal(10,8)",
                precision: 10,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(11,8)",
                oldPrecision: 10,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "DeliveryLongitude",
                table: "Tasks",
                type: "decimal(10,8)",
                precision: 10,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(11,8)",
                oldPrecision: 10,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "DeliveryLatitude",
                table: "Tasks",
                type: "decimal(10,8)",
                precision: 10,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(11,8)",
                oldPrecision: 10,
                oldScale: 8);
        }
    }
}
