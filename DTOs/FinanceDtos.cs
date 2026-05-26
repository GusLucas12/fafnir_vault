namespace fanfnir_back.DTOs;

public record UsuariosCreateDto(string Nome, string Email, string SenhaHash);
public record UsuariosUpdateDto(string Nome, string Email, string SenhaHash);
public record UsuariosResponseDto(int Id, string Nome, string Email, DateTime DataCriacao, DateTime DataAtualizacao);
public record AuthRegisterDto(string Nome, string Email, string Senha);
public record AuthLoginDto(string Email, string Senha);
public record AuthResponseDto(int Id, string Nome, string Email, string Token, DateTime ExpiraEm);

public record CarteirasCreateDto(int FkIdUsuario, string Nome, string Tipo, decimal SaldoInicial, bool Ativo = true);
public record CarteirasUpdateDto(string Nome, string Tipo, decimal SaldoInicial, bool Ativo);
public record CarteirasResponseDto(int Id, int FkIdUsuario, string Nome, string Tipo, decimal SaldoInicial, bool Ativo, DateTime DataCriacao, DateTime DataAtualizacao);

public record CategoriasCreateDto(int FkIdUsuario, string Nome, string Tipo, string? Cor, string? Icone, bool Ativo = true);
public record CategoriasUpdateDto(string Nome, string Tipo, string? Cor, string? Icone, bool Ativo);
public record CategoriasResponseDto(int Id, int? FkIdUsuario, string Nome, string Tipo, string? Cor, string? Icone, bool Ativo, DateTime DataCriacao, DateTime DataAtualizacao);

public record TransacoesCreateDto(int FkIdUsuario, int FkIdCarteira, int FkIdCategoria, string Descricao, string Tipo, decimal Valor, string? FormaPagamento, DateTime DataTransacao, short? MesReferencia, int? AnoReferencia, string? Observacao);
public record TransacoesUpdateDto(int FkIdCarteira, int FkIdCategoria, string Descricao, string Tipo, decimal Valor, string? FormaPagamento, DateTime DataTransacao, short? MesReferencia, int? AnoReferencia, string? Observacao);
public record TransacoesResponseDto(int Id, int FkIdUsuario, int FkIdCarteira, int? FkIdCategoria, string Descricao, string Tipo, decimal Valor, string? FormaPagamento, DateTime DataTransacao, short MesReferencia, int AnoReferencia, string? Observacao, DateTime DataCriacao, DateTime DataAtualizacao);
public record CategoriaTotalDto(int? FkIdCategoria, string Categoria, decimal Total);

public record AssinaturasCreateDto(int FkIdUsuario, int FkIdCategoria, int FkIdCarteira, string Nome, decimal Valor, short DiaCobranca, bool Ativa, DateTime DataInicio, DateTime? DataFim, string? Observacao);
public record AssinaturasUpdateDto(int FkIdCategoria, int FkIdCarteira, string Nome, decimal Valor, short DiaCobranca, bool Ativa, DateTime DataInicio, DateTime? DataFim, string? Observacao);
public record AssinaturasResponseDto(int Id, int FkIdUsuario, int FkIdCategoria, int FkIdCarteira, string Nome, decimal Valor, short DiaCobranca, bool Ativa, DateTime DataInicio, DateTime? DataFim, string? Observacao, DateTime DataCriacao, DateTime DataAtualizacao);

public record OrcamentosMensaisCreateDto(int FkIdUsuario, int FkIdCategoria, short MesReferencia, int AnoReferencia, decimal ValorLimite);
public record OrcamentosMensaisUpdateDto(int FkIdCategoria, short MesReferencia, int AnoReferencia, decimal ValorLimite);
public record OrcamentosMensaisResponseDto(int Id, int FkIdUsuario, int FkIdCategoria, short MesReferencia, int AnoReferencia, decimal ValorLimite, DateTime DataCriacao, DateTime DataAtualizacao);
public record OrcamentoCategoriaDto(int FkIdCategoria, string Categoria, decimal ValorLimite, decimal TotalGasto, decimal SaldoDisponivel);

public record MetasCreateDto(int FkIdUsuario, int FkIdCarteira, string Nome, string? Descricao, string TipoMeta, decimal ValorAlvo, decimal? ValorAtual, short? MesReferencia, int? AnoReferencia, DateTime DataInicio, DateTime? DataFim, bool Ativa);
public record MetasUpdateDto(int FkIdCarteira, string Nome, string? Descricao, string TipoMeta, decimal ValorAlvo, decimal ValorAtual, short? MesReferencia, int? AnoReferencia, DateTime DataInicio, DateTime? DataFim, bool Ativa);
public record MetasResponseDto(int Id, int FkIdUsuario, int FkIdCarteira, string Nome, string? Descricao, string TipoMeta, decimal ValorAlvo, decimal ValorAtual, short? MesReferencia, int? AnoReferencia, DateTime DataInicio, DateTime? DataFim, bool Ativa, bool Concluida, DateTime DataCriacao, DateTime DataAtualizacao);

public record AportesMetasCreateDto(int FkIdMeta, int FkIdUsuario, int FkIdCarteira, decimal Valor, DateTime DataAporte, string? Observacao);
public record AportesMetasUpdateDto(int FkIdCarteira, decimal Valor, DateTime DataAporte, string? Observacao);
public record AportesMetasResponseDto(int Id, int FkIdMeta, int FkIdUsuario, int FkIdCarteira, decimal Valor, DateTime DataAporte, string? Observacao, DateTime DataCriacao, DateTime DataAtualizacao);

public record DashboardMensalResponseDto(
    decimal TotalReceitas,
    decimal TotalDespesas,
    decimal SaldoPrevisto,
    decimal TotalAssinaturasAtivas,
    int TotalMetasDoMes,
    decimal TotalGuardadoMetas,
    IReadOnlyList<CategoriaTotalDto> GastosPorCategoria,
    IReadOnlyList<CategoriaTotalDto> ReceitasPorCategoria,
    IReadOnlyList<OrcamentoCategoriaDto> OrcamentosPorCategoria,
    IReadOnlyList<MetasResponseDto> MetasAtivas);
