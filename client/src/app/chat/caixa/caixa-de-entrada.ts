import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { ChatTempoRealService } from '../chat-tempo-real.service';
import { ChatService } from '../chat.service';
import { ConversaResumoModel, LadoChat } from '../chat.models';

/** Lista de conversas: as minhas (cliente) ou a caixa de entrada da locadora. O lado vem da rota. */
@Component({
    selector: 'app-caixa-de-entrada',
    imports: [MatButtonModule, MatCardModule, MatIconModule, RouterLink],
    template: `
<section class="mx-auto max-w-3xl px-4 py-6">
    <h2 class="text-2xl font-semibold">{{ lado === 'cliente' ? 'Mensagens' : 'Conversas com clientes' }}</h2>
    <p class="text-sm text-gray-600">
        @if (lado === 'cliente') { Suas conversas com as locadoras. Para começar uma, abra um carro e toque em "Falar com a locadora". }
        @else { A caixa de entrada é compartilhada: o administrador e os funcionários veem e respondem as mesmas conversas. }
    </p>
    <hr class="my-4 border-muted/30" />

    @if (erro()) {
    <p class="rounded-lg px-4 py-3 text-sm selo-vermelho">{{ erro() }}</p>
    } @else if (carregando()) {
    <p class="text-sm text-gray-500">Carregando…</p>
    } @else if (conversas().length === 0) {
    <mat-card class="rounded-2xl p-6 text-center shadow-sm">
        <p class="text-gray-600">Nenhuma conversa ainda.</p>
        @if (lado === 'cliente') {
        <a mat-flat-button routerLink="/catalogo" class="mt-4 self-center">Buscar carros</a>
        }
    </mat-card>
    } @else {
    <div class="flex flex-col gap-2">
        @for (c of conversas(); track c.id) {
        <a [routerLink]="[base, c.id]" class="no-underline">
            <mat-card class="rounded-2xl shadow-sm hover:shadow-md">
                <mat-card-content class="flex items-center justify-between gap-3 p-4">
                    <div class="min-w-0">
                        <h3 class="truncate text-base font-semibold">{{ c.titulo }}</h3>
                        <p class="truncate text-sm text-gray-600">{{ c.ultimaMensagem }}</p>
                    </div>
                    <div class="flex shrink-0 flex-col items-end gap-1">
                        <span class="text-xs text-gray-500">{{ quando(c.ultimaMensagemEmUtc) }}</span>
                        @if (c.naoLidas > 0) {
                        <span class="rounded-full px-2 py-0.5 text-xs font-medium selo-verde" aria-label="{{ c.naoLidas }} não lidas">{{ c.naoLidas }}</span>
                        }
                    </div>
                </mat-card-content>
            </mat-card>
        </a>
        }
    </div>
    }
</section>
`,
})
export class CaixaDeEntrada implements OnInit {
    private readonly service = inject(ChatService);
    private readonly tempoReal = inject(ChatTempoRealService);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly lado: LadoChat = inject(ActivatedRoute).snapshot.data['lado'];
    protected readonly base = this.lado === 'cliente' ? '/mensagens' : '/conversas';

    protected readonly conversas = signal<ConversaResumoModel[]>([]);
    protected readonly carregando = signal(true);
    protected readonly erro = signal<string | null>(null);

    ngOnInit(): void {
        this.carregar();

        // Chegou mensagem com a lista aberta: recarrega (a ordem e o contador mudam).
        this.tempoReal.mensagens$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.carregar());
    }

    private carregar(): void {
        this.service.listar(this.lado).subscribe({
            next: (lista) => {
                this.conversas.set(lista);
                this.carregando.set(false);
            },
            error: (err) => {
                this.erro.set(mensagemDeErro(err, 'Não foi possível carregar as conversas.'));
                this.carregando.set(false);
            },
        });
    }

    protected quando(iso: string): string {
        const data = new Date(iso);
        const hoje = new Date();

        return data.toDateString() === hoje.toDateString()
            ? data.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
            : data.toLocaleDateString('pt-BR');
    }
}
