using System;
using System.Collections.Generic;

namespace fanfnir_back.Models;

/// <summary>
/// Lançamentos financeiros utilizados em extrato, dashboard e relatórios mensais.
/// </summary>
public partial class Transacoes
{
    public int Id { get; set; }

    public int FkIdUsuario { get; set; }

    public int FkIdCarteira { get; set; }

    public int? FkIdCategoria { get; set; }

    public string Descricao { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public decimal Valor { get; set; }

    public string? FormaPagamento { get; set; }

    public DateTime DataTransacao { get; set; }

    public short MesReferencia { get; set; }

    public int AnoReferencia { get; set; }

    public string? Observacao { get; set; }

    public DateTime DataCriacao { get; set; }

    public DateTime DataAtualizacao { get; set; }

    public virtual Carteiras FkIdCarteiraNavigation { get; set; } = null!;

    public virtual Categorias? FkIdCategoriaNavigation { get; set; }

    public virtual Usuarios FkIdUsuarioNavigation { get; set; } = null!;
}
