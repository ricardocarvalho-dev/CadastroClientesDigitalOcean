using System.Text;
using System.Text.Json;
using CadastroClientes.Application.DTOs;
using CadastroClientes.Application.UseCases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CadastroClientes.Worker;

public class RabbitMqConsumerWorker : BackgroundService
{
    private const int MAX_TENTATIVAS = 3;

    private readonly ILogger<RabbitMqConsumerWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _queueName;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RabbitMqConsumerWorker(
        ILogger<RabbitMqConsumerWorker> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
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

        _channel.QueueDeclare(
            queue: $"{_queueName}-dlq",
            durable: true,
            exclusive: false,
            autoDelete: false);

        _channel.ExchangeDeclare("dlx", ExchangeType.Direct, durable: true);
        _channel.QueueBind($"{_queueName}-dlq", "dlx", $"{_queueName}-dlq");

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger.LogInformation("RabbitMQ Consumer iniciado. Fila: {Queue}", _queueName);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                _logger.LogInformation(
                    "Mensagem recebida. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);

                var evento = JsonSerializer.Deserialize<ClienteCriadoEvento>(body, _jsonOptions);

                if (evento is null)
                {
                    _logger.LogWarning("Mensagem não pôde ser deserializada. Acknowledging...");
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogInformation(
                    "Processando mensagem - Cliente {ClienteId}.", evento.ClienteId);

                using var scope = _scopeFactory.CreateScope();

                var emailUseCase = scope.ServiceProvider
                    .GetRequiredService<ProcessarEnvioEmailUseCase>();
                await emailUseCase.Executar(
                    evento.ClienteId, evento.Nome, evento.Email,
                    evento.Celular, evento.Mensagem);

                var smsUseCase = scope.ServiceProvider
                    .GetRequiredService<ProcessarEnvioSmsUseCase>();
                await smsUseCase.Executar(
                    evento.ClienteId, evento.Nome, evento.Email,
                    evento.Celular, evento.Mensagem);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);

                _logger.LogInformation(
                    "Mensagem processada com sucesso - Cliente {ClienteId}.", evento.ClienteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao processar mensagem. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);

                _channel.BasicNack(
                    ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
