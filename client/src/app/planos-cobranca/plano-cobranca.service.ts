import { map, Observable } from 'rxjs';

import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import {
    CadastrarPlanoCobrancaModel,
    CadastrarPlanoCobrancaResponseModel,
    EditarPlanoCobrancaModel,
    EditarPlanoCobrancaResponseModel,
    PlanoCobrancaModel,
    SelecionarPlanosCobrancaResponseModel,
} from './plano-cobranca.models';

@Injectable()
export class PlanoCobrancaService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = environment.apiUrl + '/planos-cobranca';

    public cadastrar(
        model: CadastrarPlanoCobrancaModel,
    ): Observable<CadastrarPlanoCobrancaResponseModel> {
        return this.http.post<CadastrarPlanoCobrancaResponseModel>(this.apiUrl, model);
    }

    public editar(
        id: string,
        model: EditarPlanoCobrancaModel,
    ): Observable<EditarPlanoCobrancaResponseModel> {
        return this.http.put<EditarPlanoCobrancaResponseModel>(`${this.apiUrl}/${id}`, model);
    }

    public excluir(id: string): Observable<null> {
        return this.http.delete<null>(`${this.apiUrl}/${id}`);
    }

    public selecionarPorId(id: string): Observable<PlanoCobrancaModel> {
        return this.http.get<PlanoCobrancaModel>(`${this.apiUrl}/${id}`);
    }

    public selecionarTodos(): Observable<PlanoCobrancaModel[]> {
        return this.http
            .get<SelecionarPlanosCobrancaResponseModel>(this.apiUrl)
            .pipe(map((res) => res.registros));
    }
}
