using System;
using System.Collections.Generic;

namespace fanfnir_back.Models;

/// <summary>
/// Limites mensais por categoria para controle de orçamento.
/// </summary>
public partial class OrcamentosMensais
{
    public int Id { get; set; }

    public int FkIdUsuario { get; set; }

    public int FkIdCategoria { get; set; }

    public short MesReferencia { get; set; }

    public int AnoReferencia { get; set; }

    public decimal ValorLimite { get; set; }

    public DateTime DataCriacao { get; set; }

    public DateTime DataAtualizacao { get; set; }

    public virtual Categorias FkIdCategoriaNavigation { get; set; } = null!;

    public virtual Usuarios FkIdUsuarioNavigation { get; set; } = null!;
}
