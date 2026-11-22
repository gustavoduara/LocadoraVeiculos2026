import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AfterViewChecked, Component, DestroyRef, ElementRef, inject, OnInit, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { ChatTempoRealService } from '../chat-tempo-real.service';
import { ChatService } from '../chat.service';
import { LadoChat, MensagemModel, TAMANHO_MAXIMO_MENSAGEM } from '../chat.models';

/** Uma conversa aberta: as mensagens (chegam em tempo real) e a caixa para responder. */
@Component({
    selector: 'app-conversa',
    imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, RouterLink],
    template: `
<section class="mx-auto flex max-w-3xl flex-col px-4 py-6">
    <a [routerLink]="base" class="text-sm text-primary inline-flex items-center gap-1">
        <mat-icon class="!h-5 !w-5 !text-[18px]">arrow_back</mat-icon>Voltar às conversas
    </a>

    @if (erro()) {
    <p class="mt-4 rounded-lg px-4 py-3 text-sm selo-vermelho">{{ erro() }}</p>
    } @else {
    <h2 class="mt-2 text-xl font-semibold">{{ titulo() }}</h2>

    <div #rolagem class="mt-3 flex h-[55vh] flex-col gap-2 overflow-y-auto rounded-2xl border border-muted/30 p-3" aria-live="polite">
        @if (carregando()) {
        <p class="text-sm text-gray-500">Carregando…</p>
        }
        @for (m of mensagens(); track m.id) {
        <div class="flex flex-col" [class.items-end]="minha(m)">
            <div class="max-w-[85%] whitespace-pre-wrap break-words rounded-2xl px-3 py-2 text-sm"
                 [class]="minha(m) ? 'selo-verde' : 'selo-cinza'">
                @if (m.descricaoVeiculo) {
                <p class="mb-1 text-xs opacity-80"><mat-icon class="!h-4 !w-4 !text-[14px] align-middle">directions_car</mat-icon> {{ m.descricaoVeiculo }}</p>
                }
                {{ m.texto }}
            </div>
            <span class="mt-0.5 text-[11px] text-gray-500">{{ minha(m) ? 'Você' : m.autorNome }} · {{ hora(m.enviadaEmUtc) }}</span>
        </div>
        }
    </div>

    <form class="mt-3 flex items-start gap-2" (ngSubmit)="enviar()">
        <mat-form-field class="flex-1" subscriptSizing="dynamic">
            <mat-label>Escreva sua mensagem</mat-label>
            <textarea matInput name="texto" rows="2" [(ngModel)]="texto" [maxlength]="limite"
                      (keydown.enter)="enterEnvia($event)"></textarea>
            <mat-hint align="end">{{ texto.length }}/{{ limite }}</mat-hint>
        </mat-form-field>
        <button mat-flat-button type="submit" class="mt-2" [disabled]="enviando() || !texto.trim()">
            <mat-icon>send</mat-icon>
            Enviar
        </button>
    </form>
    }
</section>
`,
})
export class Conversa implements OnInit, AfterViewChecked {
    private readonly service = inject(ChatService);
    private readonly tempoReal = inject(ChatTempoRealService);
    private readonly notificacao = inject(NotificacaoService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly rota = inject(ActivatedRoute);
    protected readonly lado: LadoChat = this.rota.snapshot.data['lado'];
    protected readonly base = this.lado === 'cliente' ? '/mensagens' : '/conversas';
    private readonly id = this.rota.snapshot.paramMap.get('id')!;

    protected readonly limite = TAMANHO_MAXIMO_MENSAGEM;
    protected readonly titulo = signal('');
    protected readonly mensagens = signal<MensagemModel[]>([]);
    protected readonly carregando = signal(true);
    protected readonly erro = signal<string | null>(null);
    protected readonly enviando = signal(false);
    protected texto = '';

    private readonly rolagem = viewChild<ElementRef<HTMLDivElement>>('rolagem');
    private descer = false;

    ngOnInit(): void {
        this.abrir();

        // Mensagem nova desta conversa: entra na tela e já conta como lida (a pessoa está olhando).
        this.tempoReal.mensagens$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((evento) => {
            if (evento.conversaId !== this.id) return;

            this.acrescentar(evento.mensagem);
            this.service.abrir(this.lado, this.id).subscribe({ next: () => this.tempoReal.atualizarContador(), error: () => undefined });
        });
    }

    ngAfterViewChecked(): void {
        const caixa = this.rolagem()?.nativeElement;

        if (this.descer && caixa) {
            caixa.scrollTop = caixa.scrollHeight;
            this.descer = false;
        }
    }

    private abrir(): void {
        this.service.abrir(this.lado, this.id).subscribe({
            next: (conversa) => {
                this.titulo.set(conversa.titulo);
                this.mensagens.set(conversa.mensagens);
                this.carregando.set(false);
                this.descer = true;
                this.tempoReal.atualizarContador();
            },
            error: (err) => {
                this.erro.set(mensagemDeErro(err, 'Não foi possível abrir a conversa.'));
                this.carregando.set(false);
            },
        });
    }

    private acrescentar(mensagem: MensagemModel): void {
        // A mesma mensagem pode chegar pelo envio e pelo canal em tempo real: não duplicar.
        if (this.mensagens().some((m) => m.id === mensagem.id)) return;

        this.mensagens.update((lista) => [...lista, mensagem]);
        this.descer = true;
    }

    protected minha(m: MensagemModel): boolean {
        return (this.lado === 'cliente') === (m.autor === 'Cliente');
    }

    protected hora(iso: string): string {
        return new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });
    }

    /** Enter envia; Shift+Enter quebra a linha. */
    protected enterEnvia(evento: Event): void {
        const tecla = evento as KeyboardEvent;

        if (tecla.shiftKey) return;

        tecla.preventDefault();
        this.enviar();
    }

    protected enviar(): void {
        const texto = this.texto.trim();

        if (!texto || this.enviando()) return;

        this.enviando.set(true);

        this.service.enviar(this.lado, this.id, texto).subscribe({
            next: (mensagem) => {
                this.acrescentar(mensagem);
                this.texto = '';
                this.enviando.set(false);
            },
            error: (err) => {
                this.notificacao.erro(mensagemDeErro(err, 'Não foi possível enviar a mensagem.'));
                this.enviando.set(false);
            },
        });
    }
}
