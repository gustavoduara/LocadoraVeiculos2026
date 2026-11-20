import { map, Observable } from 'rxjs';

import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import {
    DadosReservaModel,
    EstimativaReservaModel,
    PerfilClienteModel,
    ReservaModel,
    SalvarPerfilClienteModel,
    SelecionarReservasResponseModel,
    StatusReservaModel,
} from './reserva.models';

/** Tudo de reservas: o que o cliente faz (pedir, acompanhar, cancelar) e o que a locadora faz (aprovar, recusar). */
@Injectable()
export class ReservaService {
    private readonly http = inject(HttpClient);
    private readonly urlCliente = environment.apiUrl + '/minhas-reservas';
    private readonly urlLocadora = environment.apiUrl + '/reservas';
    private readonly urlDados = environment.apiUrl + '/meus-dados';

    // ---------------- cliente ----------------

    public obterMeusDados(): Observable<PerfilClienteModel> {
        return this.http.get<PerfilClienteModel>(this.urlDados);
    }

    public salvarMeusDados(model: SalvarPerfilClienteModel): Observable<PerfilClienteModel> {
        return this.http.put<PerfilClienteModel>(this.urlDados, model);
    }

    public calcular(dados: DadosReservaModel): Observable<EstimativaReservaModel> {
        return this.http.post<EstimativaReservaModel>(`${this.urlCliente}/calcular`, dados);
    }

    public criar(dados: DadosReservaModel): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.urlCliente, dados);
    }

    public minhasReservas(): Observable<ReservaModel[]> {
        return this.http
            .get<SelecionarReservasResponseModel>(this.urlCliente)
            .pipe(map((res) => res.registros));
    }

    public cancelar(id: string): Observable<ReservaModel> {
        return this.http.post<ReservaModel>(`${this.urlCliente}/${id}/cancelar`, {});
    }

    // ---------------- locadora ----------------

    public recebidas(status?: StatusReservaModel | null): Observable<ReservaModel[]> {
        let parametros = new HttpParams();

        if (status) parametros = parametros.set('status', status);

        return this.http
            .get<SelecionarReservasResponseModel>(this.urlLocadora, { params: parametros })
            .pipe(map((res) => res.registros));
    }

    public totalDePendentes(): Observable<number> {
        return this.http
            .get<{ total: number }>(`${this.urlLocadora}/pendentes/total`)
            .pipe(map((res) => res.total));
    }

    public aprovar(id: string, kmInicial: number): Observable<ReservaModel> {
        return this.http.post<ReservaModel>(`${this.urlLocadora}/${id}/aprovar`, { kmInicial });
    }

    public recusar(id: string, motivo: string | null): Observable<ReservaModel> {
        return this.http.post<ReservaModel>(`${this.urlLocadora}/${id}/recusar`, { motivo });
    }
}
