using CadastroClientes.Domain.Entities;

namespace CadastroClientes.Application.Interfaces;

public interface IClienteRepository
{
    Task<Cliente> CriarAsync(Cliente cliente);
    Task<IEnumerable<Cliente>> ListarTodosAsync();
    Task<Cliente?> ObterPorIdAsync(Guid id);
    Task<Cliente?> ObterPorEmailAsync(string email);
    Task<bool> EmailJaExisteAsync(string email);
    Task<Cliente> AtualizarAsync(Cliente cliente);
    Task ExcluirAsync(Cliente cliente);
}
