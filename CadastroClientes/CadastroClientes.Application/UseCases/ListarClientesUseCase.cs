using CadastroClientes.Application.DTOs;
using CadastroClientes.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CadastroClientes.Application.UseCases;

public class ListarClientesUseCase
{
    private readonly IClienteRepository _repository;
    private readonly ILogger<ListarClientesUseCase> _logger;

    public ListarClientesUseCase(
        IClienteRepository repository,
        ILogger<ListarClientesUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<ClienteDto>> Executar()
    {
        try
        {
            var clientes = await _repository.ListarTodosAsync();
            
            var clientesDto = clientes.Select(c => new ClienteDto
            {
                Id = c.Id,
                Nome = c.Nome,
                Email = c.Email,
                Celular = c.Celular,
                Mensagem = c.Mensagem,   // ← novo campo
                DataCadastro = c.DataCadastro
            });
            _logger.LogInformation($"Listados {clientesDto.Count()} clientes");
            return clientesDto;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao listar clientes: {ex.Message}");
            throw;
        }
    }
}
