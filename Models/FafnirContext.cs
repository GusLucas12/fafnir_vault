using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace fanfnir_back.Models;

public partial class FafnirContext : DbContext
{
    public FafnirContext()
    {
    }

    public FafnirContext(DbContextOptions<FafnirContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AportesMetas> AportesMetas { get; set; }

    public virtual DbSet<Assinaturas> Assinaturas { get; set; }

    public virtual DbSet<Carteiras> Carteiras { get; set; }

    public virtual DbSet<Categorias> Categorias { get; set; }

    public virtual DbSet<Metas> Metas { get; set; }

    public virtual DbSet<OrcamentosMensais> OrcamentosMensais { get; set; }

    public virtual DbSet<Transacoes> Transacoes { get; set; }

    public virtual DbSet<Usuarios> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AportesMetas>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("AportesMetas_pkey");

            entity.ToTable(tb => tb.HasComment("Histórico de aportes realizados para metas financeiras."));

            entity.HasIndex(e => new { e.FkIdMeta, e.DataAporte }, "IX_AportesMetas_Meta_DataAporte").IsDescending(false, true);

            entity.HasIndex(e => new { e.FkIdUsuario, e.FkIdCarteira }, "IX_AportesMetas_Usuario_Carteira");

            entity.Property(e => e.DataAporte).HasColumnType("timestamp without time zone");
            entity.Property(e => e.DataAtualizacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DataCriacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Valor).HasPrecision(14, 2);

            entity.HasOne(d => d.FkIdCarteiraNavigation).WithMany(p => p.AportesMetas)
                .HasForeignKey(d => d.FkIdCarteira)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.FkIdMetaNavigation).WithMany(p => p.AportesMetas).HasForeignKey(d => d.FkIdMeta);

            entity.HasOne(d => d.FkIdUsuarioNavigation).WithMany(p => p.AportesMetas).HasForeignKey(d => d.FkIdUsuario);
        });

        modelBuilder.Entity<Assinaturas>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Assinaturas_pkey");

            entity.ToTable(tb => tb.HasComment("Cobranças recorrentes do usuário, úteis para previsão financeira."));

            entity.HasIndex(e => new { e.FkIdUsuario, e.Ativa, e.DiaCobranca }, "IX_Assinaturas_Usuario_Ativa_DiaCobranca");

            entity.HasIndex(e => new { e.FkIdUsuario, e.FkIdCarteira }, "IX_Assinaturas_Usuario_Carteira");

            entity.Property(e => e.Ativa).HasDefaultValue(true);
            entity.Property(e => e.DataAtualizacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DataCriacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DataFim).HasColumnType("timestamp without time zone");
            entity.Property(e => e.DataInicio).HasColumnType("timestamp without time zone");
            entity.Property(e => e.Nome).HasMaxLength(150);
            entity.Property(e => e.Valor).HasPrecision(14, 2);

            entity.HasOne(d => d.FkIdCarteiraNavigation).WithMany(p => p.Assinaturas)
                .HasForeignKey(d => d.FkIdCarteira)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.FkIdCategoriaNavigation).WithMany(p => p.Assinaturas)
                .HasForeignKey(d => d.FkIdCategoria)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.FkIdUsuarioNavigation).WithMany(p => p.Assinaturas).HasForeignKey(d => d.FkIdUsuario);
        });

        modelBuilder.Entity<Carteiras>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Carteiras_pkey");

            entity.ToTable(tb => tb.HasComment("Carteiras, contas ou meios onde o saldo do usuário é controlado."));

            entity.HasIndex(e => new { e.FkIdUsuario, e.Ativo }, "IX_Carteiras_Usuario_Ativo");

            entity.Property(e => e.Ativo).HasDefaultValue(true);
            entity.Property(e => e.DataAtualizacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DataCriacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Nome).HasMaxLength(120);
            entity.Property(e => e.SaldoInicial)
                .HasPrecision(14, 2)
                .HasDefaultValueSql("0.00");
            entity.Property(e => e.Tipo).HasMaxLength(30);

            entity.HasOne(d => d.FkIdUsuarioNavigation).WithMany(p => p.Carteiras).HasForeignKey(d => d.FkIdUsuario);
        });

        modelBuilder.Entity<Categorias>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Categorias_pkey");

            entity.ToTable(tb => tb.HasComment("Categorias globais e personalizadas para classificação de receitas e despesas."));

            entity.HasIndex(e => new { e.FkIdUsuario, e.Ativo }, "IX_Categorias_Usuario_Ativo");

            entity.Property(e => e.Ativo).HasDefaultValue(true);
            entity.Property(e => e.Cor).HasMaxLength(20);
            entity.Property(e => e.DataAtualizacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DataCriacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.FkIdUsuario).HasComment("Nulo indica categoria padrão/global do sistema.");
            entity.Property(e => e.Icone).HasMaxLength(80);
            entity.Property(e => e.Nome).HasMaxLength(120);
            entity.Property(e => e.Tipo).HasMaxLength(20);

            entity.HasOne(d => d.FkIdUsuarioNavigation).WithMany(p => p.Categorias)
                .HasForeignKey(d => d.FkIdUsuario)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Metas>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Metas_pkey");

            entity.ToTable(tb => tb.HasComment("Objetivos financeiros do usuário, com acompanhamento de progresso."));

            entity.HasIndex(e => new { e.FkIdUsuario, e.Ativa, e.Concluida }, "IX_Metas_Usuario_Ativa_Concluida");

            entity.HasIndex(e => new { e.FkIdUsuario, e.AnoReferencia, e.MesReferencia }, "IX_Metas_Usuario_Mes_Ano");

            entity.Property(e => e.Ativa).HasDefaultValue(true);
            entity.Property(e => e.Concluida).HasDefaultValue(false);
            entity.Property(e => e.DataAtualizacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DataCriacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DataFim).HasColumnType("timestamp without time zone");
            entity.Property(e => e.DataInicio).HasColumnType("timestamp without time zone");
            entity.Property(e => e.Nome).HasMaxLength(150);
            entity.Property(e => e.TipoMeta).HasMaxLength(30);
            entity.Property(e => e.ValorAlvo).HasPrecision(14, 2);
            entity.Property(e => e.ValorAtual)
                .HasPrecision(14, 2)
                .HasDefaultValueSql("0.00");

            entity.HasOne(d => d.FkIdCarteiraNavigation).WithMany(p => p.Metas)
                .HasForeignKey(d => d.FkIdCarteira)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.FkIdUsuarioNavigation).WithMany(p => p.Metas).HasForeignKey(d => d.FkIdUsuario);
        });

        modelBuilder.Entity<OrcamentosMensais>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("OrcamentosMensais_pkey");

            entity.ToTable(tb => tb.HasComment("Limites mensais por categoria para controle de orçamento."));

            entity.HasIndex(e => new { e.FkIdUsuario, e.AnoReferencia, e.MesReferencia }, "IX_OrcamentosMensais_Usuario_Mes_Ano");

            entity.HasIndex(e => new { e.FkIdUsuario, e.FkIdCategoria, e.AnoReferencia, e.MesReferencia }, "UX_OrcamentosMensais_Usuario_Categoria_Mes_Ano").IsUnique();

            entity.Property(e => e.DataAtualizacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DataCriacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.ValorLimite).HasPrecision(14, 2);

            entity.HasOne(d => d.FkIdCategoriaNavigation).WithMany(p => p.OrcamentosMensais)
                .HasForeignKey(d => d.FkIdCategoria)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.FkIdUsuarioNavigation).WithMany(p => p.OrcamentosMensais).HasForeignKey(d => d.FkIdUsuario);
        });

        modelBuilder.Entity<Transacoes>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Transacoes_pkey");

            entity.ToTable(tb => tb.HasComment("Lançamentos financeiros utilizados em extrato, dashboard e relatórios mensais."));

            entity.HasIndex(e => new { e.Tipo, e.AnoReferencia, e.MesReferencia }, "IX_Transacoes_Tipo_Mes_Ano");

            entity.HasIndex(e => new { e.FkIdUsuario, e.FkIdCarteira, e.AnoReferencia, e.MesReferencia }, "IX_Transacoes_Usuario_Carteira_Mes_Ano");

            entity.HasIndex(e => new { e.FkIdUsuario, e.FkIdCategoria, e.AnoReferencia, e.MesReferencia }, "IX_Transacoes_Usuario_Categoria_Mes_Ano");

            entity.HasIndex(e => new { e.FkIdUsuario, e.DataTransacao }, "IX_Transacoes_Usuario_DataTransacao").IsDescending(false, true);

            entity.HasIndex(e => new { e.FkIdUsuario, e.AnoReferencia, e.MesReferencia }, "IX_Transacoes_Usuario_Mes_Ano");

            entity.Property(e => e.DataAtualizacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DataCriacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DataTransacao).HasColumnType("timestamp without time zone");
            entity.Property(e => e.Descricao).HasMaxLength(200);
            entity.Property(e => e.FormaPagamento).HasMaxLength(30);
            entity.Property(e => e.Tipo).HasMaxLength(20);
            entity.Property(e => e.Valor).HasPrecision(14, 2);

            entity.HasOne(d => d.FkIdCarteiraNavigation).WithMany(p => p.Transacoes)
                .HasForeignKey(d => d.FkIdCarteira)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.FkIdCategoriaNavigation).WithMany(p => p.Transacoes)
                .HasForeignKey(d => d.FkIdCategoria)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.FkIdUsuarioNavigation).WithMany(p => p.Transacoes).HasForeignKey(d => d.FkIdUsuario);
        });

        modelBuilder.Entity<Usuarios>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Usuarios_pkey");

            entity.ToTable(tb => tb.HasComment("Usuários proprietários dos dados financeiros."));

            entity.Property(e => e.DataAtualizacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DataCriacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Email).HasMaxLength(254);
            entity.Property(e => e.Nome).HasMaxLength(150);
            entity.Property(e => e.SenhaHash)
                .HasMaxLength(500)
                .HasComment("Hash da senha gerado pela aplicação; nunca persistir senha em texto puro.");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
