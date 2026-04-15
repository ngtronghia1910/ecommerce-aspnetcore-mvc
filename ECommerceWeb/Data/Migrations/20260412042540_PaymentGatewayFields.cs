using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class PaymentGatewayFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GatewayTxnRef",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "StockDeducted",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Đơn cũ (COD) đã trừ tồn kho khi checkout; đánh dấu để callback online không trừ lần nữa.
            migrationBuilder.Sql(
                "UPDATE [Orders] SET [StockDeducted] = 1 WHERE [PaymentMethod] = 0 AND [PaymentStatus] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GatewayTxnRef",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StockDeducted",
                table: "Orders");
        }
    }
}
