using System;
using System.Collections.Generic;

namespace fanfnir_back.Models;

/// <summary>
/// Cobranças recorrentes do usuário, úteis para previsão financeira.
/// </summary>
public partial class Assinaturas
{
    public int Id { get; set; }

    public int FkIdUsuario { get; set; }

    public int? FkIdCategoria { get; set; }

    public int FkIdCarteira { get; set; }

    public string Nome { get; set; } = null!;

    public decimal Valor { get; set; }

    public short DiaCobranca { get; set; }

    public bool Ativa { get; set; }

    public DateTime DataInicio { get; set; }

    public DateTime? DataFim { get; set; }

    public string? Observacao { get; set; }

    public DateTime DataCriacao { get; set; }

    public DateTime DataAtualizacao { get; set; }

    public virtual Carteiras FkIdCarteiraNavigation { get; set; } = null!;

    public virtual Categorias? FkIdCategoriaNavigation { get; set; }

    public virtual Usuarios FkIdUsuarioNavigation { get; set; } = null!;
}
