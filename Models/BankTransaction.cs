using System;

namespace fanfnir_back.Models;

public partial class BankTransaction
{
    public int Id { get; set; }
    public int FkIdUsuario { get; set; }
    public int FkIdContaBancaria { get; set; }
    public string Provedor { get; set; } = null!;
    public string ProvedorTransacaoId { get; set; } = null!;
    public DateTime DataTransacao { get; set; }
    public decimal Valor { get; set; }
    public string Descricao { get; set; } = null!;
    public string? EstabelecimentoNome { get; set; }
    public string Tipo { get; set; } = null!; // CREDIT or DEBIT
    public string Moeda { get; set; } = null!;
    public int? FkIdCategoria { get; set; }
    public string? Metadata { get; set; } // Extra details stored as JSON string
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }

    public virtual Usuarios FkIdUsuarioNavigation { get; set; } = null!;
    public virtual BankAccount FkIdContaBancariaNavigation { get; set; } = null!;
    public virtual Categorias? FkIdCategoriaNavigation { get; set; }
}
