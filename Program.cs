using fanfnir_back.DTOs;
using fanfnir_back.Models;
using fanfnir_back.Services;
using fanfnir_back.Services.AI;
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

builder.Services.AddHttpClient();

// Configurações de IA
builder.Services.Configure<AiOptions>(options =>
{
    builder.Configuration.GetSection(AiOptions.SectionName).Bind(options);
    
    // Suporte para variáveis de ambiente diretas
    var envApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    if (!string.IsNullOrWhiteSpace(envApiKey)) options.Gemini.ApiKey = envApiKey;

    var envProvider = Environment.GetEnvironmentVariable("AI_PROVIDER");
    if (!string.IsNullOrWhiteSpace(envProvider)) options.Provider = envProvider;

    var envModel = Environment.GetEnvironmentVariable("GEMINI_MODEL");
    if (!string.IsNullOrWhiteSpace(envModel)) options.Gemini.Model = envModel;
});

builder.Services.AddScoped<IUsuariosService, UsuariosService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICarteirasService, CarteirasService>();
builder.Services.AddScoped<ICdiService, CdiService>();
builder.Services.AddScoped<ICategoriasService, CategoriasService>();
builder.Services.AddScoped<ITransacoesService, TransacoesService>();
builder.Services.AddScoped<IAssinaturasService, AssinaturasService>();
builder.Services.AddScoped<IOrcamentosMensaisService, OrcamentosMensaisService>();
builder.Services.AddScoped<IMetasService, MetasService>();
builder.Services.AddScoped<IAportesMetasService, AportesMetasService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IOpenFinanceProvider, PluggyProvider>();
builder.Services.AddScoped<IOpenFinanceService, OpenFinanceService>();

// Serviços de IA Desacoplados
builder.Services.AddScoped<IAiProvider, GeminiProvider>();
builder.Services.AddScoped<IFafnirContextBuilder, FafnirContextBuilder>();
builder.Services.AddScoped<IFafnirService, FafnirService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var path = context.Request.Path;
    var query = context.Request.QueryString;
    Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - Requisicao Recebida: {method} {path}{query}");

    var watch = System.Diagnostics.Stopwatch.StartNew();
    await next();
    watch.Stop();

    var statusCode = context.Response.StatusCode;
    Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - Requisicao Processada: {method} {path} - Status: {statusCode} ({watch.ElapsedMilliseconds}ms)");
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AngularDev");
app.MapControllers();

app.Run();
