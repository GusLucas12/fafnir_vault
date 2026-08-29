using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace fanfnir_back.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    SenhaHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Hash da senha gerado pela aplicação; nunca persistir senha em texto puro."),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Usuarios_pkey", x => x.Id);
                },
                comment: "Usuários proprietários dos dados financeiros.");

            migrationBuilder.CreateTable(
                name: "Carteiras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FkIdUsuario = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SaldoInicial = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false, defaultValueSql: "0.00"),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Carteiras_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Carteiras_Usuarios_FkIdUsuario",
                        column: x => x.FkIdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Carteiras, contas ou meios onde o saldo do usuário é controlado.");

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FkIdUsuario = table.Column<int>(type: "integer", nullable: true, comment: "Nulo indica categoria padrão/global do sistema."),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Cor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Icone = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Categorias_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categorias_Usuarios_FkIdUsuario",
                        column: x => x.FkIdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Categorias globais e personalizadas para classificação de receitas e despesas.");

            migrationBuilder.CreateTable(
                name: "Metas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FkIdUsuario = table.Column<int>(type: "integer", nullable: false),
                    FkIdCarteira = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    TipoMeta = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ValorAlvo = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    ValorAtual = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false, defaultValueSql: "0.00"),
                    MesReferencia = table.Column<short>(type: "smallint", nullable: true),
                    AnoReferencia = table.Column<int>(type: "integer", nullable: true),
                    DataInicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataFim = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Concluida = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Metas_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Metas_Carteiras_FkIdCarteira",
                        column: x => x.FkIdCarteira,
                        principalTable: "Carteiras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Metas_Usuarios_FkIdUsuario",
                        column: x => x.FkIdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Objetivos financeiros do usuário, com acompanhamento de progresso.");

            migrationBuilder.CreateTable(
                name: "Assinaturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FkIdUsuario = table.Column<int>(type: "integer", nullable: false),
                    FkIdCategoria = table.Column<int>(type: "integer", nullable: false),
                    FkIdCarteira = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    DiaCobranca = table.Column<short>(type: "smallint", nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DataInicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataFim = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Assinaturas_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assinaturas_Carteiras_FkIdCarteira",
                        column: x => x.FkIdCarteira,
                        principalTable: "Carteiras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assinaturas_Categorias_FkIdCategoria",
                        column: x => x.FkIdCategoria,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assinaturas_Usuarios_FkIdUsuario",
                        column: x => x.FkIdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Cobranças recorrentes do usuário, úteis para previsão financeira.");

            migrationBuilder.CreateTable(
                name: "OrcamentosMensais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FkIdUsuario = table.Column<int>(type: "integer", nullable: false),
                    FkIdCategoria = table.Column<int>(type: "integer", nullable: false),
                    MesReferencia = table.Column<short>(type: "smallint", nullable: false),
                    AnoReferencia = table.Column<int>(type: "integer", nullable: false),
                    ValorLimite = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("OrcamentosMensais_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrcamentosMensais_Categorias_FkIdCategoria",
                        column: x => x.FkIdCategoria,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrcamentosMensais_Usuarios_FkIdUsuario",
                        column: x => x.FkIdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Limites mensais por categoria para controle de orçamento.");

            migrationBuilder.CreateTable(
                name: "Transacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FkIdUsuario = table.Column<int>(type: "integer", nullable: false),
                    FkIdCarteira = table.Column<int>(type: "integer", nullable: false),
                    FkIdCategoria = table.Column<int>(type: "integer", nullable: true),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    FormaPagamento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    DataTransacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MesReferencia = table.Column<short>(type: "smallint", nullable: false),
                    AnoReferencia = table.Column<int>(type: "integer", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Transacoes_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transacoes_Carteiras_FkIdCarteira",
                        column: x => x.FkIdCarteira,
                        principalTable: "Carteiras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transacoes_Categorias_FkIdCategoria",
                        column: x => x.FkIdCategoria,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transacoes_Usuarios_FkIdUsuario",
                        column: x => x.FkIdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Lançamentos financeiros utilizados em extrato, dashboard e relatórios mensais.");

            migrationBuilder.CreateTable(
                name: "AportesMetas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FkIdMeta = table.Column<int>(type: "integer", nullable: false),
                    FkIdUsuario = table.Column<int>(type: "integer", nullable: false),
                    FkIdCarteira = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    DataAporte = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("AportesMetas_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AportesMetas_Carteiras_FkIdCarteira",
                        column: x => x.FkIdCarteira,
                        principalTable: "Carteiras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AportesMetas_Metas_FkIdMeta",
                        column: x => x.FkIdMeta,
                        principalTable: "Metas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AportesMetas_Usuarios_FkIdUsuario",
                        column: x => x.FkIdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Histórico de aportes realizados para metas financeiras.");

            migrationBuilder.CreateIndex(
                name: "IX_AportesMetas_FkIdCarteira",
                table: "AportesMetas",
                column: "FkIdCarteira");

            migrationBuilder.CreateIndex(
                name: "IX_AportesMetas_Meta_DataAporte",
                table: "AportesMetas",
                columns: new[] { "FkIdMeta", "DataAporte" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AportesMetas_Usuario_Carteira",
                table: "AportesMetas",
                columns: new[] { "FkIdUsuario", "FkIdCarteira" });

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_FkIdCarteira",
                table: "Assinaturas",
                column: "FkIdCarteira");

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_FkIdCategoria",
                table: "Assinaturas",
                column: "FkIdCategoria");

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_Usuario_Ativa_DiaCobranca",
                table: "Assinaturas",
                columns: new[] { "FkIdUsuario", "Ativa", "DiaCobranca" });

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_Usuario_Carteira",
                table: "Assinaturas",
                columns: new[] { "FkIdUsuario", "FkIdCarteira" });

            migrationBuilder.CreateIndex(
                name: "IX_Carteiras_Usuario_Ativo",
                table: "Carteiras",
                columns: new[] { "FkIdUsuario", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Usuario_Ativo",
                table: "Categorias",
                columns: new[] { "FkIdUsuario", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_Metas_FkIdCarteira",
                table: "Metas",
                column: "FkIdCarteira");

            migrationBuilder.CreateIndex(
                name: "IX_Metas_Usuario_Ativa_Concluida",
                table: "Metas",
                columns: new[] { "FkIdUsuario", "Ativa", "Concluida" });

            migrationBuilder.CreateIndex(
                name: "IX_Metas_Usuario_Mes_Ano",
                table: "Metas",
                columns: new[] { "FkIdUsuario", "AnoReferencia", "MesReferencia" });

            migrationBuilder.CreateIndex(
                name: "IX_OrcamentosMensais_FkIdCategoria",
                table: "OrcamentosMensais",
                column: "FkIdCategoria");

            migrationBuilder.CreateIndex(
                name: "IX_OrcamentosMensais_Usuario_Mes_Ano",
                table: "OrcamentosMensais",
                columns: new[] { "FkIdUsuario", "AnoReferencia", "MesReferencia" });

            migrationBuilder.CreateIndex(
                name: "UX_OrcamentosMensais_Usuario_Categoria_Mes_Ano",
                table: "OrcamentosMensais",
                columns: new[] { "FkIdUsuario", "FkIdCategoria", "AnoReferencia", "MesReferencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transacoes_FkIdCarteira",
                table: "Transacoes",
                column: "FkIdCarteira");

            migrationBuilder.CreateIndex(
                name: "IX_Transacoes_FkIdCategoria",
                table: "Transacoes",
                column: "FkIdCategoria");

            migrationBuilder.CreateIndex(
                name: "IX_Transacoes_Tipo_Mes_Ano",
                table: "Transacoes",
                columns: new[] { "Tipo", "AnoReferencia", "MesReferencia" });

            migrationBuilder.CreateIndex(
                name: "IX_Transacoes_Usuario_Carteira_Mes_Ano",
                table: "Transacoes",
                columns: new[] { "FkIdUsuario", "FkIdCarteira", "AnoReferencia", "MesReferencia" });

            migrationBuilder.CreateIndex(
                name: "IX_Transacoes_Usuario_Categoria_Mes_Ano",
                table: "Transacoes",
                columns: new[] { "FkIdUsuario", "FkIdCategoria", "AnoReferencia", "MesReferencia" });

            migrationBuilder.CreateIndex(
                name: "IX_Transacoes_Usuario_DataTransacao",
                table: "Transacoes",
                columns: new[] { "FkIdUsuario", "DataTransacao" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Transacoes_Usuario_Mes_Ano",
                table: "Transacoes",
                columns: new[] { "FkIdUsuario", "AnoReferencia", "MesReferencia" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AportesMetas");

            migrationBuilder.DropTable(
                name: "Assinaturas");

            migrationBuilder.DropTable(
                name: "OrcamentosMensais");

            migrationBuilder.DropTable(
                name: "Transacoes");

            migrationBuilder.DropTable(
                name: "Metas");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Carteiras");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
