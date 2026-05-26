using System;
using System.Collections.Generic;

namespace fanfnir_back.Models;

/// <summary>
/// Objetivos financeiros do usuário, com acompanhamento de progresso.
/// </summary>
public partial class Metas
{
    public int Id { get; set; }

    public int FkIdUsuario { get; set; }

    public int FkIdCarteira { get; set; }

    public string Nome { get; set; } = null!;

    public string? Descricao { get; set; }

    public string TipoMeta { get; set; } = null!;

    public decimal ValorAlvo { get; set; }

    public decimal ValorAtual { get; set; }

    public short? MesReferencia { get; set; }

    public int? AnoReferencia { get; set; }

    public DateTime DataInicio { get; set; }

    public DateTime? DataFim { get; set; }

    public bool Ativa { get; set; }

    public bool Concluida { get; set; }

    public DateTime DataCriacao { get; set; }

    public DateTime DataAtualizacao { get; set; }

    public virtual ICollection<AportesMetas> AportesMetas { get; set; } = new List<AportesMetas>();

    public virtual Carteiras FkIdCarteiraNavigation { get; set; } = null!;

    public virtual Usuarios FkIdUsuarioNavigation { get; set; } = null!;
}
