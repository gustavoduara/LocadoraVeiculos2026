import { Observable } from 'rxjs';

import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';

export interface SituacaoDaContaModel {
    /** Dia (UTC) em que a exclusão da locadora acontece; vazio quando não há exclusão agendada. */
    exclusaoAgendadaParaUtc: string | null;
}

/** Exclusão da própria conta (LGPD): cliente na hora, locadora agendada para 7 dias depois. */
@Injectable({ providedIn: 'root' })
export class ContaService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = environment.apiUrl + '/conta';

    public situacao(): Observable<SituacaoDaContaModel> {
        return this.http.get<SituacaoDaContaModel>(`${this.apiUrl}/situacao`);
    }

    /** Cliente: exclui agora. `confirmacao` é a palavra EXCLUIR. */
    public excluir(senha: string, confirmacao: string): Observable<null> {
        return this.http.post<null>(`${this.apiUrl}/excluir`, { senha, confirmacao });
    }

    /** Locadora (administrador): agenda a exclusão da empresa para 7 dias depois. */
    public agendarExclusao(senha: string, nomeDaEmpresa: string): Observable<SituacaoDaContaModel> {
        return this.http.post<SituacaoDaContaModel>(`${this.apiUrl}/agendar-exclusao`, { senha, nomeDaEmpresa });
    }

    public cancelarExclusao(): Observable<null> {
        return this.http.post<null>(`${this.apiUrl}/cancelar-exclusao`, {});
    }
}
