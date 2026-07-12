using CadastroClientes.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CadastroClientes.Application.UseCases;

public class ExcluirClienteUseCase
{
    private readonly IClienteRepository _repository;
    private readonly ILogger<ExcluirClienteUseCase> _logger;

    public ExcluirClienteUseCase(
        IClienteRepository repository,
        ILogger<ExcluirClienteUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Executar(Guid id)
    {
        var cliente = await _repository.ObterPorIdAsync(id)
            ?? throw new KeyNotFoundException($"Cliente {id} não encontrado");

        await _repository.ExcluirAsync(cliente);
        _logger.LogInformation($"Cliente {id} excluído com sucesso.");
    }
}