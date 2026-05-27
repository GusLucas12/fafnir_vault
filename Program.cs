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

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AngularDev");
app.MapControllers();

app.Run();
