import { map, Observable } from 'rxjs';

import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import {
    CadastrarGrupoVeiculosModel,
    CadastrarGrupoVeiculosResponseModel,
    DetalhesGrupoVeiculosModel,
    EditarGrupoVeiculosModel,
    EditarGrupoVeiculosResponseModel,
    SelecionarGruposVeiculosModel,
    SelecionarGruposVeiculosResponseModel,
} from './grupo-veiculos.models';

@Injectable()
export class GrupoVeiculosService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = environment.apiUrl + '/grupos-veiculos';

    public cadastrar(
        model: CadastrarGrupoVeiculosModel,
    ): Observable<CadastrarGrupoVeiculosResponseModel> {
        return this.http.post<CadastrarGrupoVeiculosResponseModel>(this.apiUrl, model);
    }

    public editar(
        id: string,
        model: EditarGrupoVeiculosModel,
    ): Observable<EditarGrupoVeiculosResponseModel> {
        return this.http.put<EditarGrupoVeiculosResponseModel>(`${this.apiUrl}/${id}`, model);
    }

    public excluir(id: string): Observable<null> {
        return this.http.delete<null>(`${this.apiUrl}/${id}`);
    }

    public selecionarPorId(id: string): Observable<DetalhesGrupoVeiculosModel> {
        return this.http.get<DetalhesGrupoVeiculosModel>(`${this.apiUrl}/${id}`);
    }

    public selecionarTodos(): Observable<SelecionarGruposVeiculosModel[]> {
        return this.http
            .get<SelecionarGruposVeiculosResponseModel>(this.apiUrl)
            .pipe(map((res) => res.registros));
    }
}
