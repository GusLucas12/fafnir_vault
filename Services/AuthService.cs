using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using fanfnir_back.DTOs;
using fanfnir_back.Models;
using Microsoft.EntityFrameworkCore;

namespace fanfnir_back.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthResponseDto>> RegisterAsync(AuthRegisterDto dto, CancellationToken ct);
    Task<ServiceResult<AuthResponseDto>> LoginAsync(AuthLoginDto dto, CancellationToken ct);
}

public sealed class AuthService(FafnirContext db, IConfiguration configuration) : IAuthService
{
    public async Task<ServiceResult<AuthResponseDto>> RegisterAsync(AuthRegisterDto dto, CancellationToken ct)
    {
        var validation = ValidateCredentials(dto.Nome, dto.Email, dto.Senha, requireName: true);
        if (validation is not null) return ServiceResult<AuthResponseDto>.BadRequest(validation);

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(x => x.Email.ToLower() == email, ct))
            return ServiceResult<AuthResponseDto>.BadRequest("Email ja cadastrado.");

        var now = DateTime.Now;
        var user = new Usuarios
        {
            Nome = dto.Nome.Trim(),
            Email = email,
            SenhaHash = PasswordHash.Hash(dto.Senha),
            DataCriacao = now,
            DataAtualizacao = now
        };

        db.Usuarios.Add(user);
        await db.SaveChangesAsync(ct);

        return ServiceResult<AuthResponseDto>.Created(CreateResponse(user));
    }

    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(AuthLoginDto dto, CancellationToken ct)
    {
        var validation = ValidateCredentials(null, dto.Email, dto.Senha, requireName: false);
        if (validation is not null) return ServiceResult<AuthResponseDto>.BadRequest(validation);

        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await db.Usuarios.FirstOrDefaultAsync(x => x.Email.ToLower() == email, ct);
        if (user is null || !PasswordHash.Verify(dto.Senha, user.SenhaHash))
            return ServiceResult<AuthResponseDto>.BadRequest("Email ou senha invalidos.");

        if (PasswordHash.NeedsUpgrade(user.SenhaHash))
        {
            user.SenhaHash = PasswordHash.Hash(dto.Senha);
            user.DataAtualizacao = DateTime.Now;
            await db.SaveChangesAsync(ct);
        }

        return ServiceResult<AuthResponseDto>.Ok(CreateResponse(user));
    }

    private AuthResponseDto CreateResponse(Usuarios user)
    {
        var expires = DateTime.UtcNow.AddDays(30);
        return new AuthResponseDto(user.Id, user.Nome, user.Email, TokenSigner.Sign(user.Id, user.Email, expires, configuration), expires);
    }

    private static string? ValidateCredentials(string? nome, string email, string senha, bool requireName)
    {
        if (requireName && string.IsNullOrWhiteSpace(nome)) return "Nome e obrigatorio.";
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return "Email invalido.";
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 8) return "Senha deve ter pelo menos 8 caracteres.";
        if (!senha.Any(char.IsUpper)) return "Senha deve ter ao menos uma letra maiuscula.";
        if (!senha.Any(char.IsLower)) return "Senha deve ter ao menos uma letra minuscula.";
        if (!senha.Any(char.IsDigit)) return "Senha deve ter ao menos um numero.";
        return null;
    }
}

public static class PasswordHash
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const string Prefix = "PBKDF2";

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != Prefix) return false;
        if (!int.TryParse(parts[1], out var iterations)) return false;

        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public static bool NeedsUpgrade(string stored) => !stored.StartsWith($"{Prefix}${Iterations}$", StringComparison.Ordinal);
}

public static class TokenSigner
{
    public static string Sign(int userId, string email, DateTime expiresUtc, IConfiguration configuration)
    {
        var payload = JsonSerializer.Serialize(new { sub = userId, email, exp = new DateTimeOffset(expiresUtc).ToUnixTimeSeconds() });
        var payload64 = Base64Url(Encoding.UTF8.GetBytes(payload));
        var secret = configuration["Auth:TokenSecret"] ?? "fafnir-vault-dev-secret-change-me";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature64 = Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload64)));
        return $"{payload64}.{signature64}";
    }

    private static string Base64Url(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
