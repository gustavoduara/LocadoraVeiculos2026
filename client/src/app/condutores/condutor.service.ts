import { map, Observable } from 'rxjs';

import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import {
    CadastrarCondutorModel,
    CadastrarCondutorResponseModel,
    CondutorModel,
    EditarCondutorModel,
    SelecionarCondutoresResponseModel,
} from './condutor.models';

@Injectable()
export class CondutorService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = environment.apiUrl + '/condutores';

    public cadastrar(model: CadastrarCondutorModel): Observable<CadastrarCondutorResponseModel> {
        return this.http.post<CadastrarCondutorResponseModel>(this.apiUrl, model);
    }

    public editar(id: string, model: EditarCondutorModel): Observable<CondutorModel> {
        return this.http.put<CondutorModel>(`${this.apiUrl}/${id}`, model);
    }

    public excluir(id: string): Observable<null> {
        return this.http.delete<null>(`${this.apiUrl}/${id}`);
    }

    public selecionarPorId(id: string): Observable<CondutorModel> {
        return this.http.get<CondutorModel>(`${this.apiUrl}/${id}`);
    }

    /** Lista os condutores; com `clienteId`, apenas os daquele cliente. */
    public selecionarTodos(clienteId?: string | null): Observable<CondutorModel[]> {
        let parametros = new HttpParams();

        if (clienteId) parametros = parametros.set('clienteId', clienteId);

        return this.http
            .get<SelecionarCondutoresResponseModel>(this.apiUrl, { params: parametros })
            .pipe(map((res) => res.registros));
    }
}
