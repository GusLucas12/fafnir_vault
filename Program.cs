using fanfnir_back.Models;
using fanfnir_back.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Valor invalido." : error.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new
        {
            message = "Requisicao invalida. Confira os campos enviados.",
            errors
        });
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddDbContext<FafnirContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FafnirConnection")));

builder.Services.AddScoped<IUsuariosService, UsuariosService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICarteirasService, CarteirasService>();
builder.Services.AddScoped<ICategoriasService, CategoriasService>();
builder.Services.AddScoped<ITransacoesService, TransacoesService>();
builder.Services.AddScoped<IAssinaturasService, AssinaturasService>();
builder.Services.AddScoped<IOrcamentosMensaisService, OrcamentosMensaisService>();
builder.Services.AddScoped<IMetasService, MetasService>();
builder.Services.AddScoped<IAportesMetasService, AportesMetasService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<FafnirContext>();
        db.Database.OpenConnection();
        using (var cmd = db.Database.GetDbConnection().CreateCommand())
        {
            cmd.CommandText = "SELECT a.\"Id\", a.\"FkIdUsuario\", a.\"FkIdCategoria\", c.\"Nome\", a.\"Nome\", a.\"Valor\", a.\"Ativa\" FROM \"Assinaturas\" a LEFT JOIN \"Categorias\" c ON a.\"FkIdCategoria\" = c.\"Id\";";
            using (var reader = cmd.ExecuteReader())
            {
                Console.WriteLine("=== ASSINATURAS + CATEGORIAS DATA ===");
                while (reader.Read())
                {
                    Console.WriteLine($"ID: {reader.GetInt32(0)}, UsuarioId: {reader.GetInt32(1)}, CatId: {reader.GetInt32(2)}, CatNome: {(reader.IsDBNull(3) ? "NULL" : reader.GetString(3))}, Nome: {reader.GetString(4)}, Valor: {reader.GetDecimal(5)}, Ativa: {reader.GetBoolean(6)}");
                }
                Console.WriteLine("=====================================");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DB Query Error: {ex.Message}");
    }
}

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<FafnirContext>();
        db.Database.OpenConnection();
        using (var cmd = db.Database.GetDbConnection().CreateCommand())
        {
            cmd.CommandText = "SELECT \"Id\", \"FkIdUsuario\", \"Nome\", \"Valor\", \"Ativa\" FROM \"Assinaturas\";";
            using (var reader = cmd.ExecuteReader())
            {
                Console.WriteLine("=== ASSINATURAS DATA ===");
                while (reader.Read())
                {
                    Console.WriteLine($"ID: {reader.GetInt32(0)}, UsuarioId: {reader.GetInt32(1)}, Nome: {reader.GetString(2)}, Valor: {reader.GetDecimal(3)}, Ativa: {reader.GetBoolean(4)}");
                }
                Console.WriteLine("========================");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DB Query Error: {ex.Message}");
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AngularDev");
app.MapControllers();

app.Run();
