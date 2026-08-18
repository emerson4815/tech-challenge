import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';

import { mensagemDeErro } from '../../nucleo/api';
import { MensagemServico } from '../../nucleo/mensagem-servico';
import { Plano } from '../../planos/plano';
import { AtualizarBeneficiarioRequest, Beneficiario, StatusBeneficiario } from '../beneficiario';
import { BeneficiarioServico } from '../beneficiario-servico';
import { formatarCpf } from '../../nucleo/formatadores';

@Component({
  selector: 'app-beneficiario-formulario',
  imports: [FormsModule],
  templateUrl: './beneficiario-formulario.html',
  styleUrl: './beneficiario-formulario.css',
})
export class BeneficiarioFormulario {
  private readonly servico = inject(BeneficiarioServico);
  private readonly mensagemServico = inject(MensagemServico);
  private readonly destroyRef = inject(DestroyRef);

  readonly beneficiario = input<Beneficiario | null>(null);
  readonly planos = input<Plano[]>([]);

  readonly fechar = output<void>();
  readonly salvo = output<void>();

  protected readonly salvando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected nomeCompleto = '';
  protected cpf = '';
  protected dataNascimento = '';
  protected planoId = '';
  protected status: StatusBeneficiario = 'ATIVO';
  protected readonly formatarCpf = formatarCpf;

  constructor() {
    effect(() => {
      const beneficiario = this.beneficiario();

      if (beneficiario) {
        this.nomeCompleto = beneficiario.nome_completo;
        this.cpf = beneficiario.cpf;
        this.dataNascimento = beneficiario.data_nascimento;
        this.planoId = beneficiario.plano_id;
        this.status = beneficiario.status;

        return;
      }

      this.nomeCompleto = '';
      this.cpf = '';
      this.dataNascimento = '';
      this.planoId = '';
      this.status = 'ATIVO';
    });
  }

  protected get editando(): boolean {
    return this.beneficiario() !== null;
  }

  protected get beneficiarioInativo(): boolean {
    return this.beneficiario()?.status === 'INATIVO';
  }

  protected cancelar(): void {
    if (this.salvando()) {
      return;
    }

    this.fechar.emit();
  }

  protected salvar(): void {
    this.erro.set(null);

    if (!this.validar()) {
      return;
    }

    const beneficiario = this.beneficiario();

    if (beneficiario) {
      this.atualizar(beneficiario.id);
      return;
    }

    this.criar();
  }

  private atualizar(id: string): void {
    const original = this.beneficiario();

    if (!original) {
      this.erro.set('Não foi possível identificar o beneficiário para atualização.');
      return;
    }
    const dados: AtualizarBeneficiarioRequest = {};

    if (this.nomeCompleto.trim() !== original.nome_completo) {
      dados.nome_completo = this.nomeCompleto.trim();
    }

    if (this.dataNascimento !== original.data_nascimento) {
      dados.data_nascimento = this.dataNascimento;
    }

    if (this.planoId !== original.plano_id) {
      dados.plano_id = this.planoId;
    }

    if (this.status !== original.status) {
      dados.status = this.status;
    }

    if (Object.keys(dados).length === 0) {
      this.erro.set('Nenhuma alteração foi realizada.');
      return;
    }

    this.salvando.set(true);
    this.servico
      .atualizar(id, dados)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.salvando.set(false);

          this.mensagemServico.sucesso('Beneficiário atualizado com sucesso.');

          this.salvo.emit();
          this.fechar.emit();
        },
        error: (resposta: HttpErrorResponse) => {
          this.salvando.set(false);
          this.erro.set(mensagemDeErro(resposta));
        },
      });
  }

  private criar(): void {
    this.salvando.set(true);

    this.servico
      .criar({
        nome_completo: this.nomeCompleto.trim(),
        cpf: this.cpf,
        data_nascimento: this.dataNascimento,
        plano_id: this.planoId,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.salvando.set(false);

          this.mensagemServico.sucesso('Beneficiário cadastrado com sucesso.');

          this.salvo.emit();
          this.fechar.emit();
        },
        error: (resposta: HttpErrorResponse) => {
          this.salvando.set(false);
          this.erro.set(mensagemDeErro(resposta));
        },
      });
  }

  private validar(): boolean {
    if (!this.nomeCompleto.trim()) {
      this.erro.set('Informe o nome completo.');
      return false;
    }

    if (!this.editando && !this.cpfValido(this.cpf)) {
      this.erro.set('Informe um CPF válido com 11 dígitos.');
      return false;
    }

    if (!this.dataNascimento) {
      this.erro.set('Informe a data de nascimento.');
      return false;
    }

    if (this.dataNascimento >= new Date().toISOString().slice(0, 10)) {
      this.erro.set('A data de nascimento deve estar no passado.');
      return false;
    }

    if (!this.planoId) {
      this.erro.set('Selecione um plano.');
      return false;
    }

    return true;
  }

  private cpfValido(cpf: string): boolean {
    return /^\d{11}$/.test(cpf);
  }
  protected alterarCpf(valor: string): void {
    this.cpf = valor.replace(/\D/g, '').slice(0, 11);
  }
}
