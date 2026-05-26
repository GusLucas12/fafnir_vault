using fanfnir_back.DTOs;
using fanfnir_back.Services;
using Microsoft.AspNetCore.Mvc;

namespace fanfnir_back.Controllers;

[ApiController]
public abstract class FafnirControllerBase : ControllerBase
{
    protected ActionResult FromResult<T>(ServiceResult<T> result)
    {
        if (result.StatusCode == StatusCodes.Status204NoContent) return NoContent();
        if (result.Success) return StatusCode(result.StatusCode, result.Data);
        return StatusCode(result.StatusCode, new { message = result.Error });
    }

    protected ObjectResult Unexpected(Exception ex) => StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro inesperado.", detail = ex.Message });
}

[Route("api/usuarios")]
public sealed class UsuariosController(IUsuariosService service) : FafnirControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<UsuariosResponseDto>>> GetAll(CancellationToken ct) { try { return Ok(await service.GetAllAsync(ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpGet("{id:int}")] public async Task<ActionResult<UsuariosResponseDto>> GetById(int id, CancellationToken ct) { try { return FromResult(await service.GetByIdAsync(id, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPost] public async Task<ActionResult<UsuariosResponseDto>> Create(UsuariosCreateDto dto, CancellationToken ct) { try { return FromResult(await service.CreateAsync(dto, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPut("{id:int}")] public async Task<ActionResult<UsuariosResponseDto>> Update(int id, UsuariosUpdateDto dto, CancellationToken ct) { try { return FromResult(await service.UpdateAsync(id, dto, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { try { return FromResult(await service.DeleteAsync(id, ct)); } catch (Exception ex) { return Unexpected(ex); } }
}

[Route("api/auth")]
public sealed class AuthController(IAuthService service) : FafnirControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(AuthRegisterDto dto, CancellationToken ct)
    {
        try { return FromResult(await service.RegisterAsync(dto, ct)); }
        catch (Exception ex) { return Unexpected(ex); }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(AuthLoginDto dto, CancellationToken ct)
    {
        try { return FromResult(await service.LoginAsync(dto, ct)); }
        catch (Exception ex) { return Unexpected(ex); }
    }
}

[Route("api/carteiras")]
public sealed class CarteirasController(ICarteirasService service) : FafnirControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<CarteirasResponseDto>>> GetAll(CancellationToken ct) { try { return Ok(await service.GetAllAsync(ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpGet("{id:int}")] public async Task<ActionResult<CarteirasResponseDto>> GetById(int id, CancellationToken ct) { try { return FromResult(await service.GetByIdAsync(id, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPost] public async Task<ActionResult<CarteirasResponseDto>> Create(CarteirasCreateDto dto, CancellationToken ct) { try { return FromResult(await service.CreateAsync(dto, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPut("{id:int}")] public async Task<ActionResult<CarteirasResponseDto>> Update(int id, CarteirasUpdateDto dto, CancellationToken ct) { try { return FromResult(await service.UpdateAsync(id, dto, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { try { return FromResult(await service.DeleteAsync(id, ct)); } catch (Exception ex) { return Unexpected(ex); } }
}

[Route("api/categorias")]
public sealed class CategoriasController(ICategoriasService service) : FafnirControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<CategoriasResponseDto>>> GetAll(CancellationToken ct) { try { return Ok(await service.GetAllAsync(ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpGet("{id:int}")] public async Task<ActionResult<CategoriasResponseDto>> GetById(int id, CancellationToken ct) { try { return FromResult(await service.GetByIdAsync(id, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPost] public async Task<ActionResult<CategoriasResponseDto>> Create(CategoriasCreateDto dto, CancellationToken ct) { try { return FromResult(await service.CreateAsync(dto, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPut("{id:int}")] public async Task<ActionResult<CategoriasResponseDto>> Update(int id, CategoriasUpdateDto dto, CancellationToken ct) { try { return FromResult(await service.UpdateAsync(id, dto, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { try { return FromResult(await service.DeleteAsync(id, ct)); } catch (Exception ex) { return Unexpected(ex); } }
}

[Route("api/transacoes")]
public sealed class TransacoesController(ITransacoesService service) : FafnirControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<TransacoesResponseDto>>> GetAll(CancellationToken ct) { try { return Ok(await service.GetAllAsync(ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpGet("{id:int}")] public async Task<ActionResult<TransacoesResponseDto>> GetById(int id, CancellationToken ct) { try { return FromResult(await service.GetByIdAsync(id, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPost] public async Task<ActionResult<TransacoesResponseDto>> Create(TransacoesCreateDto dto, CancellationToken ct) { try { return FromResult(await service.CreateAsync(dto, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPut("{id:int}")] public async Task<ActionResult<TransacoesResponseDto>> Update(int id, TransacoesUpdateDto dto, CancellationToken ct) { try { return FromResult(await service.UpdateAsync(id, dto, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { try { return FromResult(await service.DeleteAsync(id, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpGet("por-mes")] public async Task<ActionResult<IReadOnlyList<TransacoesResponseDto>>> PorMes([FromQuery] int usuarioId, [FromQuery] int mes, [FromQuery] int ano, CancellationToken ct) { try { return Ok(await service.GetPorMesAsync(usuarioId, mes, ano, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpGet("por-categoria")] public async Task<ActionResult<IReadOnlyList<CategoriaTotalDto>>> PorCategoria([FromQuery] int usuarioId, [FromQuery] int mes, [FromQuery] int ano, CancellationToken ct) { try { return Ok(await service.GetPorCategoriaAsync(usuarioId, mes, ano, ct)); } catch (Exception ex) { return Unexpected(ex); } }
}

[Route("api/assinaturas")]
public sealed class AssinaturasController(IAssinaturasService service) : FafnirControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<AssinaturasResponseDto>>> GetAll(CancellationToken ct) { try { return Ok(await service.GetAllAsync(ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpGet("{id:int}")] public async Task<ActionResult<AssinaturasResponseDto>> GetById(int id, CancellationToken ct) { try { return FromResult(await service.GetByIdAsync(id, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPost] public async Task<ActionResult<AssinaturasResponseDto>> Create(AssinaturasCreateDto dto, CancellationToken ct) { try { return FromResult(await service.CreateAsync(dto, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPut("{id:int}")] public async Task<ActionResult<AssinaturasResponseDto>> Update(int id, AssinaturasUpdateDto dto, CancellationToken ct) { try { return FromResult(await service.UpdateAsync(id, dto, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { try { return FromResult(await service.DeleteAsync(id, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpGet("ativas")] public async Task<ActionResult<IReadOnlyList<AssinaturasResponseDto>>> Ativas([FromQuery] int usuarioId, CancellationToken ct) { try { return Ok(await service.GetAtivasAsync(usuarioId, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPost("{id:int}/gerar-transacao-mensal")] public async Task<ActionResult<TransacoesResponseDto>> GerarTransacaoMensal(int id, [FromQuery] int mes, [FromQuery] int ano, CancellationToken ct) { try { return FromResult(await service.GerarTransacaoMensalAsync(id, mes, ano, ct)); } catch (Exception ex) { return Unexpected(ex); } }
}

[Route("api/orcamentosmensais")]
public sealed class OrcamentosMensaisController(IOrcamentosMensaisService service) : FafnirControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<OrcamentosMensaisResponseDto>>> GetAll(CancellationToken ct) { try { return Ok(await service.GetAllAsync(ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpGet("{id:int}")] public async Task<ActionResult<OrcamentosMensaisResponseDto>> GetById(int id, CancellationToken ct) { try { return FromResult(await service.GetByIdAsync(id, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPost] public async Task<ActionResult<OrcamentosMensaisResponseDto>> Create(OrcamentosMensaisCreateDto dto, CancellationToken ct) { try { return FromResult(await service.CreateAsync(dto, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPut("{id:int}")] public async Task<ActionResult<OrcamentosMensaisResponseDto>> Update(int id, OrcamentosMensaisUpdateDto dto, CancellationToken ct) { try { return FromResult(await service.UpdateAsync(id, dto, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { try { return FromResult(await service.DeleteAsync(id, ct)); } catch (Exception ex) { return Unexpected(ex); } }
}

[Route("api/metas")]
public sealed class MetasController(IMetasService service) : FafnirControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<MetasResponseDto>>> GetAll(CancellationToken ct) { try { return Ok(await service.GetAllAsync(ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpGet("{id:int}")] public async Task<ActionResult<MetasResponseDto>> GetById(int id, CancellationToken ct) { try { return FromResult(await service.GetByIdAsync(id, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPost] public async Task<ActionResult<MetasResponseDto>> Create(MetasCreateDto dto, CancellationToken ct) { try { return FromResult(await service.CreateAsync(dto, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPut("{id:int}")] public async Task<ActionResult<MetasResponseDto>> Update(int id, MetasUpdateDto dto, CancellationToken ct) { try { return FromResult(await service.UpdateAsync(id, dto, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { try { return FromResult(await service.DeleteAsync(id, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpGet("ativas")] public async Task<ActionResult<IReadOnlyList<MetasResponseDto>>> Ativas([FromQuery] int usuarioId, CancellationToken ct) { try { return Ok(await service.GetAtivasAsync(usuarioId, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpGet("mensais")] public async Task<ActionResult<IReadOnlyList<MetasResponseDto>>> Mensais([FromQuery] int usuarioId, [FromQuery] int mes, [FromQuery] int ano, CancellationToken ct) { try { return Ok(await service.GetMensaisAsync(usuarioId, mes, ano, ct)); } catch (Exception ex) { return Unexpected(ex); } }
}

[Route("api/aportesmetas")]
public sealed class AportesMetasController(IAportesMetasService service) : FafnirControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<AportesMetasResponseDto>>> GetAll(CancellationToken ct) { try { return Ok(await service.GetAllAsync(ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpGet("{id:int}")] public async Task<ActionResult<AportesMetasResponseDto>> GetById(int id, CancellationToken ct) { try { return FromResult(await service.GetByIdAsync(id, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPost] public async Task<ActionResult<AportesMetasResponseDto>> Create(AportesMetasCreateDto dto, CancellationToken ct) { try { return FromResult(await service.CreateAsync(dto, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpPut("{id:int}")] public async Task<ActionResult<AportesMetasResponseDto>> Update(int id, AportesMetasUpdateDto dto, CancellationToken ct) { try { return FromResult(await service.UpdateAsync(id, dto, ct)); } catch (Exception ex) { return Unexpected(ex); } }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { try { return FromResult(await service.DeleteAsync(id, ct)); } catch (Exception ex) { return Unexpected(ex); } }
}

[Route("api/dashboard")]
public sealed class DashboardController(IDashboardService service) : FafnirControllerBase
{
    [HttpGet("mensal")]
    public async Task<ActionResult<DashboardMensalResponseDto>> Mensal([FromQuery] int usuarioId, [FromQuery] int mes, [FromQuery] int ano, CancellationToken ct)
    {
        try { return Ok(await service.GetMensalAsync(usuarioId, mes, ano, ct)); }
        catch (Exception ex) { return Unexpected(ex); }
    }
}
