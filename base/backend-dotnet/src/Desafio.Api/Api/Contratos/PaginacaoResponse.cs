namespace Desafio.Api.Api.Contratos;

public sealed record PaginacaoResponse<T>(
    IReadOnlyList<T> Dados,
    int Pagina,
    int Tamanho,
    int Total
);