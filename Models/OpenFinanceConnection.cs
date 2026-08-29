using System;
using System.Collections.Generic;

namespace fanfnir_back.Models;

public partial class OpenFinanceConnection
{
    public int Id { get; set; }
    public int FkIdUsuario { get; set; }
    public string Provedor { get; set; } = null!;
    public string ProvedorItemId { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? InstituicaoId { get; set; }
    public string? InstituicaoNome { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }

    public virtual Usuarios FkIdUsuarioNavigation { get; set; } = null!;
    public virtual ICollection<BankAccount> BankAccounts { get; set; } = new List<BankAccount>();
}
