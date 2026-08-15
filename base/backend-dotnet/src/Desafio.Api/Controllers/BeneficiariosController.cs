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
public class BeneficiariosController(AppDbContext _db, BeneficiarioServico servico) : ControllerBase
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

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lista = await _db.Beneficiarios.ToListAsync();

        // O plano é resolvido aqui, e não na consulta principal, porque o FindAsync usa o
        // cache do contexto: a listagem continua fazendo uma única ida ao banco, qualquer
        // que seja o tamanho da página.
        foreach (var b in lista)
        {
            b.Plano = await _db.Planos.FindAsync(b.PlanoId);
        }

        return Ok(lista);
    }
}
