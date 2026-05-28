using fanfnir_back.DTOs;
using fanfnir_back.Models;
using Microsoft.EntityFrameworkCore;

namespace fanfnir_back.Services;

public interface IUsuariosService
{
    Task<IReadOnlyList<UsuariosResponseDto>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<UsuariosResponseDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<ServiceResult<UsuariosResponseDto>> CreateAsync(UsuariosCreateDto dto, CancellationToken ct);
    Task<ServiceResult<UsuariosResponseDto>> UpdateAsync(int id, UsuariosUpdateDto dto, CancellationToken ct);
    Task<ServiceResult<object>> DeleteAsync(int id, CancellationToken ct);
}

public interface ICarteirasService
{
    Task<IReadOnlyList<CarteirasResponseDto>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<CarteirasResponseDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<ServiceResult<CarteirasResponseDto>> CreateAsync(CarteirasCreateDto dto, CancellationToken ct);
    Task<ServiceResult<CarteirasResponseDto>> UpdateAsync(int id, CarteirasUpdateDto dto, CancellationToken ct);
    Task<ServiceResult<object>> DeleteAsync(int id, CancellationToken ct);
}

public interface ICategoriasService
{
    Task<IReadOnlyList<CategoriasResponseDto>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<CategoriasResponseDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<ServiceResult<CategoriasResponseDto>> CreateAsync(CategoriasCreateDto dto, CancellationToken ct);
    Task<ServiceResult<CategoriasResponseDto>> UpdateAsync(int id, CategoriasUpdateDto dto, CancellationToken ct);
    Task<ServiceResult<object>> DeleteAsync(int id, CancellationToken ct);
}

public interface ITransacoesService
{
    Task<IReadOnlyList<TransacoesResponseDto>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<TransacoesResponseDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<ServiceResult<TransacoesResponseDto>> CreateAsync(TransacoesCreateDto dto, CancellationToken ct);
    Task<ServiceResult<TransacoesResponseDto>> UpdateAsync(int id, TransacoesUpdateDto dto, CancellationToken ct);
    Task<ServiceResult<object>> DeleteAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<TransacoesResponseDto>> GetPorMesAsync(int usuarioId, int mes, int ano, CancellationToken ct);
    Task<IReadOnlyList<CategoriaTotalDto>> GetPorCategoriaAsync(int usuarioId, int mes, int ano, CancellationToken ct);
}

public interface IAssinaturasService
{
    Task<IReadOnlyList<AssinaturasResponseDto>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<AssinaturasResponseDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<ServiceResult<AssinaturasResponseDto>> CreateAsync(AssinaturasCreateDto dto, CancellationToken ct);
    Task<ServiceResult<AssinaturasResponseDto>> UpdateAsync(int id, AssinaturasUpdateDto dto, CancellationToken ct);
    Task<ServiceResult<object>> DeleteAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<AssinaturasResponseDto>> GetAtivasAsync(int usuarioId, CancellationToken ct);
    Task<ServiceResult<TransacoesResponseDto>> GerarTransacaoMensalAsync(int id, int mes, int ano, CancellationToken ct);
}

public interface IOrcamentosMensaisService
{
    Task<IReadOnlyList<OrcamentosMensaisResponseDto>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<OrcamentosMensaisResponseDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<ServiceResult<OrcamentosMensaisResponseDto>> CreateAsync(OrcamentosMensaisCreateDto dto, CancellationToken ct);
    Task<ServiceResult<OrcamentosMensaisResponseDto>> UpdateAsync(int id, OrcamentosMensaisUpdateDto dto, CancellationToken ct);
    Task<ServiceResult<object>> DeleteAsync(int id, CancellationToken ct);
}

public interface IMetasService
{
    Task<IReadOnlyList<MetasResponseDto>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<MetasResponseDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<ServiceResult<MetasResponseDto>> CreateAsync(MetasCreateDto dto, CancellationToken ct);
    Task<ServiceResult<MetasResponseDto>> UpdateAsync(int id, MetasUpdateDto dto, CancellationToken ct);
    Task<ServiceResult<object>> DeleteAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<MetasResponseDto>> GetAtivasAsync(int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<MetasResponseDto>> GetMensaisAsync(int usuarioId, int mes, int ano, CancellationToken ct);
}

public interface IAportesMetasService
{
    Task<IReadOnlyList<AportesMetasResponseDto>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<AportesMetasResponseDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<ServiceResult<AportesMetasResponseDto>> CreateAsync(AportesMetasCreateDto dto, CancellationToken ct);
    Task<ServiceResult<AportesMetasResponseDto>> UpdateAsync(int id, AportesMetasUpdateDto dto, CancellationToken ct);
    Task<ServiceResult<object>> DeleteAsync(int id, CancellationToken ct);
}

public interface IDashboardService
{
    Task<DashboardMensalResponseDto> GetMensalAsync(int usuarioId, int mes, int ano, CancellationToken ct);
}

internal static class FinanceRules
{
    public static bool IsTipoTransacao(string tipo) => NormalizeTipoDb(tipo) is "RECEITA" or "DESPESA";
    public static bool IsTipoCarteira(string tipo) => tipo is "CONTA_CORRENTE" or "POUPANCA" or "DINHEIRO" or "CARTAO_CREDITO" or "INVESTIMENTO" or "OUTRA";
    public static bool IsTipoMeta(string tipo) => tipo is "economia_mensal" or "compra" or "reserva_emergencia" or "viagem" or "investimento" or "quitar_divida";
    public static bool IsValidMonth(int mes) => mes is >= 1 and <= 12;
    public static bool IsValidYear(int ano) => ano is >= 1900 and <= 9999;
    public static DateTime Now() => DateTime.Now;
    public static string NormalizeTipoCarteira(string tipo) => tipo.Trim().ToUpperInvariant();
    public static string NormalizeTipoDb(string tipo) => tipo.Trim().ToUpperInvariant();
    public static string NormalizeTipoApi(string tipo) => tipo.Trim().ToLowerInvariant();
}

internal static class Maps
{
    public static UsuariosResponseDto ToDto(this Usuarios e) => new(e.Id, e.Nome, e.Email, e.DataCriacao, e.DataAtualizacao);
    public static CarteirasResponseDto ToDto(this Carteiras e) => new(e.Id, e.FkIdUsuario, e.Nome, e.Tipo, e.SaldoInicial, e.Ativo, e.DataCriacao, e.DataAtualizacao);
    public static CategoriasResponseDto ToDto(this Categorias e) => new(e.Id, e.FkIdUsuario, e.Nome, FinanceRules.NormalizeTipoApi(e.Tipo), e.Cor, e.Icone, e.Ativo, e.DataCriacao, e.DataAtualizacao);
    public static TransacoesResponseDto ToDto(this Transacoes e) => new(e.Id, e.FkIdUsuario, e.FkIdCarteira, e.FkIdCategoria, e.Descricao, FinanceRules.NormalizeTipoApi(e.Tipo), e.Valor, e.FormaPagamento, e.DataTransacao, e.MesReferencia, e.AnoReferencia, e.Observacao, e.DataCriacao, e.DataAtualizacao);
    public static AssinaturasResponseDto ToDto(this Assinaturas e) => new(e.Id, e.FkIdUsuario, e.FkIdCategoria, e.FkIdCarteira, e.Nome, e.Valor, e.DiaCobranca, e.Ativa, e.DataInicio, e.DataFim, e.Observacao, e.DataCriacao, e.DataAtualizacao);
    public static OrcamentosMensaisResponseDto ToDto(this OrcamentosMensais e) => new(e.Id, e.FkIdUsuario, e.FkIdCategoria, e.MesReferencia, e.AnoReferencia, e.ValorLimite, e.DataCriacao, e.DataAtualizacao);
    public static MetasResponseDto ToDto(this Metas e) => new(e.Id, e.FkIdUsuario, e.FkIdCarteira, e.Nome, e.Descricao, e.TipoMeta, e.ValorAlvo, e.ValorAtual, e.MesReferencia, e.AnoReferencia, e.DataInicio, e.DataFim, e.Ativa, e.Concluida, e.DataCriacao, e.DataAtualizacao);
    public static AportesMetasResponseDto ToDto(this AportesMetas e) => new(e.Id, e.FkIdMeta, e.FkIdUsuario, e.FkIdCarteira, e.Valor, e.DataAporte, e.Observacao, e.DataCriacao, e.DataAtualizacao);
}

public sealed class UsuariosService(FafnirContext db) : IUsuariosService
{
    public async Task<IReadOnlyList<UsuariosResponseDto>> GetAllAsync(CancellationToken ct) =>
        await db.Usuarios.AsNoTracking().OrderBy(x => x.Id).Select(x => x.ToDto()).ToListAsync(ct);

    public async Task<ServiceResult<UsuariosResponseDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var item = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? ServiceResult<UsuariosResponseDto>.NotFound("Usuario nao encontrado.") : ServiceResult<UsuariosResponseDto>.Ok(item.ToDto());
    }

    public async Task<ServiceResult<UsuariosResponseDto>> CreateAsync(UsuariosCreateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.SenhaHash))
            return ServiceResult<UsuariosResponseDto>.BadRequest("Nome, Email e SenhaHash sao obrigatorios.");
        if (await db.Usuarios.AnyAsync(x => x.Email.ToLower() == dto.Email.ToLower(), ct))
            return ServiceResult<UsuariosResponseDto>.BadRequest("Email ja cadastrado.");

        var now = FinanceRules.Now();
        var item = new Usuarios { Nome = dto.Nome.Trim(), Email = dto.Email.Trim().ToLowerInvariant(), SenhaHash = PasswordHash.Hash(dto.SenhaHash), DataCriacao = now, DataAtualizacao = now };
        db.Usuarios.Add(item);
        await db.SaveChangesAsync(ct);
        return ServiceResult<UsuariosResponseDto>.Created(item.ToDto());
    }

    public async Task<ServiceResult<UsuariosResponseDto>> UpdateAsync(int id, UsuariosUpdateDto dto, CancellationToken ct)
    {
        var item = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult<UsuariosResponseDto>.NotFound("Usuario nao encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.SenhaHash))
            return ServiceResult<UsuariosResponseDto>.BadRequest("Nome, Email e SenhaHash sao obrigatorios.");
        if (await db.Usuarios.AnyAsync(x => x.Id != id && x.Email.ToLower() == dto.Email.ToLower(), ct))
            return ServiceResult<UsuariosResponseDto>.BadRequest("Email ja cadastrado.");
        item.Nome = dto.Nome.Trim(); item.Email = dto.Email.Trim().ToLowerInvariant(); item.SenhaHash = PasswordHash.Hash(dto.SenhaHash); item.DataAtualizacao = FinanceRules.Now();
        await db.SaveChangesAsync(ct);
        return ServiceResult<UsuariosResponseDto>.Ok(item.ToDto());
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id, CancellationToken ct)
    {
        var item = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult<object>.NotFound("Usuario nao encontrado.");
        db.Usuarios.Remove(item);
        await db.SaveChangesAsync(ct);
        return ServiceResult<object>.NoContent();
    }
}

public sealed class CarteirasService(FafnirContext db) : ICarteirasService
{
    public async Task<IReadOnlyList<CarteirasResponseDto>> GetAllAsync(CancellationToken ct) => await db.Carteiras.AsNoTracking().OrderBy(x => x.Id).Select(x => x.ToDto()).ToListAsync(ct);
    public async Task<ServiceResult<CarteirasResponseDto>> GetByIdAsync(int id, CancellationToken ct) => (await db.Carteiras.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)) is { } item ? ServiceResult<CarteirasResponseDto>.Ok(item.ToDto()) : ServiceResult<CarteirasResponseDto>.NotFound("Carteira nao encontrada.");

    public async Task<ServiceResult<CarteirasResponseDto>> CreateAsync(CarteirasCreateDto dto, CancellationToken ct)
    {
        var validation = await ValidateAsync(dto.FkIdUsuario, dto.Nome, dto.Tipo, ct);
        if (validation is not null) return ServiceResult<CarteirasResponseDto>.BadRequest(validation);
        var now = FinanceRules.Now();
        var item = new Carteiras { FkIdUsuario = dto.FkIdUsuario, Nome = dto.Nome.Trim(), Tipo = FinanceRules.NormalizeTipoCarteira(dto.Tipo), SaldoInicial = dto.SaldoInicial, Ativo = dto.Ativo, DataCriacao = now, DataAtualizacao = now };
        db.Carteiras.Add(item); await db.SaveChangesAsync(ct);
        return ServiceResult<CarteirasResponseDto>.Created(item.ToDto());
    }

    public async Task<ServiceResult<CarteirasResponseDto>> UpdateAsync(int id, CarteirasUpdateDto dto, CancellationToken ct)
    {
        var item = await db.Carteiras.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult<CarteirasResponseDto>.NotFound("Carteira nao encontrada.");
        var validation = await ValidateAsync(item.FkIdUsuario, dto.Nome, dto.Tipo, ct);
        if (validation is not null) return ServiceResult<CarteirasResponseDto>.BadRequest(validation);
        item.Nome = dto.Nome.Trim(); item.Tipo = FinanceRules.NormalizeTipoCarteira(dto.Tipo); item.SaldoInicial = dto.SaldoInicial; item.Ativo = dto.Ativo; item.DataAtualizacao = FinanceRules.Now();
        await db.SaveChangesAsync(ct);
        return ServiceResult<CarteirasResponseDto>.Ok(item.ToDto());
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id, CancellationToken ct)
    {
        var item = await db.Carteiras.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult<object>.NotFound("Carteira nao encontrada.");
        item.Ativo = false; item.DataAtualizacao = FinanceRules.Now();
        await db.SaveChangesAsync(ct);
        return ServiceResult<object>.NoContent();
    }

    private async Task<string?> ValidateAsync(int usuarioId, string nome, string tipo, CancellationToken ct)
    {
        if (usuarioId <= 0) return "Sessao invalida. Entre novamente antes de cadastrar uma carteira.";
        if (string.IsNullOrWhiteSpace(nome)) return "Nome da carteira e obrigatorio.";
        if (string.IsNullOrWhiteSpace(tipo)) return "Tipo da carteira e obrigatorio.";
        if (nome.Trim().Length > 120) return "Nome da carteira deve ter no maximo 120 caracteres.";
        if (tipo.Trim().Length > 30) return "Tipo da carteira deve ter no maximo 30 caracteres.";
        if (!FinanceRules.IsTipoCarteira(FinanceRules.NormalizeTipoCarteira(tipo))) return "Tipo da carteira deve ser CONTA_CORRENTE, POUPANCA, DINHEIRO, CARTAO_CREDITO, INVESTIMENTO ou OUTRA.";
        if (!await db.Usuarios.AnyAsync(x => x.Id == usuarioId, ct)) return "Usuario informado nao existe.";
        return null;
    }
}

public sealed class CategoriasService(FafnirContext db) : ICategoriasService
{
    public async Task<IReadOnlyList<CategoriasResponseDto>> GetAllAsync(CancellationToken ct) => await db.Categorias.AsNoTracking().OrderBy(x => x.Id).Select(x => x.ToDto()).ToListAsync(ct);
    public async Task<ServiceResult<CategoriasResponseDto>> GetByIdAsync(int id, CancellationToken ct) => (await db.Categorias.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)) is { } item ? ServiceResult<CategoriasResponseDto>.Ok(item.ToDto()) : ServiceResult<CategoriasResponseDto>.NotFound("Categoria nao encontrada.");

    public async Task<ServiceResult<CategoriasResponseDto>> CreateAsync(CategoriasCreateDto dto, CancellationToken ct)
    {
        if (dto.FkIdUsuario <= 0 || string.IsNullOrWhiteSpace(dto.Nome) || !FinanceRules.IsTipoTransacao(dto.Tipo)) return ServiceResult<CategoriasResponseDto>.BadRequest("FkIdUsuario, Nome e Tipo receita/despesa sao obrigatorios.");
        if (!await db.Usuarios.AnyAsync(x => x.Id == dto.FkIdUsuario, ct)) return ServiceResult<CategoriasResponseDto>.BadRequest("Usuario informado nao existe.");
        var now = FinanceRules.Now();
        var item = new Categorias { FkIdUsuario = dto.FkIdUsuario, Nome = dto.Nome.Trim(), Tipo = FinanceRules.NormalizeTipoDb(dto.Tipo), Cor = dto.Cor, Icone = dto.Icone, Ativo = dto.Ativo, DataCriacao = now, DataAtualizacao = now };
        db.Categorias.Add(item); await db.SaveChangesAsync(ct);
        return ServiceResult<CategoriasResponseDto>.Created(item.ToDto());
    }

    public async Task<ServiceResult<CategoriasResponseDto>> UpdateAsync(int id, CategoriasUpdateDto dto, CancellationToken ct)
    {
        var item = await db.Categorias.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult<CategoriasResponseDto>.NotFound("Categoria nao encontrada.");
        if (string.IsNullOrWhiteSpace(dto.Nome) || !FinanceRules.IsTipoTransacao(dto.Tipo)) return ServiceResult<CategoriasResponseDto>.BadRequest("Nome e Tipo receita/despesa sao obrigatorios.");
        item.Nome = dto.Nome.Trim(); item.Tipo = FinanceRules.NormalizeTipoDb(dto.Tipo); item.Cor = dto.Cor; item.Icone = dto.Icone; item.Ativo = dto.Ativo; item.DataAtualizacao = FinanceRules.Now();
        await db.SaveChangesAsync(ct);
        return ServiceResult<CategoriasResponseDto>.Ok(item.ToDto());
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id, CancellationToken ct)
    {
        var item = await db.Categorias.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult<object>.NotFound("Categoria nao encontrada.");
        item.Ativo = false; item.DataAtualizacao = FinanceRules.Now();
        await db.SaveChangesAsync(ct);
        return ServiceResult<object>.NoContent();
    }
}

public sealed class TransacoesService(FafnirContext db) : ITransacoesService
{
    public async Task<IReadOnlyList<TransacoesResponseDto>> GetAllAsync(CancellationToken ct) => await db.Transacoes.AsNoTracking().OrderByDescending(x => x.DataTransacao).Select(x => x.ToDto()).ToListAsync(ct);
    public async Task<ServiceResult<TransacoesResponseDto>> GetByIdAsync(int id, CancellationToken ct) => (await db.Transacoes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)) is { } item ? ServiceResult<TransacoesResponseDto>.Ok(item.ToDto()) : ServiceResult<TransacoesResponseDto>.NotFound("Transacao nao encontrada.");

    public async Task<ServiceResult<TransacoesResponseDto>> CreateAsync(TransacoesCreateDto dto, CancellationToken ct)
    {
        var validation = await ValidateTransacaoAsync(dto.FkIdUsuario, dto.FkIdCarteira, dto.FkIdCategoria, dto.Tipo, dto.Valor, dto.Descricao, ct);
        if (validation is not null) return ServiceResult<TransacoesResponseDto>.BadRequest(validation);
        var carteira = await db.Carteiras.FirstAsync(x => x.Id == dto.FkIdCarteira, ct);
        var now = FinanceRules.Now();
        var item = new Transacoes { FkIdUsuario = dto.FkIdUsuario, FkIdCarteira = dto.FkIdCarteira, FkIdCategoria = dto.FkIdCategoria, Descricao = dto.Descricao.Trim(), Tipo = FinanceRules.NormalizeTipoDb(dto.Tipo), Valor = dto.Valor, FormaPagamento = dto.FormaPagamento, DataTransacao = dto.DataTransacao, MesReferencia = dto.MesReferencia ?? (short)dto.DataTransacao.Month, AnoReferencia = dto.AnoReferencia ?? dto.DataTransacao.Year, Observacao = dto.Observacao, DataCriacao = now, DataAtualizacao = now };
        ApplyTransacao(carteira, item.Tipo, item.Valor);
        db.Transacoes.Add(item); await db.SaveChangesAsync(ct);
        return ServiceResult<TransacoesResponseDto>.Created(item.ToDto());
    }

    public async Task<ServiceResult<TransacoesResponseDto>> UpdateAsync(int id, TransacoesUpdateDto dto, CancellationToken ct)
    {
        var item = await db.Transacoes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult<TransacoesResponseDto>.NotFound("Transacao nao encontrada.");
        var validation = await ValidateTransacaoAsync(item.FkIdUsuario, dto.FkIdCarteira, dto.FkIdCategoria, dto.Tipo, dto.Valor, dto.Descricao, ct);
        if (validation is not null) return ServiceResult<TransacoesResponseDto>.BadRequest(validation);

        var oldCarteira = await db.Carteiras.FirstAsync(x => x.Id == item.FkIdCarteira, ct);
        ApplyTransacao(oldCarteira, item.Tipo, -item.Valor);
        var newCarteira = item.FkIdCarteira == dto.FkIdCarteira ? oldCarteira : await db.Carteiras.FirstAsync(x => x.Id == dto.FkIdCarteira, ct);
        item.FkIdCarteira = dto.FkIdCarteira; item.FkIdCategoria = dto.FkIdCategoria; item.Descricao = dto.Descricao.Trim(); item.Tipo = FinanceRules.NormalizeTipoDb(dto.Tipo); item.Valor = dto.Valor; item.FormaPagamento = dto.FormaPagamento; item.DataTransacao = dto.DataTransacao; item.MesReferencia = dto.MesReferencia ?? (short)dto.DataTransacao.Month; item.AnoReferencia = dto.AnoReferencia ?? dto.DataTransacao.Year; item.Observacao = dto.Observacao; item.DataAtualizacao = FinanceRules.Now();
        ApplyTransacao(newCarteira, item.Tipo, item.Valor);
        await db.SaveChangesAsync(ct);
        return ServiceResult<TransacoesResponseDto>.Ok(item.ToDto());
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id, CancellationToken ct)
    {
        var item = await db.Transacoes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult<object>.NotFound("Transacao nao encontrada.");
        var carteira = await db.Carteiras.FirstAsync(x => x.Id == item.FkIdCarteira, ct);
        ApplyTransacao(carteira, item.Tipo, -item.Valor);
        db.Transacoes.Remove(item); await db.SaveChangesAsync(ct);
        return ServiceResult<object>.NoContent();
    }

    public async Task<IReadOnlyList<TransacoesResponseDto>> GetPorMesAsync(int usuarioId, int mes, int ano, CancellationToken ct) =>
        await db.Transacoes.AsNoTracking().Where(x => x.FkIdUsuario == usuarioId && x.MesReferencia == mes && x.AnoReferencia == ano).OrderByDescending(x => x.DataTransacao).Select(x => x.ToDto()).ToListAsync(ct);

    public async Task<IReadOnlyList<CategoriaTotalDto>> GetPorCategoriaAsync(int usuarioId, int mes, int ano, CancellationToken ct) =>
        await TotaisCategoria(db, usuarioId, mes, ano, null, ct);

    internal static async Task<IReadOnlyList<CategoriaTotalDto>> TotaisCategoria(FafnirContext db, int usuarioId, int mes, int ano, string? tipo, CancellationToken ct) =>
        await db.Transacoes.AsNoTracking()
            .Where(x => x.FkIdUsuario == usuarioId && x.MesReferencia == mes && x.AnoReferencia == ano && (tipo == null || x.Tipo == FinanceRules.NormalizeTipoDb(tipo)))
            .GroupBy(x => new { x.FkIdCategoria, Categoria = x.FkIdCategoriaNavigation == null ? "Sem categoria" : x.FkIdCategoriaNavigation.Nome })
            .Select(g => new CategoriaTotalDto(g.Key.FkIdCategoria, g.Key.Categoria, g.Sum(x => x.Valor)))
            .ToListAsync(ct);

    private async Task<string?> ValidateTransacaoAsync(int usuarioId, int carteiraId, int? categoriaId, string tipo, decimal valor, string descricao, CancellationToken ct)
    {
        if (usuarioId <= 0 || carteiraId <= 0 || string.IsNullOrWhiteSpace(descricao)) return "Usuario, carteira e descricao sao obrigatorios.";
        if (!FinanceRules.IsTipoTransacao(tipo)) return "Tipo deve ser receita ou despesa.";
        if (valor <= 0) return "Valor deve ser maior que zero.";
        if (!await db.Usuarios.AnyAsync(x => x.Id == usuarioId, ct)) return "Usuario informado nao existe.";
        if (!await db.Carteiras.AnyAsync(x => x.Id == carteiraId && x.FkIdUsuario == usuarioId, ct)) return "Carteira informada nao existe para o usuario.";
        if (categoriaId.HasValue && !await db.Categorias.AnyAsync(x => x.Id == categoriaId.Value && (x.FkIdUsuario == usuarioId || x.FkIdUsuario == null), ct)) return "Categoria informada nao existe para o usuario.";
        return null;
    }

    private static void ApplyTransacao(Carteiras carteira, string tipo, decimal valor)
    {
        carteira.SaldoInicial += tipo == "RECEITA" ? valor : -valor;
        carteira.DataAtualizacao = FinanceRules.Now();
    }
}

public sealed class AssinaturasService(FafnirContext db, ITransacoesService transacoesService) : IAssinaturasService
{
    public async Task<IReadOnlyList<AssinaturasResponseDto>> GetAllAsync(CancellationToken ct) => await db.Assinaturas.AsNoTracking().OrderBy(x => x.Id).Select(x => x.ToDto()).ToListAsync(ct);
    public async Task<ServiceResult<AssinaturasResponseDto>> GetByIdAsync(int id, CancellationToken ct) => (await db.Assinaturas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)) is { } item ? ServiceResult<AssinaturasResponseDto>.Ok(item.ToDto()) : ServiceResult<AssinaturasResponseDto>.NotFound("Assinatura nao encontrada.");

    public async Task<ServiceResult<AssinaturasResponseDto>> CreateAsync(AssinaturasCreateDto dto, CancellationToken ct)
    {
        var validation = await ValidateAsync(dto.FkIdUsuario, dto.FkIdCategoria, dto.FkIdCarteira, dto.Nome, dto.Valor, dto.DiaCobranca, ct);
        if (validation is not null) return ServiceResult<AssinaturasResponseDto>.BadRequest(validation);
        var now = FinanceRules.Now();
        var item = new Assinaturas { FkIdUsuario = dto.FkIdUsuario, FkIdCategoria = dto.FkIdCategoria, FkIdCarteira = dto.FkIdCarteira, Nome = dto.Nome.Trim(), Valor = dto.Valor, DiaCobranca = dto.DiaCobranca, Ativa = dto.Ativa, DataInicio = dto.DataInicio, DataFim = dto.DataFim, Observacao = dto.Observacao, DataCriacao = now, DataAtualizacao = now };
        db.Assinaturas.Add(item); await db.SaveChangesAsync(ct);
        return ServiceResult<AssinaturasResponseDto>.Created(item.ToDto());
    }

    public async Task<ServiceResult<AssinaturasResponseDto>> UpdateAsync(int id, AssinaturasUpdateDto dto, CancellationToken ct)
    {
        var item = await db.Assinaturas.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult<AssinaturasResponseDto>.NotFound("Assinatura nao encontrada.");
        var validation = await ValidateAsync(item.FkIdUsuario, dto.FkIdCategoria, dto.FkIdCarteira, dto.Nome, dto.Valor, dto.DiaCobranca, ct);
        if (validation is not null) return ServiceResult<AssinaturasResponseDto>.BadRequest(validation);
        item.FkIdCategoria = dto.FkIdCategoria; item.FkIdCarteira = dto.FkIdCarteira; item.Nome = dto.Nome.Trim(); item.Valor = dto.Valor; item.DiaCobranca = dto.DiaCobranca; item.Ativa = dto.Ativa; item.DataInicio = dto.DataInicio; item.DataFim = dto.DataFim; item.Observacao = dto.Observacao; item.DataAtualizacao = FinanceRules.Now();
        await db.SaveChangesAsync(ct);
        return ServiceResult<AssinaturasResponseDto>.Ok(item.ToDto());
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id, CancellationToken ct)
    {
        var item = await db.Assinaturas.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult<object>.NotFound("Assinatura nao encontrada.");
        db.Assinaturas.Remove(item); await db.SaveChangesAsync(ct);
        return ServiceResult<object>.NoContent();
    }

    public async Task<IReadOnlyList<AssinaturasResponseDto>> GetAtivasAsync(int usuarioId, CancellationToken ct) =>
        await db.Assinaturas.AsNoTracking().Where(x => x.FkIdUsuario == usuarioId && x.Ativa).OrderBy(x => x.DiaCobranca).Select(x => x.ToDto()).ToListAsync(ct);

    public async Task<ServiceResult<TransacoesResponseDto>> GerarTransacaoMensalAsync(int id, int mes, int ano, CancellationToken ct)
    {
        if (!FinanceRules.IsValidMonth(mes) || !FinanceRules.IsValidYear(ano)) return ServiceResult<TransacoesResponseDto>.BadRequest("Mes ou ano invalido.");
        var assinatura = await db.Assinaturas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (assinatura is null) return ServiceResult<TransacoesResponseDto>.NotFound("Assinatura nao encontrada.");
        if (!assinatura.Ativa) return ServiceResult<TransacoesResponseDto>.BadRequest("Assinatura inativa.");
        var dia = Math.Min(assinatura.DiaCobranca, (short)DateTime.DaysInMonth(ano, mes));
        return await transacoesService.CreateAsync(new TransacoesCreateDto(assinatura.FkIdUsuario, assinatura.FkIdCarteira, assinatura.FkIdCategoria, assinatura.Nome, "despesa", assinatura.Valor, null, new DateTime(ano, mes, dia), (short)mes, ano, assinatura.Observacao), ct);
    }

    private async Task<string?> ValidateAsync(int usuarioId, int categoriaId, int carteiraId, string nome, decimal valor, short dia, CancellationToken ct)
    {
        if (usuarioId <= 0 || categoriaId <= 0 || carteiraId <= 0 || string.IsNullOrWhiteSpace(nome)) return "Usuario, categoria, carteira e nome sao obrigatorios.";
        if (valor <= 0) return "Valor deve ser maior que zero.";
        if (dia is < 1 or > 31) return "DiaCobranca deve estar entre 1 e 31.";
        if (!await db.Usuarios.AnyAsync(x => x.Id == usuarioId, ct)) return "Usuario informado nao existe.";
        if (!await db.Carteiras.AnyAsync(x => x.Id == carteiraId && x.FkIdUsuario == usuarioId, ct)) return "Carteira informada nao existe para o usuario.";
        if (!await db.Categorias.AnyAsync(x => x.Id == categoriaId && (x.FkIdUsuario == usuarioId || x.FkIdUsuario == null) && x.Tipo == "DESPESA", ct)) return "Categoria de despesa informada nao existe para o usuario.";
        return null;
    }
}

public sealed class OrcamentosMensaisService(FafnirContext db) : IOrcamentosMensaisService
{
    public async Task<IReadOnlyList<OrcamentosMensaisResponseDto>> GetAllAsync(CancellationToken ct) => await db.OrcamentosMensais.AsNoTracking().OrderBy(x => x.Id).Select(x => x.ToDto()).ToListAsync(ct);
    public async Task<ServiceResult<OrcamentosMensaisResponseDto>> GetByIdAsync(int id, CancellationToken ct) => (await db.OrcamentosMensais.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)) is { } item ? ServiceResult<OrcamentosMensaisResponseDto>.Ok(item.ToDto()) : ServiceResult<OrcamentosMensaisResponseDto>.NotFound("Orcamento nao encontrado.");

    public async Task<ServiceResult<OrcamentosMensaisResponseDto>> CreateAsync(OrcamentosMensaisCreateDto dto, CancellationToken ct)
    {
        var validation = await ValidateAsync(dto.FkIdUsuario, dto.FkIdCategoria, dto.MesReferencia, dto.AnoReferencia, dto.ValorLimite, 0, ct);
        if (validation is not null) return ServiceResult<OrcamentosMensaisResponseDto>.BadRequest(validation);
        var now = FinanceRules.Now();
        var item = new OrcamentosMensais { FkIdUsuario = dto.FkIdUsuario, FkIdCategoria = dto.FkIdCategoria, MesReferencia = dto.MesReferencia, AnoReferencia = dto.AnoReferencia, ValorLimite = dto.ValorLimite, DataCriacao = now, DataAtualizacao = now };
        db.OrcamentosMensais.Add(item); await db.SaveChangesAsync(ct);
        return ServiceResult<OrcamentosMensaisResponseDto>.Created(item.ToDto());
    }

    public async Task<ServiceResult<OrcamentosMensaisResponseDto>> UpdateAsync(int id, OrcamentosMensaisUpdateDto dto, CancellationToken ct)
    {
        var item = await db.OrcamentosMensais.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult<OrcamentosMensaisResponseDto>.NotFound("Orcamento nao encontrado.");
        var validation = await ValidateAsync(item.FkIdUsuario, dto.FkIdCategoria, dto.MesReferencia, dto.AnoReferencia, dto.ValorLimite, id, ct);
        if (validation is not null) return ServiceResult<OrcamentosMensaisResponseDto>.BadRequest(validation);
        item.FkIdCategoria = dto.FkIdCategoria; item.MesReferencia = dto.MesReferencia; item.AnoReferencia = dto.AnoReferencia; item.ValorLimite = dto.ValorLimite; item.DataAtualizacao = FinanceRules.Now();
        await db.SaveChangesAsync(ct);
        return ServiceResult<OrcamentosMensaisResponseDto>.Ok(item.ToDto());
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id, CancellationToken ct)
    {
        var item = await db.OrcamentosMensais.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult<object>.NotFound("Orcamento nao encontrado.");
        db.OrcamentosMensais.Remove(item); await db.SaveChangesAsync(ct);
        return ServiceResult<object>.NoContent();
    }

    private async Task<string?> ValidateAsync(int usuarioId, int categoriaId, short mes, int ano, decimal valor, int ignoreId, CancellationToken ct)
    {
        if (usuarioId <= 0 || categoriaId <= 0) return "Usuario e categoria sao obrigatorios.";
        if (valor <= 0) return "ValorLimite deve ser maior que zero.";
        if (!FinanceRules.IsValidMonth(mes) || !FinanceRules.IsValidYear(ano)) return "MesReferencia ou AnoReferencia invalido.";
        if (!await db.Usuarios.AnyAsync(x => x.Id == usuarioId, ct)) return "Usuario informado nao existe.";
        if (!await db.Categorias.AnyAsync(x => x.Id == categoriaId && (x.FkIdUsuario == usuarioId || x.FkIdUsuario == null), ct)) return "Categoria informada nao existe para o usuario.";
        if (await db.OrcamentosMensais.AnyAsync(x => x.Id != ignoreId && x.FkIdUsuario == usuarioId && x.FkIdCategoria == categoriaId && x.MesReferencia == mes && x.AnoReferencia == ano, ct)) return "Ja existe orcamento para usuario, categoria, mes e ano.";
        return null;
    }
}

public sealed class MetasService(FafnirContext db) : IMetasService
{
    public async Task<IReadOnlyList<MetasResponseDto>> GetAllAsync(CancellationToken ct) => await db.Metas.AsNoTracking().OrderBy(x => x.Id).Select(x => x.ToDto()).ToListAsync(ct);
    public async Task<ServiceResult<MetasResponseDto>> GetByIdAsync(int id, CancellationToken ct) => (await db.Metas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)) is { } item ? ServiceResult<MetasResponseDto>.Ok(item.ToDto()) : ServiceResult<MetasResponseDto>.NotFound("Meta nao encontrada.");

    public async Task<ServiceResult<MetasResponseDto>> CreateAsync(MetasCreateDto dto, CancellationToken ct)
    {
        var validation = await ValidateAsync(dto.FkIdUsuario, dto.FkIdCarteira, dto.Nome, dto.TipoMeta, dto.ValorAlvo, dto.MesReferencia, dto.AnoReferencia, ct);
        if (validation is not null) return ServiceResult<MetasResponseDto>.BadRequest(validation);
        var now = FinanceRules.Now();
        var valorAtual = dto.ValorAtual ?? 0;
        var item = new Metas { FkIdUsuario = dto.FkIdUsuario, FkIdCarteira = dto.FkIdCarteira, Nome = dto.Nome.Trim(), Descricao = dto.Descricao, TipoMeta = dto.TipoMeta, ValorAlvo = dto.ValorAlvo, ValorAtual = valorAtual, MesReferencia = dto.MesReferencia, AnoReferencia = dto.AnoReferencia, DataInicio = dto.DataInicio, DataFim = dto.DataFim, Ativa = dto.Ativa, Concluida = valorAtual >= dto.ValorAlvo, DataCriacao = now, DataAtualizacao = now };
        db.Metas.Add(item); await db.SaveChangesAsync(ct);
        return ServiceResult<MetasResponseDto>.Created(item.ToDto());
    }

    public async Task<ServiceResult<MetasResponseDto>> UpdateAsync(int id, MetasUpdateDto dto, CancellationToken ct)
    {
        var item = await db.Metas.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult<MetasResponseDto>.NotFound("Meta nao encontrada.");
        var validation = await ValidateAsync(item.FkIdUsuario, dto.FkIdCarteira, dto.Nome, dto.TipoMeta, dto.ValorAlvo, dto.MesReferencia, dto.AnoReferencia, ct);
        if (validation is not null) return ServiceResult<MetasResponseDto>.BadRequest(validation);
        item.FkIdCarteira = dto.FkIdCarteira; item.Nome = dto.Nome.Trim(); item.Descricao = dto.Descricao; item.TipoMeta = dto.TipoMeta; item.ValorAlvo = dto.ValorAlvo; item.ValorAtual = dto.ValorAtual; item.MesReferencia = dto.MesReferencia; item.AnoReferencia = dto.AnoReferencia; item.DataInicio = dto.DataInicio; item.DataFim = dto.DataFim; item.Ativa = dto.Ativa; item.Concluida = dto.ValorAtual >= dto.ValorAlvo; item.DataAtualizacao = FinanceRules.Now();
        await db.SaveChangesAsync(ct);
        return ServiceResult<MetasResponseDto>.Ok(item.ToDto());
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id, CancellationToken ct)
    {
        var item = await db.Metas.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult<object>.NotFound("Meta nao encontrada.");
        db.Metas.Remove(item); await db.SaveChangesAsync(ct);
        return ServiceResult<object>.NoContent();
    }

    public async Task<IReadOnlyList<MetasResponseDto>> GetAtivasAsync(int usuarioId, CancellationToken ct) => await db.Metas.AsNoTracking().Where(x => x.FkIdUsuario == usuarioId && x.Ativa).OrderBy(x => x.Id).Select(x => x.ToDto()).ToListAsync(ct);
    public async Task<IReadOnlyList<MetasResponseDto>> GetMensaisAsync(int usuarioId, int mes, int ano, CancellationToken ct) => await db.Metas.AsNoTracking().Where(x => x.FkIdUsuario == usuarioId && x.MesReferencia == mes && x.AnoReferencia == ano).OrderBy(x => x.Id).Select(x => x.ToDto()).ToListAsync(ct);

    private async Task<string?> ValidateAsync(int usuarioId, int carteiraId, string nome, string tipoMeta, decimal valorAlvo, short? mes, int? ano, CancellationToken ct)
    {
        if (usuarioId <= 0 || carteiraId <= 0 || string.IsNullOrWhiteSpace(nome)) return "Usuario, carteira e nome sao obrigatorios.";
        if (valorAlvo <= 0) return "ValorAlvo deve ser maior que zero.";
        if (!FinanceRules.IsTipoMeta(tipoMeta)) return "TipoMeta invalido.";
        if (mes.HasValue && !FinanceRules.IsValidMonth(mes.Value)) return "MesReferencia invalido.";
        if (ano.HasValue && !FinanceRules.IsValidYear(ano.Value)) return "AnoReferencia invalido.";
        if (!await db.Usuarios.AnyAsync(x => x.Id == usuarioId, ct)) return "Usuario informado nao existe.";
        if (!await db.Carteiras.AnyAsync(x => x.Id == carteiraId && x.FkIdUsuario == usuarioId, ct)) return "Carteira informada nao existe para o usuario.";
        return null;
    }
}

public sealed class AportesMetasService(FafnirContext db) : IAportesMetasService
{
    public async Task<IReadOnlyList<AportesMetasResponseDto>> GetAllAsync(CancellationToken ct) => await db.AportesMetas.AsNoTracking().OrderByDescending(x => x.DataAporte).Select(x => x.ToDto()).ToListAsync(ct);
    public async Task<ServiceResult<AportesMetasResponseDto>> GetByIdAsync(int id, CancellationToken ct) => (await db.AportesMetas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)) is { } item ? ServiceResult<AportesMetasResponseDto>.Ok(item.ToDto()) : ServiceResult<AportesMetasResponseDto>.NotFound("Aporte nao encontrado.");

    public async Task<ServiceResult<AportesMetasResponseDto>> CreateAsync(AportesMetasCreateDto dto, CancellationToken ct)
    {
        var meta = await db.Metas.FirstOrDefaultAsync(x => x.Id == dto.FkIdMeta && x.FkIdUsuario == dto.FkIdUsuario, ct);
        var carteira = await db.Carteiras.FirstOrDefaultAsync(x => x.Id == dto.FkIdCarteira && x.FkIdUsuario == dto.FkIdUsuario, ct);
        if (meta is null || carteira is null || dto.Valor <= 0) return ServiceResult<AportesMetasResponseDto>.BadRequest("Meta, usuario, carteira e valor maior que zero sao obrigatorios.");
        var now = FinanceRules.Now();
        var item = new AportesMetas { FkIdMeta = dto.FkIdMeta, FkIdUsuario = dto.FkIdUsuario, FkIdCarteira = dto.FkIdCarteira, Valor = dto.Valor, DataAporte = dto.DataAporte, Observacao = dto.Observacao, DataCriacao = now, DataAtualizacao = now };
        ApplyAporte(meta, carteira, dto.Valor);
        db.AportesMetas.Add(item); await db.SaveChangesAsync(ct);
        return ServiceResult<AportesMetasResponseDto>.Created(item.ToDto());
    }

    public async Task<ServiceResult<AportesMetasResponseDto>> UpdateAsync(int id, AportesMetasUpdateDto dto, CancellationToken ct)
    {
        var item = await db.AportesMetas.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult<AportesMetasResponseDto>.NotFound("Aporte nao encontrado.");
        if (dto.Valor <= 0) return ServiceResult<AportesMetasResponseDto>.BadRequest("Valor deve ser maior que zero.");
        var meta = await db.Metas.FirstAsync(x => x.Id == item.FkIdMeta, ct);
        var oldCarteira = await db.Carteiras.FirstAsync(x => x.Id == item.FkIdCarteira, ct);
        ApplyAporte(meta, oldCarteira, -item.Valor);
        var newCarteira = item.FkIdCarteira == dto.FkIdCarteira ? oldCarteira : await db.Carteiras.FirstOrDefaultAsync(x => x.Id == dto.FkIdCarteira && x.FkIdUsuario == item.FkIdUsuario, ct);
        if (newCarteira is null) return ServiceResult<AportesMetasResponseDto>.BadRequest("Carteira informada nao existe para o usuario.");
        item.FkIdCarteira = dto.FkIdCarteira; item.Valor = dto.Valor; item.DataAporte = dto.DataAporte; item.Observacao = dto.Observacao; item.DataAtualizacao = FinanceRules.Now();
        ApplyAporte(meta, newCarteira, dto.Valor);
        await db.SaveChangesAsync(ct);
        return ServiceResult<AportesMetasResponseDto>.Ok(item.ToDto());
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id, CancellationToken ct)
    {
        var item = await db.AportesMetas.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult<object>.NotFound("Aporte nao encontrado.");
        var meta = await db.Metas.FirstAsync(x => x.Id == item.FkIdMeta, ct);
        var carteira = await db.Carteiras.FirstAsync(x => x.Id == item.FkIdCarteira, ct);
        ApplyAporte(meta, carteira, -item.Valor);
        db.AportesMetas.Remove(item); await db.SaveChangesAsync(ct);
        return ServiceResult<object>.NoContent();
    }

    private static void ApplyAporte(Metas meta, Carteiras carteira, decimal valor)
    {
        meta.ValorAtual += valor;
        meta.Concluida = meta.ValorAtual >= meta.ValorAlvo;
        meta.DataAtualizacao = FinanceRules.Now();
        carteira.SaldoInicial -= valor;
        carteira.DataAtualizacao = FinanceRules.Now();
    }
}

public sealed class DashboardService(FafnirContext db) : IDashboardService
{
    public async Task<DashboardMensalResponseDto> GetMensalAsync(int usuarioId, int mes, int ano, CancellationToken ct)
    {
        var transacoes = db.Transacoes.AsNoTracking().Where(x => x.FkIdUsuario == usuarioId && x.MesReferencia == mes && x.AnoReferencia == ano);
        var totalReceitas = await transacoes.Where(x => x.Tipo == "RECEITA").SumAsync(x => x.Valor, ct);
        
        // Fetch active subscriptions and calculate their total value
        var assinaturasAtivasLista = await db.Assinaturas.AsNoTracking()
            .Where(x => x.FkIdUsuario == usuarioId && x.Ativa)
            .Select(x => new { x.FkIdCategoria, CategoriaNome = x.FkIdCategoriaNavigation.Nome, x.Valor })
            .ToListAsync(ct);
        var totalAssinaturas = assinaturasAtivasLista.Sum(x => x.Valor);

        // Sum transaction despesas and active subscriptions
        var totalDespesasTransacoes = await transacoes.Where(x => x.Tipo == "DESPESA").SumAsync(x => x.Valor, ct);
        var totalDespesas = totalDespesasTransacoes + totalAssinaturas;

        var metasDoMes = await db.Metas.AsNoTracking().Where(x => x.FkIdUsuario == usuarioId && x.MesReferencia == mes && x.AnoReferencia == ano).ToListAsync(ct);
        
        // Combine transaction expenses and subscriptions by category
        var gastosPorCategoriaTransacoes = await TransacoesService.TotaisCategoria(db, usuarioId, mes, ano, "despesa", ct);
        var gastosPorCategoriaCombined = gastosPorCategoriaTransacoes
            .Select(x => new CategoriaTotalDto(x.FkIdCategoria, x.Categoria, x.Total))
            .ToList();
        
        if (totalAssinaturas > 0)
        {
            gastosPorCategoriaCombined.Add(new CategoriaTotalDto(null, "Assinaturas", totalAssinaturas));
        }

        var receitasPorCategoria = await TransacoesService.TotaisCategoria(db, usuarioId, mes, ano, "receita", ct);
        var metasAtivas = await db.Metas.AsNoTracking().Where(x => x.FkIdUsuario == usuarioId && x.Ativa).OrderBy(x => x.Id).Select(x => x.ToDto()).ToListAsync(ct);
        
        // Fetch budget limits and include active subscriptions in category spending totals
        var orcamentos = await db.OrcamentosMensais.AsNoTracking()
            .Include(x => x.FkIdCategoriaNavigation)
            .Where(x => x.FkIdUsuario == usuarioId && x.MesReferencia == mes && x.AnoReferencia == ano)
            .ToListAsync(ct);

        var orcamentosResponse = new List<OrcamentoCategoriaDto>();
        foreach (var orc in orcamentos)
        {
            var transSum = await db.Transacoes.Where(t => t.FkIdUsuario == usuarioId && t.FkIdCategoria == orc.FkIdCategoria && t.MesReferencia == mes && t.AnoReferencia == ano && t.Tipo == "DESPESA").SumAsync(t => t.Valor, ct);
            var subSum = assinaturasAtivasLista.Where(s => s.FkIdCategoria == orc.FkIdCategoria).Sum(s => s.Valor);
            var totalGasto = transSum + subSum;
            orcamentosResponse.Add(new OrcamentoCategoriaDto(
                orc.FkIdCategoria,
                orc.FkIdCategoriaNavigation?.Nome ?? "Sem categoria",
                orc.ValorLimite,
                totalGasto,
                orc.ValorLimite - totalGasto
            ));
        }

        return new DashboardMensalResponseDto(
            totalReceitas,
            totalDespesas,
            totalReceitas - totalDespesas, // TotalDespesas already includes subscriptions now
            totalAssinaturas,
            metasDoMes.Count,
            metasDoMes.Sum(x => x.ValorAtual),
            gastosPorCategoriaCombined,
            receitasPorCategoria,
            orcamentosResponse,
            metasAtivas);
    }
}
