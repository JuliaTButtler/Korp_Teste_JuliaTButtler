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

    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("EstoqueApi:TimeoutSeconds", 3)
    );
});

builder.Services.AddScoped<NotaFiscalService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
});

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

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();