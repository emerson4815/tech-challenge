import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { mensagemDeErro } from '../nucleo/api';
import { Beneficiario, StatusBeneficiario } from './beneficiario';
import { BeneficiarioServico } from './beneficiario-servico';
import { PlanoServico } from '../planos/plano-servico';
import { Plano } from '../planos/plano';
import { formatarCpf, formatarData } from '../nucleo/formatadores';
import { MensagemServico } from '../nucleo/mensagem-servico';
import { BeneficiarioFormulario } from './beneficiario-formulario/beneficiario-formulario';

@Component({
  selector: 'app-beneficiarios-lista',
  imports: [BeneficiarioFormulario],
  templateUrl: './beneficiarios-lista.html',
  styleUrl: './beneficiarios-lista.css',
})
export class BeneficiariosLista {
  private readonly servico = inject(BeneficiarioServico);
  private readonly planoServico = inject(PlanoServico);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly beneficiarios = signal<Beneficiario[]>([]);
  protected readonly carregando = signal(true);
  protected readonly erro = signal<string | null>(null);

  protected readonly pagina = signal(1);
  protected readonly tamanho = signal(10);
  protected readonly total = signal(0);

  protected readonly filtroStatus = signal<StatusBeneficiario | undefined>(undefined);

  protected readonly filtroPlano = signal<string | undefined>(undefined);

  protected readonly planos = signal<Plano[]>([]);
  protected readonly carregandoPlanos = signal(true);
  protected readonly formatarCpf = formatarCpf;
  protected readonly formatarData = formatarData;

  protected readonly modalExcluirAberto = signal(false);
  protected readonly beneficiarioExcluir = signal<Beneficiario | null>(null);

  protected readonly excluindo = signal(false);

  private readonly mensagemServico = inject(MensagemServico);
  protected readonly mensagem = this.mensagemServico.mensagem;

  protected readonly beneficiarioSelecionado = signal<Beneficiario | null>(null);

  protected readonly formularioAberto = signal(false);

  constructor() {
    this.carregarPlanos();
    this.carregar();
  }

  protected carregar(): void {
    this.carregando.set(true);
    this.erro.set(null);

    this.servico
      .listar(this.pagina(), this.tamanho(), this.filtroStatus(), this.filtroPlano())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (resultado) => {
          this.beneficiarios.set(resultado.dados);
          this.pagina.set(resultado.pagina);
          this.tamanho.set(resultado.tamanho);
          this.total.set(resultado.total);
          this.carregando.set(false);
        },
        error: (resposta: HttpErrorResponse) => {
          this.erro.set(mensagemDeErro(resposta));
          this.carregando.set(false);
        },
      });
  }
  protected carregarPlanos(): void {
    this.planoServico
      .listar()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (planos) => {
          this.planos.set(planos);
          this.carregandoPlanos.set(false);
        },
        error: (resposta: HttpErrorResponse) => {
          this.erro.set(mensagemDeErro(resposta));
          this.carregandoPlanos.set(false);
        },
      });
  }
  protected nomePlano(planoId: string): string {
    if (this.carregandoPlanos()) {
      return 'Carregando plano...';
    }
    return this.planos().find((plano) => plano.id === planoId)?.nome ?? 'Plano não encontrado';
  }

  protected paginaAnterior(): void {
    if (this.pagina() <= 1) {
      return;
    }

    this.pagina.update((valor) => valor - 1);
    this.carregar();
  }

  protected proximaPagina(): void {
    if (this.pagina() * this.tamanho() >= this.total()) {
      return;
    }

    this.pagina.update((valor) => valor + 1);
    this.carregar();
  }
  protected editar(beneficiario: Beneficiario): void {
    this.beneficiarioSelecionado.set(beneficiario);
    this.formularioAberto.set(true);
  }

  protected fecharFormulario(): void {
    this.formularioAberto.set(false);
    this.beneficiarioSelecionado.set(null);
  }

  protected abrirExclusao(beneficiario: Beneficiario): void {
    this.beneficiarioExcluir.set(beneficiario);
    this.modalExcluirAberto.set(true);
  }

  protected fecharExclusao(): void {
    this.beneficiarioExcluir.set(null);
    this.modalExcluirAberto.set(false);
  }
  protected confirmarExclusao(): void {
    const beneficiario = this.beneficiarioExcluir();

    if (!beneficiario) {
      return;
    }
    this.excluindo.set(true);
    this.erro.set(null);

    this.servico
      .excluir(beneficiario.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.excluindo.set(false);
          this.fecharExclusao();
          this.mensagemServico.sucesso('Beneficiário excluído com sucesso.');
          this.carregar();
        },
        error: (resposta: HttpErrorResponse) => {
          this.excluindo.set(false);
          this.erro.set(mensagemDeErro(resposta));
        },
      });
  }
}
