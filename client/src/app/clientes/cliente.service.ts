import { map, Observable } from 'rxjs';

import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import {
    ClienteModel,
    EditarClienteModel,
    SelecionarClientesResponseModel,
    TipoClienteModel,
} from './cliente.models';

@Injectable()
export class ClienteService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = environment.apiUrl + '/clientes';

    public editar(id: string, model: EditarClienteModel): Observable<ClienteModel> {
        return this.http.put<ClienteModel>(`${this.apiUrl}/${id}`, model);
    }

    public excluir(id: string): Observable<null> {
        return this.http.delete<null>(`${this.apiUrl}/${id}`);
    }

    public selecionarPorId(id: string): Observable<ClienteModel> {
        return this.http.get<ClienteModel>(`${this.apiUrl}/${id}`);
    }

    /** Lista os clientes; com `tipo`, apenas pessoas físicas ou apenas jurídicas. */
    public selecionarTodos(tipo?: TipoClienteModel | null): Observable<ClienteModel[]> {
        let parametros = new HttpParams();

        if (tipo) parametros = parametros.set('tipo', tipo);

        return this.http
            .get<SelecionarClientesResponseModel>(this.apiUrl, { params: parametros })
            .pipe(map((res) => res.registros));
    }
}
