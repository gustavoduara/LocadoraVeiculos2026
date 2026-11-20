import { map, Observable } from 'rxjs';

import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import {
    CadastrarCupomModel,
    CadastrarCupomResponseModel,
    CupomModel,
    CuponsMaisUtilizadosModel,
    EditarCupomModel,
    SelecionarCuponsResponseModel,
} from './cupom.models';

@Injectable()
export class CupomService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = environment.apiUrl + '/cupons';

    public cadastrar(model: CadastrarCupomModel): Observable<CadastrarCupomResponseModel> {
        return this.http.post<CadastrarCupomResponseModel>(this.apiUrl, model);
    }

    public editar(id: string, model: EditarCupomModel): Observable<CupomModel> {
        return this.http.put<CupomModel>(`${this.apiUrl}/${id}`, model);
    }

    public excluir(id: string): Observable<null> {
        return this.http.delete<null>(`${this.apiUrl}/${id}`);
    }

    public selecionarPorId(id: string): Observable<CupomModel> {
        return this.http.get<CupomModel>(`${this.apiUrl}/${id}`);
    }

    public selecionarTodos(): Observable<CupomModel[]> {
        return this.http
            .get<SelecionarCuponsResponseModel>(this.apiUrl)
            .pipe(map((res) => res.registros));
    }

    /** Ranking de uso (só o administrador da empresa vê). */
    public maisUtilizados(): Observable<CuponsMaisUtilizadosModel> {
        return this.http.get<CuponsMaisUtilizadosModel>(`${this.apiUrl}/mais-utilizados`);
    }
}
