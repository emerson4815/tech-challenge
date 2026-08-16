export interface ListaPaginada<T> {
  dados: T[];
  pagina: number;
  tamanho: number;
  total: number;
}
