using Desafio.Api.Api.Contratos;
using Desafio.Api.Dominio;
using Desafio.Api.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Desafio.Api.Aplicacao;

public class BeneficiarioServico(AppDbContext db)
{

    private const string CodigoViolacaoDeUnicidade = "23505";

    private static bool CpfValido(string cpf)
    {
        if (cpf.Length != 11 || cpf.Any(c => !char.IsDigit(c)))
        {
            return false;
        }

        if (cpf.Distinct().Count() == 1)
        {
            return false;
        }

        var numeros = cpf.Select(c => c - '0').ToArray();

        var soma = 0;

        for (var i = 0; i < 9; i++)
        {
            soma += numeros[i] * (10 - i);
        }

        var resto = soma % 11;
        var primeiroDigito = resto < 2 ? 0 : 11 - resto;

        if (numeros[9] != primeiroDigito)
        {
            return false;
        }

        soma = 0;

        for (var i = 0; i < 10; i++)
        {
            soma += numeros[i] * (11 - i);
        }

        resto = soma % 11;
        var segundoDigito = resto < 2 ? 0 : 11 - resto;

        return numeros[10] == segundoDigito;
    }
    private async Task GarantirCpfUnicoAsync(
        string cpf,
        CancellationToken cancellationToken)
    {
        var existe = await db.Beneficiarios
         .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(b => b.Cpf == cpf, cancellationToken);

        if (existe)
        {
            throw new ConflitoException(
                "Já existe beneficiário cadastrado com esse CPF",
                [new DetalheErro("cpf", "duplicado")]);
        }
    }

    private static void ValidarDados(BeneficiarioRequestDados dados)
    {
        var detalhes = new List<DetalheErro>();

        if (string.IsNullOrWhiteSpace(dados.NomeCompleto))
            detalhes.Add(new DetalheErro("nome_completo", "obrigatorio"));

        if (string.IsNullOrWhiteSpace(dados.Cpf))
        {
            detalhes.Add(new DetalheErro("cpf", "obrigatorio"));
        }
        else if (!CpfValido(dados.Cpf))
        {
            detalhes.Add(new DetalheErro("cpf", "invalido"));
        }

        if (dados.DataNascimento is null)
        {
            detalhes.Add(new DetalheErro("data_nascimento", "obrigatorio"));
        }
        else if (dados.DataNascimento > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            detalhes.Add(new DetalheErro("data_nascimento", "invalido"));
        }

        if (dados.PlanoId is null || dados.PlanoId == Guid.Empty)
        {
            detalhes.Add(new DetalheErro("plano_id", "obrigatorio"));
        }

        if (detalhes.Count > 0)
            throw new ValidacaoException(
                "Dados do beneficiário inválidos",
                detalhes);
    }
    private async Task GarantirPlanoExisteAsync(
            Guid planoId,
            CancellationToken cancellationToken)
    {
        var existe = await db.Planos
            .AsNoTracking()
            .AnyAsync(p => p.Id == planoId, cancellationToken);

        if (!existe)
        {
            throw new NaoProcessavelException(
                "Plano informado não existe",
                [new DetalheErro("plano_id", "nao_encontrado")]);
        }
    }
    private async Task SalvarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (EhViolacaoDeUnicidade(excecao))
        {
            throw new ConflitoException(
            "Já existe beneficiário cadastrado com esse CPF",
            [new DetalheErro("cpf", "duplicado")]);
        }
    }
    public async Task<Beneficiario> CriarAsync(
        BeneficiarioRequestDados dados,
        CancellationToken cancellationToken)
    {
        ValidarDados(dados);

        await GarantirPlanoExisteAsync(dados.PlanoId!.Value, cancellationToken);
        await GarantirCpfUnicoAsync(dados.Cpf!, cancellationToken);

        var beneficiario = new Beneficiario
        {
            Id = Guid.NewGuid(),
            NomeCompleto = dados.NomeCompleto!.Trim(),
            Cpf = dados.Cpf!,
            DataNascimento = dados.DataNascimento!.Value,
            PlanoId = dados.PlanoId!.Value,
            Status = StatusBeneficiario.ATIVO,
            DataCadastro = DateTime.UtcNow
        };

        db.Beneficiarios.Add(beneficiario);

        await SalvarAsync(cancellationToken);

        return beneficiario;
    }
    public async Task<Beneficiario> ObterAsync(
    Guid id,
    CancellationToken cancellationToken)
    {
        return await db.Beneficiarios
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException(
                "Beneficiário não encontrado");
    }
    private static bool EhViolacaoDeUnicidade(DbUpdateException excecao) =>
    excecao.InnerException is PostgresException postgres &&
    postgres.SqlState == CodigoViolacaoDeUnicidade;
    private static void ValidarFiltro(BeneficiarioFiltro filtro)
    {
        var detalhes = new List<DetalheErro>();

        if (filtro.Pagina < 1)
        {
            detalhes.Add(new DetalheErro("pagina", "invalido"));
        }

        if (filtro.Tamanho < 1 || filtro.Tamanho > 100)
        {
            detalhes.Add(new DetalheErro("tamanho", "invalido"));
        }

        if (detalhes.Count > 0)
        {
            throw new ValidacaoException(
                "Parâmetros de paginação inválidos",
                detalhes);
        }
    }
    private static void ValidarDadosAtualizacao(
    AtualizarBeneficiarioDados dados)
    {
        var detalhes = new List<DetalheErro>();

        if (dados.NomeCompleto is not null &&
            string.IsNullOrWhiteSpace(dados.NomeCompleto))
        {
            detalhes.Add(
                new DetalheErro("nome_completo", "invalido"));
        }

        if (dados.DataNascimento is not null &&
            dados.DataNascimento > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            detalhes.Add(
                new DetalheErro("data_nascimento", "invalido"));
        }

        if (dados.PlanoId == Guid.Empty)
        {
            detalhes.Add(
                new DetalheErro("plano_id", "invalido"));
        }

        if (detalhes.Count > 0)
        {
            throw new ValidacaoException(
                "Dados do beneficiário inválidos",
                detalhes);
        }
    }
    public async Task<PaginacaoResponse<BeneficiarioResponse>> ListarAsync(
    BeneficiarioFiltro filtro,
    CancellationToken cancellationToken)
    {
        ValidarFiltro(filtro);

        var consulta = db.Beneficiarios
            .AsNoTracking()
            .AsQueryable();

        if (filtro.Status is not null)
        {
            consulta = consulta.Where(b => b.Status == filtro.Status);
        }

        if (filtro.PlanoId is not null)
        {
            consulta = consulta.Where(b => b.PlanoId == filtro.PlanoId);
        }

        var total = await consulta.CountAsync(cancellationToken);

        var beneficiarios = await consulta
            .OrderBy(b => b.Id)
            .Skip((filtro.Pagina - 1) * filtro.Tamanho)
            .Take(filtro.Tamanho)
            .ToListAsync(cancellationToken);

        var dados = beneficiarios
            .Select(BeneficiarioResponse.De)
            .ToList();

        return new PaginacaoResponse<BeneficiarioResponse>(
            dados,
            filtro.Pagina,
            filtro.Tamanho,
            total);
    }
    public async Task<Beneficiario> AtualizarAsync(
    Guid id,
    AtualizarBeneficiarioDados dados,
    CancellationToken cancellationToken)
    {
        var beneficiario = await db.Beneficiarios
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException(
                "Beneficiário não encontrado");

        ValidarDadosAtualizacao(dados);

        if (dados.PlanoId is not null)
        {
            await GarantirPlanoExisteAsync(
                dados.PlanoId.Value,
                cancellationToken);
        }

        if (beneficiario.Status == StatusBeneficiario.INATIVO)
        {
            var tentouAlterarDadosCadastrais =
                dados.NomeCompleto is not null ||
                dados.DataNascimento is not null ||
                dados.PlanoId is not null;

            if (tentouAlterarDadosCadastrais)
            {
                throw new ConflitoException(
                    "Beneficiário inativo não pode ter dados cadastrais alterados");
            }
        }

        if (dados.NomeCompleto is not null)
        {
            beneficiario.NomeCompleto = dados.NomeCompleto.Trim();
        }

        if (dados.DataNascimento is not null)
        {
            beneficiario.DataNascimento = dados.DataNascimento.Value;
        }

        if (dados.PlanoId is not null)
        {
            beneficiario.PlanoId = dados.PlanoId.Value;
        }

        if (dados.Status is not null)
        {
            beneficiario.Status = dados.Status.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return beneficiario;
    }
    public async Task ExcluirAsync(
    Guid id,
    CancellationToken cancellationToken)
    {
        var beneficiario = await ObterAsync(id, cancellationToken);

        beneficiario.Excluir();

        await SalvarAsync(cancellationToken);
    }
}

