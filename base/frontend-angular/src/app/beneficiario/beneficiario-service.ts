import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE } from '../nucleo/api';
import { Beneficiario, StatusBeneficiario } from './beneficiario';
import { ListaPaginada } from '../nucleo/lista-paginada';

@Injectable({ providedIn: 'root' })
export class BeneficiarioServico {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE);

  listar(
    pagina = 1,
    tamanho = 10,
    status?: StatusBeneficiario,
    planoId?: string,
  ): Observable<ListaPaginada<Beneficiario>> {
    let params = new HttpParams().set('pagina', pagina).set('tamanho', tamanho);

    if (status) {
      params = params.set('status', status);
    }

    if (planoId) {
      params = params.set('plano_id', planoId);
    }

    return this.http.get<ListaPaginada<Beneficiario>>(`${this.base}/beneficiarios`, { params });
  }
}
