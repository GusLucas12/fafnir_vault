using System;
using System.Collections.Generic;

namespace fanfnir_back.Models;

/// <summary>
/// Usuários proprietários dos dados financeiros.
/// </summary>
public partial class Usuarios
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public string Email { get; set; } = null!;

    /// <summary>
    /// Hash da senha gerado pela aplicação; nunca persistir senha em texto puro.
    /// </summary>
    public string SenhaHash { get; set; } = null!;

    public DateTime DataCriacao { get; set; }

    public DateTime DataAtualizacao { get; set; }

    public virtual ICollection<AportesMetas> AportesMetas { get; set; } = new List<AportesMetas>();

    public virtual ICollection<Assinaturas> Assinaturas { get; set; } = new List<Assinaturas>();

    public virtual ICollection<Carteiras> Carteiras { get; set; } = new List<Carteiras>();

    public virtual ICollection<Categorias> Categorias { get; set; } = new List<Categorias>();

    public virtual ICollection<Metas> Metas { get; set; } = new List<Metas>();

    public virtual ICollection<OrcamentosMensais> OrcamentosMensais { get; set; } = new List<OrcamentosMensais>();

    public virtual ICollection<Transacoes> Transacoes { get; set; } = new List<Transacoes>();
}
