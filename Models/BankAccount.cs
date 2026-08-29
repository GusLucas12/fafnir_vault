using System;
using System.Collections.Generic;

namespace fanfnir_back.Models;

public partial class BankAccount
{
    public int Id { get; set; }
    public int FkIdUsuario { get; set; }
    public int FkIdConexao { get; set; }
    public string Provedor { get; set; } = null!;
    public string ProvedorContaId { get; set; } = null!;
    public string InstituicaoId { get; set; } = null!;
    public string InstituicaoNome { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public string Moeda { get; set; } = null!;
    public decimal SaldoAtual { get; set; }
    public decimal? SaldoDisponivel { get; set; }
    public DateTime? UltimaSincronizacao { get; set; }
    public string Status { get; set; } = null!;
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }

    public virtual Usuarios FkIdUsuarioNavigation { get; set; } = null!;
    public virtual OpenFinanceConnection FkIdConexaoNavigation { get; set; } = null!;
    public virtual ICollection<BankTransaction> BankTransactions { get; set; } = new List<BankTransaction>();
}
