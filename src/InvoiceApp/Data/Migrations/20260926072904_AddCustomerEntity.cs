using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Invoices");

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "Invoices",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Customers_CustomerId",
                table: "Invoices",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Customers_CustomerId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Invoices");

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Invoices",
                type: "TEXT",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }
    }
}
