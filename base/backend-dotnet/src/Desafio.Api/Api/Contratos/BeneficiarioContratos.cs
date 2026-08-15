using Desafio.Api.Dominio;
using Microsoft.AspNetCore.Mvc;

namespace Desafio.Api.Api.Contratos;

public sealed record BeneficiarioResponse(
    Guid Id,
    string NomeCompleto,
    string Cpf,
    DateOnly DataNascimento,
    StatusBeneficiario Status,
    Guid PlanoId,
    DateTime DataCadastro)
{
    public static BeneficiarioResponse De(Beneficiario beneficiario) =>
        new(
            beneficiario.Id,
            beneficiario.NomeCompleto,
            beneficiario.Cpf,
            beneficiario.DataNascimento,
            beneficiario.Status,
            beneficiario.PlanoId,
            beneficiario.DataCadastro);
}
public sealed record BeneficiarioRequestDados(
    string? NomeCompleto,
    string? Cpf,
    DateOnly? DataNascimento,
    Guid? PlanoId
);
public sealed class BeneficiarioFiltro
{
    public int Pagina { get; init; } = 1;

    public int Tamanho { get; init; } = 10;

    public StatusBeneficiario? Status { get; init; }

    [FromQuery(Name = "plano_id")]
    public Guid? PlanoId { get; init; }
}
public sealed record AtualizarBeneficiarioRequest(
    string? NomeCompleto,
    DateOnly? DataNascimento,
    Guid? PlanoId,
    StatusBeneficiario? Status
);
public sealed record AtualizarBeneficiarioDados(
    string? NomeCompleto,
    DateOnly? DataNascimento,
    Guid? PlanoId,
    StatusBeneficiario? Status
);