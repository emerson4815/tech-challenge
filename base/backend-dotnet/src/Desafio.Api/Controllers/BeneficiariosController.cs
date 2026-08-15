using Desafio.Api.Api.Contratos;
using Desafio.Api.Aplicacao;
using Desafio.Api.Dominio;
using Desafio.Api.Infraestrutura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Desafio.Api.Controllers;

[ApiController]
[Route("beneficiarios")]
[Produces("application/json")]
public class BeneficiariosController(BeneficiarioServico servico) : ControllerBase
{


    [HttpPost]
    [ProducesResponseType<BeneficiarioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar(
     [FromBody] BeneficiarioRequestDados requisicao,
     CancellationToken cancellationToken)
    {

        var beneficiario = await servico.CriarAsync(requisicao, cancellationToken);

        return Created(
            $"/beneficiarios/{beneficiario.Id}",
            BeneficiarioResponse.De(beneficiario));
    }
    [HttpGet("{id:guid}")]
    [ProducesResponseType<BeneficiarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(
    Guid id,
    CancellationToken cancellationToken)
    {
        var beneficiario = await servico.ObterAsync(
            id,
            cancellationToken);

        return Ok(BeneficiarioResponse.De(beneficiario));
    }

    [HttpGet]
    [ProducesResponseType<PaginacaoResponse<BeneficiarioResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar([FromQuery] BeneficiarioFiltro filtro, CancellationToken cancellationToken)
    {
        var resultado = await servico.ListarAsync(filtro, cancellationToken);
        return Ok(resultado);
    }
    [HttpPut("{id:guid}")]
    [ProducesResponseType<BeneficiarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(
    Guid id,
    [FromBody] AtualizarBeneficiarioRequest requisicao,
    CancellationToken cancellationToken)
    {
        var dados = new AtualizarBeneficiarioDados(
            requisicao.NomeCompleto,
            requisicao.DataNascimento,
            requisicao.PlanoId,
            requisicao.Status);

        var beneficiario = await servico.AtualizarAsync(
            id,
            dados,
            cancellationToken);

        return Ok(BeneficiarioResponse.De(beneficiario));
    }
}
