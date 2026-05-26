using System;
using System.Collections.Generic;

namespace fanfnir_back.Models;

/// <summary>
/// Categorias globais e personalizadas para classificação de receitas e despesas.
/// </summary>
public partial class Categorias
{
    public int Id { get; set; }

    /// <summary>
    /// Nulo indica categoria padrão/global do sistema.
    /// </summary>
    public int? FkIdUsuario { get; set; }

    public string Nome { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public string? Cor { get; set; }

    public string? Icone { get; set; }

    public bool Ativo { get; set; }

    public DateTime DataCriacao { get; set; }

    public DateTime DataAtualizacao { get; set; }

    public virtual ICollection<Assinaturas> Assinaturas { get; set; } = new List<Assinaturas>();

    public virtual Usuarios? FkIdUsuarioNavigation { get; set; }

    public virtual ICollection<OrcamentosMensais> OrcamentosMensais { get; set; } = new List<OrcamentosMensais>();

    public virtual ICollection<Transacoes> Transacoes { get; set; } = new List<Transacoes>();
}
