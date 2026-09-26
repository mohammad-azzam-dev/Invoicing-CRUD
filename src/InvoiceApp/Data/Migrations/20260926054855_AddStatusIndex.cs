using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status",
                table: "Invoices",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_Status",
                table: "Invoices");
        }
    }
}
