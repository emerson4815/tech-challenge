export type StatusBeneficiario = 'ATIVO' | 'INATIVO';

export interface Beneficiario {
  id: string;
  nome_completo: string;
  cpf: string;
  data_nascimento: string;
  status: StatusBeneficiario;
  plano_id: string;
  data_cadastro: string;
}

export interface CriarBeneficiarioRequest {
  nome_completo: string;
  cpf: string;
  data_nascimento: string;
  plano_id: string;
}

export interface AtualizarBeneficiarioRequest {
  nome_completo?: string;
  data_nascimento?: string;
  plano_id?: string;
  status?: StatusBeneficiario;
}
