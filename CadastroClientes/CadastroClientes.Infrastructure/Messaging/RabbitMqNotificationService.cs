using System.Text;
using System.Text.Json;
using CadastroClientes.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace CadastroClientes.Infrastructure.Messaging;

public class RabbitMqNotificationService : IMessagingService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqNotificationService> _logger;
    private readonly string _queueName;

    public RabbitMqNotificationService(IConfiguration configuration,
        ILogger<RabbitMqNotificationService> logger)
    {
        _logger = logger;
        _queueName = configuration["RabbitMQ:QueueName"]
            ?? "fila-cadastro-clientes";

        var factory = new ConnectionFactory
        {
            Uri = new Uri(configuration["RabbitMQ:Uri"]
                ?? throw new InvalidOperationException("RabbitMQ:Uri não configurado."))
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dlx" },
                { "x-dead-letter-routing-key", $"{_queueName}-dlq" }
            });

        _logger.LogInformation("RabbitMQ Publisher conectado. Fila: {Queue}", _queueName);
    }

    public Task PublicarCriacaoClienteAsync(
        Guid clienteId, string nome, string email,
        string celular, string mensagem, string canal = "Email")
    {
        var payload = new
        {
            clienteId,
            nome,
            email,
            celular,
            mensagem,
            canal,
            dataCadastro = DateTime.UtcNow,
            tipo = "cliente.criado"
        };

        var json = JsonSerializer.Serialize(payload);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        _channel.BasicPublish(
            exchange: "",
            routingKey: _queueName,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Mensagem publicada no RabbitMQ. ClienteId: {ClienteId}", clienteId);

        return Task.CompletedTask;
    }
}
