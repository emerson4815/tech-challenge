export function formatarCpf(cpf: string): string {
  return cpf.replace(/^(\d{3})(\d{3})(\d{3})(\d{2})$/, '$1.$2.$3-$4');
}

export function formatarData(data: string): string {
  const [ano, mes, dia] = data.split('-');

  return `${dia}/${mes}/${ano}`;
}
