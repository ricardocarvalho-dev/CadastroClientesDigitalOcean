using CadastroClientes.Application.Interfaces;
using CadastroClientes.Application.UseCases;
using CadastroClientes.Infrastructure.Data;
using CadastroClientes.Infrastructure.Email;
using CadastroClientes.Infrastructure.Repositories;
using CadastroClientes.Infrastructure.Sms;
using CadastroClientes.Worker;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Banco PostgreSQL (Supabase)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada.");

    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null));
});

// RabbitMQ (CloudAMQP)
var rabbitMqUri = builder.Configuration["RabbitMQ:Uri"]
    ?? throw new InvalidOperationException("RabbitMQ:Uri não configurado.");

var rabbitConnectionFactory = new ConnectionFactory
{
    Uri = new Uri(rabbitMqUri)
};
var rabbitConnection = rabbitConnectionFactory.CreateConnection();
builder.Services.AddSingleton<IConnection>(rabbitConnection);

// Email via Resend
builder.Services.AddHttpClient<IEmailService, ResendEmailService>();

// SMS via Twilio
builder.Services.AddSingleton<ISmsService, TwilioSmsService>();
builder.Services.AddScoped<ProcessarEnvioSmsUseCase>();

// Repositórios e Use Cases
builder.Services.AddScoped<IHistoricoEnvioMensagemRepository, HistoricoEnvioMensagemRepository>();
builder.Services.AddScoped<ProcessarEnvioEmailUseCase>();

// Worker RabbitMQ
builder.Services.AddHostedService<RabbitMqConsumerWorker>();

var host = builder.Build();
host.Run();
