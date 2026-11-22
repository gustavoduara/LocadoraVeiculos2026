import { firstValueFrom, Subject, take } from 'rxjs';

import { inject, Injectable, signal } from '@angular/core';
import type { HubConnection } from '@microsoft/signalr';

import { environment } from '../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { ChatService } from './chat.service';
import { LadoChat, MensagemRecebidaEvento } from './chat.models';

/**
 * Conexão em tempo real do chat (SignalR). Fica aberta enquanto a pessoa está logada: avisa as telas de cada mensagem
 * que chega e mantém o contador de não lidas do menu. Se o WebSocket não estiver disponível, o SignalR cai sozinho
 * para outro transporte; se a conexão cair, reconecta e recontaa.
 */
@Injectable({ providedIn: 'root' })
export class ChatTempoRealService {
    private readonly auth = inject(AuthService);
    private readonly chat = inject(ChatService);

    private conexao?: HubConnection;
    private lado?: LadoChat;

    /** Mensagens que acabaram de chegar (as telas abertas escutam). */
    public readonly mensagens$ = new Subject<MensagemRecebidaEvento>();

    /** Mensagens não lidas, para o selo do menu. */
    public readonly naoLidas = signal(0);

    public async conectar(lado: LadoChat): Promise<void> {
        // O lado vale já na primeira chamada (o carregamento do SignalR é assíncrono): evita abrir duas conexões.
        if (this.lado === lado) return;

        await this.desconectar();

        this.lado = lado;
        this.atualizarContador();

        // Carrega o SignalR só quando há alguém logado (fora do pacote inicial do site).
        const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');

        const conexao = new HubConnectionBuilder()
            .withUrl(environment.apiUrl.replace(/\/api$/, '') + '/hubs/chat', {
                accessTokenFactory: async () => (await firstValueFrom(this.auth.obterAccessToken().pipe(take(1))))?.chave ?? '',
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.None)
            .build();

        conexao.on('MensagemRecebida', (evento: MensagemRecebidaEvento) => {
            this.mensagens$.next(evento);
            this.atualizarContador();
        });

        // Depois de uma queda, pode ter chegado coisa que não foi empurrada: recontar.
        conexao.onreconnected(() => this.atualizarContador());

        this.conexao = conexao;

        try {
            await conexao.start();
        } catch {
            // Sem tempo real a tela continua funcionando (o contador atualiza ao navegar e ao enviar).
        }
    }

    public async desconectar(): Promise<void> {
        const conexao = this.conexao;

        this.conexao = undefined;
        this.lado = undefined;
        this.naoLidas.set(0);

        if (conexao) {
            try { await conexao.stop(); } catch { /* já caiu */ }
        }
    }

    /** Pede de novo o total ao servidor (depois de ler uma conversa, por exemplo). */
    public atualizarContador(): void {
        const lado = this.lado;

        if (!lado) return;

        this.chat.totalNaoLidas(lado).subscribe({
            next: (total) => this.naoLidas.set(total),
            error: () => undefined,
        });
    }
}
