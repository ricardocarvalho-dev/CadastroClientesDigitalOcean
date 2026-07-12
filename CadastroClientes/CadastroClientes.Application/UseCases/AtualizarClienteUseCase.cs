using CadastroClientes.Application.DTOs;
using CadastroClientes.Application.Interfaces;
using CadastroClientes.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CadastroClientes.Application.UseCases;

public class AtualizarClienteUseCase
{
    private readonly IClienteRepository _repository;
    private readonly CriarClienteDtoValidator _validator;
    private readonly ILogger<AtualizarClienteUseCase> _logger;

    public AtualizarClienteUseCase(
        IClienteRepository repository,
        ILogger<AtualizarClienteUseCase> logger)
    {
        _repository = repository;
        _validator = new CriarClienteDtoValidator();
        _logger = logger;
    }

    public async Task<ClienteDto> Executar(Guid id, CriarClienteDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new ValidationException(errors);
        }

        var cliente = await _repository.ObterPorIdAsync(id)
            ?? throw new KeyNotFoundException($"Cliente {id} não encontrado");

        // Verifica se o novo email já pertence a outro cliente
        if (!cliente.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailEmUso = await _repository.EmailJaExisteAsync(dto.Email);
            if (emailEmUso)
                throw new InvalidOperationException($"Email {dto.Email} já está cadastrado");
        }

        cliente.Nome = dto.Nome;
        cliente.Email = dto.Email;
        cliente.Celular = dto.Celular;
        cliente.Mensagem = dto.Mensagem;

        var atualizado = await _repository.AtualizarAsync(cliente);
        _logger.LogInformation($"Cliente {id} atualizado com sucesso.");

        return new ClienteDto
        {
            Id = atualizado.Id,
            Nome = atualizado.Nome,
            Email = atualizado.Email,
            Celular = atualizado.Celular,
            Mensagem = atualizado.Mensagem,
            DataCadastro = atualizado.DataCadastro
        };
    }
}