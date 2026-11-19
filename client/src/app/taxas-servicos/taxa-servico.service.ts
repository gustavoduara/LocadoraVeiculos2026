import { map, Observable } from 'rxjs';

import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import {
    CadastrarTaxaServicoModel,
    CadastrarTaxaServicoResponseModel,
    EditarTaxaServicoModel,
    EditarTaxaServicoResponseModel,
    SelecionarTaxasServicosResponseModel,
    TaxaServicoModel,
} from './taxa-servico.models';

@Injectable()
export class TaxaServicoService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = environment.apiUrl + '/taxas-servicos';

    public cadastrar(model: CadastrarTaxaServicoModel): Observable<CadastrarTaxaServicoResponseModel> {
        return this.http.post<CadastrarTaxaServicoResponseModel>(this.apiUrl, model);
    }

    public editar(
        id: string,
        model: EditarTaxaServicoModel,
    ): Observable<EditarTaxaServicoResponseModel> {
        return this.http.put<EditarTaxaServicoResponseModel>(`${this.apiUrl}/${id}`, model);
    }

    public excluir(id: string): Observable<null> {
        return this.http.delete<null>(`${this.apiUrl}/${id}`);
    }

    public selecionarPorId(id: string): Observable<TaxaServicoModel> {
        return this.http.get<TaxaServicoModel>(`${this.apiUrl}/${id}`);
    }

    public selecionarTodos(): Observable<TaxaServicoModel[]> {
        return this.http
            .get<SelecionarTaxasServicosResponseModel>(this.apiUrl)
            .pipe(map((res) => res.registros));
    }
}
