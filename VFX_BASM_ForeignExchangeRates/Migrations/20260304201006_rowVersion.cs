using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VFX_BASM_ForeignExchangeRates.Migrations
{
    /// <inheritdoc />
    public partial class rowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ForeignExchangeRates",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ForeignExchangeRates");
        }
    }
}
