import { Observable } from 'rxjs';

import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import { ConfiguracaoModel } from './configuracao.models';

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
}
