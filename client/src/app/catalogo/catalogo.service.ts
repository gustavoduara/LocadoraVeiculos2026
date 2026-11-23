import { Observable } from 'rxjs';

import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import { CarroDetalheModel } from '../reservas/reserva.models';
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

    /** Detalhe de um carro: planos e seguros que a locadora oferece (para escolher como reservar). */
    public detalhar(id: string): Observable<CarroDetalheModel> {
        return this.http.get<CarroDetalheModel>(`${this.apiUrl}/${id}`);
    }

    public opcoesDeFiltro(): Observable<OpcoesFiltroCatalogoModel> {
        return this.http.get<OpcoesFiltroCatalogoModel>(`${this.apiUrl}/filtros`);
    }

    /** A foto na posição pedida da galeria (0 = a primeira): usada pelo carrossel. */
    public obterFotoDaGaleria(id: string, posicao: number): Observable<Blob> {
        return this.http.get(`${this.apiUrl}/${id}/fotos/${posicao}`, { responseType: 'blob' });
    }

    /** A foto exige login, então é baixada como Blob (um <img src> não enviaria o token). */
    public obterFoto(id: string): Observable<Blob> {
        return this.http.get(`${this.apiUrl}/${id}/foto`, { responseType: 'blob' });
    }
}
