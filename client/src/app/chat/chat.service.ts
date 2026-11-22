import { map, Observable } from 'rxjs';

import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import { ConversaDetalheModel, ConversaResumoModel, LadoChat, MensagemModel } from './chat.models';

/** As conversas pela API. O cliente usa "minhas-conversas"; a locadora (administrador e funcionários), "conversas". */
@Injectable({ providedIn: 'root' })
export class ChatService {
    private readonly http = inject(HttpClient);

    private url(lado: LadoChat): string {
        return environment.apiUrl + (lado === 'cliente' ? '/minhas-conversas' : '/conversas');
    }

    public listar(lado: LadoChat): Observable<ConversaResumoModel[]> {
        return this.http
            .get<{ registros: ConversaResumoModel[] }>(this.url(lado))
            .pipe(map((r) => r.registros));
    }

    public totalNaoLidas(lado: LadoChat): Observable<number> {
        return this.http.get<{ total: number }>(`${this.url(lado)}/nao-lidas/total`).pipe(map((r) => r.total));
    }

    /** Abre a conversa e, no servidor, marca como lidas as mensagens da outra ponta. */
    public abrir(lado: LadoChat, id: string): Observable<ConversaDetalheModel> {
        return this.http.get<ConversaDetalheModel>(`${this.url(lado)}/${id}`);
    }

    public enviar(lado: LadoChat, id: string, texto: string): Observable<MensagemModel> {
        return this.http.post<MensagemModel>(`${this.url(lado)}/${id}/mensagens`, { texto });
    }

    /** O cliente fala com a locadora de um carro (começa a conversa, ou continua a que já existe com ela). */
    public iniciar(automovelId: string, texto: string): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.url('cliente'), { automovelId, texto });
    }
}
