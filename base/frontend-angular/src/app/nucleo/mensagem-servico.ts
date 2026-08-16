import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class MensagemServico {
  readonly mensagem = signal<string | null>(null);

  sucesso(mensagem: string): void {
    this.mensagem.set(mensagem);

    setTimeout(() => {
      this.mensagem.set(null);
    }, 3000);
  }

  limpar(): void {
    this.mensagem.set(null);
  }
}
