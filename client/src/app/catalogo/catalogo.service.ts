import { Observable } from 'rxjs';

import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import {
    FiltrosCatalogoModel,
    OpcoesFiltroCatalogoModel,
    ResultadoCatalogoModel,
} from './catalogo.models';

@Injectable()
export class CatalogoService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = environment.apiUrl + '/catalogo';

    /** Envia só os filtros preenchidos. */
    public pesquisar(filtros: FiltrosCatalogoModel): Observable<ResultadoCatalogoModel> {
        let parametros = new HttpParams();

        for (const [chave, valor] of Object.entries(filtros)) {
            if (valor === undefined || valor === null || valor === '') continue;

            parametros = parametros.set(chave, String(valor));
        }

        return this.http.get<ResultadoCatalogoModel>(this.apiUrl, { params: parametros });
    }

    public opcoesDeFiltro(): Observable<OpcoesFiltroCatalogoModel> {
        return this.http.get<OpcoesFiltroCatalogoModel>(`${this.apiUrl}/filtros`);
    }

    /** A foto exige login, então é baixada como Blob (um <img src> não enviaria o token). */
    public obterFoto(id: string): Observable<Blob> {
        return this.http.get(`${this.apiUrl}/${id}/foto`, { responseType: 'blob' });
    }
}
