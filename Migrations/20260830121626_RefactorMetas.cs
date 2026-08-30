using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fanfnir_back.Migrations
{
    /// <inheritdoc />
    public partial class RefactorMetas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Metas_Carteiras_FkIdCarteira",
                table: "Metas");



            migrationBuilder.DropColumn(
                name: "FkIdCarteira",
                table: "Metas");

            migrationBuilder.AlterColumn<int>(
                name: "FkIdCarteira",
                table: "AportesMetas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FkIdCarteira",
                table: "Metas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "FkIdCarteira",
                table: "AportesMetas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);



            migrationBuilder.AddForeignKey(
                name: "FK_Metas_Carteiras_FkIdCarteira",
                table: "Metas",
                column: "FkIdCarteira",
                principalTable: "Carteiras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
