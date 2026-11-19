import { map, Observable } from 'rxjs';

import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import {
    AutomovelModel,
    CadastrarAutomovelModel,
    CadastrarAutomovelResponseModel,
    EditarAutomovelModel,
    EditarAutomovelResponseModel,
    EnviarFotoResponseModel,
    SelecionarAutomoveisResponseModel,
} from './automovel.models';

@Injectable()
export class AutomovelService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = environment.apiUrl + '/automoveis';

    public cadastrar(model: CadastrarAutomovelModel): Observable<CadastrarAutomovelResponseModel> {
        return this.http.post<CadastrarAutomovelResponseModel>(this.apiUrl, model);
    }

    public editar(id: string, model: EditarAutomovelModel): Observable<EditarAutomovelResponseModel> {
        return this.http.put<EditarAutomovelResponseModel>(`${this.apiUrl}/${id}`, model);
    }

    public excluir(id: string): Observable<null> {
        return this.http.delete<null>(`${this.apiUrl}/${id}`);
    }

    public selecionarPorId(id: string): Observable<AutomovelModel> {
        return this.http.get<AutomovelModel>(`${this.apiUrl}/${id}`);
    }

    /** Lista a frota; com `grupoVeiculosId`, apenas os veículos daquele grupo. */
    public selecionarTodos(grupoVeiculosId?: string | null): Observable<AutomovelModel[]> {
        let parametros = new HttpParams();

        if (grupoVeiculosId) parametros = parametros.set('grupoVeiculosId', grupoVeiculosId);

        return this.http
            .get<SelecionarAutomoveisResponseModel>(this.apiUrl, { params: parametros })
            .pipe(map((res) => res.registros));
    }

    public enviarFoto(id: string, arquivo: File): Observable<EnviarFotoResponseModel> {
        const formulario = new FormData();

        formulario.append('arquivo', arquivo, arquivo.name);

        return this.http.put<EnviarFotoResponseModel>(`${this.apiUrl}/${id}/foto`, formulario);
    }

    /** A foto exige autenticação, então é baixada como Blob (um <img src> não enviaria o token). */
    public obterFoto(id: string): Observable<Blob> {
        return this.http.get(`${this.apiUrl}/${id}/foto`, { responseType: 'blob' });
    }

    public removerFoto(id: string): Observable<null> {
        return this.http.delete<null>(`${this.apiUrl}/${id}/foto`);
    }
}
