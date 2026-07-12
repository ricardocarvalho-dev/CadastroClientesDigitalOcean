using CadastroClientes.Application.DTOs;
using CadastroClientes.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace CadastroClientes.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private readonly CriarClienteUseCase _criarUseCase;
    private readonly ListarClientesUseCase _listarUseCase;
    private readonly AtualizarClienteUseCase _atualizarUseCase;
    private readonly ExcluirClienteUseCase _excluirUseCase;
    private readonly ILogger<ClientesController> _logger;

    public ClientesController(
        CriarClienteUseCase criarUseCase,
        ListarClientesUseCase listarUseCase,
        AtualizarClienteUseCase atualizarUseCase,
        ExcluirClienteUseCase excluirUseCase,
        ILogger<ClientesController> logger)
    {
        _criarUseCase = criarUseCase;
        _listarUseCase = listarUseCase;
        _atualizarUseCase = atualizarUseCase;
        _excluirUseCase = excluirUseCase;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarClienteDto dto)
    {
        try
        {
            var cliente = await _criarUseCase.Executar(dto);
            return Ok(new { mensagem = "Cliente criado com sucesso", cliente });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao criar cliente: {ex.Message}");
            return StatusCode(500, new {
                mensagem = "Erro interno ao criar cliente",
                erroReal = ex.Message,
                erroInterno = ex.InnerException?.Message
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        try
        {
            var clientes = await _listarUseCase.Executar();
            return Ok(new { mensagem = "Clientes listados com sucesso", clientes });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao listar clientes: {ex.Message}");
            return StatusCode(500, new { mensagem = "Erro interno ao listar clientes", erroReal = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] CriarClienteDto dto)
    {
        try
        {
            var cliente = await _atualizarUseCase.Executar(id, dto);
            return Ok(new { mensagem = "Cliente atualizado com sucesso", cliente });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao atualizar cliente: {ex.Message}");
            return StatusCode(500, new { mensagem = "Erro interno ao atualizar cliente" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        try
        {
            await _excluirUseCase.Executar(id);
            return Ok(new { mensagem = "Cliente excluído com sucesso" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao excluir cliente: {ex.Message}");
            return StatusCode(500, new { mensagem = "Erro interno ao excluir cliente" });
        }
    }
}