import { map, Observable } from 'rxjs';

import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import {
    AluguelModel,
    CadastrarAluguelResponseModel,
    DadosAluguelModel,
    DetalhamentoFinalModel,
    DevolucaoRequestModel,
    SelecionarAlugueisResponseModel,
    SimulacaoAluguelModel,
    StatusAluguelModel,
} from './aluguel.models';

@Injectable()
export class AluguelService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = environment.apiUrl + '/alugueis';

    public cadastrar(model: DadosAluguelModel): Observable<CadastrarAluguelResponseModel> {
        return this.http.post<CadastrarAluguelResponseModel>(this.apiUrl, model);
    }

    public editar(id: string, model: DadosAluguelModel): Observable<AluguelModel> {
        return this.http.put<AluguelModel>(`${this.apiUrl}/${id}`, model);
    }

    public excluir(id: string): Observable<null> {
        return this.http.delete<null>(`${this.apiUrl}/${id}`);
    }

    public selecionarPorId(id: string): Observable<AluguelModel> {
        return this.http.get<AluguelModel>(`${this.apiUrl}/${id}`);
    }

    /** Lista os aluguéis; com `status`, apenas os em aberto ou apenas os concluídos. */
    public selecionarTodos(status?: StatusAluguelModel | null): Observable<AluguelModel[]> {
        let parametros = new HttpParams();

        if (status) parametros = parametros.set('status', status);

        return this.http
            .get<SelecionarAlugueisResponseModel>(this.apiUrl, { params: parametros })
            .pipe(map((res) => res.registros));
    }

    /** Simula o preço do aluguel sem gravar nada. */
    public calcular(model: DadosAluguelModel): Observable<SimulacaoAluguelModel> {
        return this.http.post<SimulacaoAluguelModel>(`${this.apiUrl}/calcular`, model);
    }

    public devolver(id: string, model: DevolucaoRequestModel): Observable<AluguelModel> {
        return this.http.post<AluguelModel>(`${this.apiUrl}/${id}/devolucao`, model);
    }

    /** Simula o valor final da devolução sem concluir o aluguel. */
    public calcularDevolucao(
        id: string,
        model: DevolucaoRequestModel,
    ): Observable<DetalhamentoFinalModel> {
        return this.http.post<DetalhamentoFinalModel>(`${this.apiUrl}/${id}/devolucao/calcular`, model);
    }
}
