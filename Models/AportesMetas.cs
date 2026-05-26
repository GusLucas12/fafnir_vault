using System;
using System.Collections.Generic;

namespace fanfnir_back.Models;

/// <summary>
/// Histórico de aportes realizados para metas financeiras.
/// </summary>
public partial class AportesMetas
{
    public int Id { get; set; }

    public int FkIdMeta { get; set; }

    public int FkIdUsuario { get; set; }

    public int FkIdCarteira { get; set; }

    public decimal Valor { get; set; }

    public DateTime DataAporte { get; set; }

    public string? Observacao { get; set; }

    public DateTime DataCriacao { get; set; }

    public DateTime DataAtualizacao { get; set; }

    public virtual Carteiras FkIdCarteiraNavigation { get; set; } = null!;

    public virtual Metas FkIdMetaNavigation { get; set; } = null!;

    public virtual Usuarios FkIdUsuarioNavigation { get; set; } = null!;
}
