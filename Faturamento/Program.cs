using Faturamento.Data;
using Faturamento.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(
        builder.Configuration.GetConnectionString("OracleConnection")
    )
);

builder.Services.AddHttpClient<EstoqueClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["EstoqueApi:BaseUrl"]
            ?? throw new InvalidOperationException(
                "A URL do microsserviço de estoque não foi configurada."
            )
    );
});

builder.Services.AddScoped<NotaFiscalService>();

builder.Services.AddAuthorization();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();