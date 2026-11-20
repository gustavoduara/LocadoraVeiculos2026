import { Observable } from 'rxjs';

import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import { ConfiguracaoModel, DadosDaLocadoraModel } from './configuracao.models';

@Injectable()
export class ConfiguracaoService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = environment.apiUrl + '/configuracoes';

    public selecionar(): Observable<ConfiguracaoModel> {
        return this.http.get<ConfiguracaoModel>(this.apiUrl);
    }

    public editar(model: ConfiguracaoModel): Observable<ConfiguracaoModel> {
        return this.http.put<ConfiguracaoModel>(this.apiUrl, model);
    }

    /** Nome e telefone que os clientes veem no catálogo e nas reservas. */
    public obterDadosDaLocadora(): Observable<DadosDaLocadoraModel> {
        return this.http.get<DadosDaLocadoraModel>(`${this.apiUrl}/locadora`);
    }

    public salvarDadosDaLocadora(model: DadosDaLocadoraModel): Observable<DadosDaLocadoraModel> {
        return this.http.put<DadosDaLocadoraModel>(`${this.apiUrl}/locadora`, model);
    }
}
