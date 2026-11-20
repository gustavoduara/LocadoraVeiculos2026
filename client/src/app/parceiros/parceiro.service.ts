import { map, Observable } from 'rxjs';

import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import {
    CadastrarParceiroModel,
    CadastrarParceiroResponseModel,
    EditarParceiroModel,
    EditarParceiroResponseModel,
    ParceiroModel,
    SelecionarParceirosResponseModel,
} from './parceiro.models';

@Injectable()
export class ParceiroService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = environment.apiUrl + '/parceiros';

    public cadastrar(model: CadastrarParceiroModel): Observable<CadastrarParceiroResponseModel> {
        return this.http.post<CadastrarParceiroResponseModel>(this.apiUrl, model);
    }

    public editar(id: string, model: EditarParceiroModel): Observable<EditarParceiroResponseModel> {
        return this.http.put<EditarParceiroResponseModel>(`${this.apiUrl}/${id}`, model);
    }

    public excluir(id: string): Observable<null> {
        return this.http.delete<null>(`${this.apiUrl}/${id}`);
    }

    public selecionarPorId(id: string): Observable<ParceiroModel> {
        return this.http.get<ParceiroModel>(`${this.apiUrl}/${id}`);
    }

    public selecionarTodos(): Observable<ParceiroModel[]> {
        return this.http
            .get<SelecionarParceirosResponseModel>(this.apiUrl)
            .pipe(map((res) => res.registros));
    }
}
