using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace fanfnir_back.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenFinanceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpenFinanceConexoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FkIdUsuario = table.Column<int>(type: "integer", nullable: false),
                    Provedor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProvedorItemId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InstituicaoId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InstituicaoNome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("OpenFinanceConexoes_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenFinanceConexoes_Usuarios_FkIdUsuario",
                        column: x => x.FkIdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContasBancarias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FkIdUsuario = table.Column<int>(type: "integer", nullable: false),
                    FkIdConexao = table.Column<int>(type: "integer", nullable: false),
                    Provedor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProvedorContaId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InstituicaoId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InstituicaoNome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Moeda = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SaldoAtual = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    SaldoDisponivel = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    UltimaSincronizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ContasBancarias_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasBancarias_OpenFinanceConexoes_FkIdConexao",
                        column: x => x.FkIdConexao,
                        principalTable: "OpenFinanceConexoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContasBancarias_Usuarios_FkIdUsuario",
                        column: x => x.FkIdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransacoesBancarias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FkIdUsuario = table.Column<int>(type: "integer", nullable: false),
                    FkIdContaBancaria = table.Column<int>(type: "integer", nullable: false),
                    Provedor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProvedorTransacaoId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DataTransacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EstabelecimentoNome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Moeda = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FkIdCategoria = table.Column<int>(type: "integer", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TransacoesBancarias_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransacoesBancarias_Categorias_FkIdCategoria",
                        column: x => x.FkIdCategoria,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransacoesBancarias_ContasBancarias_FkIdContaBancaria",
                        column: x => x.FkIdContaBancaria,
                        principalTable: "ContasBancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransacoesBancarias_Usuarios_FkIdUsuario",
                        column: x => x.FkIdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContasBancarias_FkIdConexao",
                table: "ContasBancarias",
                column: "FkIdConexao");

            migrationBuilder.CreateIndex(
                name: "IX_ContasBancarias_FkIdUsuario",
                table: "ContasBancarias",
                column: "FkIdUsuario");

            migrationBuilder.CreateIndex(
                name: "UX_ContasBancarias_Provedor_ContaId",
                table: "ContasBancarias",
                columns: new[] { "Provedor", "ProvedorContaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpenFinanceConexoes_FkIdUsuario",
                table: "OpenFinanceConexoes",
                column: "FkIdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_OpenFinanceConexoes_ProvedorItemId",
                table: "OpenFinanceConexoes",
                column: "ProvedorItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransacoesBancarias_FkIdCategoria",
                table: "TransacoesBancarias",
                column: "FkIdCategoria");

            migrationBuilder.CreateIndex(
                name: "IX_TransacoesBancarias_FkIdContaBancaria",
                table: "TransacoesBancarias",
                column: "FkIdContaBancaria");

            migrationBuilder.CreateIndex(
                name: "IX_TransacoesBancarias_FkIdUsuario",
                table: "TransacoesBancarias",
                column: "FkIdUsuario");

            migrationBuilder.CreateIndex(
                name: "UX_TransacoesBancarias_Provedor_Transacao_Conta",
                table: "TransacoesBancarias",
                columns: new[] { "Provedor", "ProvedorTransacaoId", "FkIdContaBancaria" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransacoesBancarias");

            migrationBuilder.DropTable(
                name: "ContasBancarias");

            migrationBuilder.DropTable(
                name: "OpenFinanceConexoes");
        }
    }
}
