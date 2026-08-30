using System;
using System.Collections.Generic;

namespace fanfnir_back.Models;

/// <summary>
/// Carteiras, contas ou meios onde o saldo do usuário é controlado.
/// </summary>
public partial class Carteiras
{
    public int Id { get; set; }

    public int FkIdUsuario { get; set; }

    public string Nome { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public decimal SaldoInicial { get; set; }

    public bool Ativo { get; set; }

    public bool Rende { get; set; } = false;

    public string? TipoRendimento { get; set; }

    public decimal? TaxaRendimento { get; set; }

    public DateTime? UltimoProcessamentoRendimento { get; set; }

    public DateTime DataCriacao { get; set; }

    public DateTime DataAtualizacao { get; set; }

    public virtual ICollection<AportesMetas> AportesMetas { get; set; } = new List<AportesMetas>();

    public virtual ICollection<Assinaturas> Assinaturas { get; set; } = new List<Assinaturas>();

    public virtual Usuarios FkIdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<Transacoes> Transacoes { get; set; } = new List<Transacoes>();
}
