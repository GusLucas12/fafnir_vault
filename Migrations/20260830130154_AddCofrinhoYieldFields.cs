using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fanfnir_back.Migrations
{
    /// <inheritdoc />
    public partial class AddCofrinhoYieldFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Rende",
                table: "Carteiras",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxaRendimento",
                table: "Carteiras",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoRendimento",
                table: "Carteiras",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoProcessamentoRendimento",
                table: "Carteiras",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rende",
                table: "Carteiras");

            migrationBuilder.DropColumn(
                name: "TaxaRendimento",
                table: "Carteiras");

            migrationBuilder.DropColumn(
                name: "TipoRendimento",
                table: "Carteiras");

            migrationBuilder.DropColumn(
                name: "UltimoProcessamentoRendimento",
                table: "Carteiras");
        }
    }
}
