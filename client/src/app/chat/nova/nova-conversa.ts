import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { CatalogoService } from '../../catalogo/catalogo.service';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { ChatService } from '../chat.service';
import { TAMANHO_MAXIMO_MENSAGEM } from '../chat.models';

/** O cliente escreve para a locadora de um carro (vindo da tela do carro ou do pedido). */
@Component({
    selector: 'app-nova-conversa',
    imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, RouterLink],
    template: `
<section class="mx-auto max-w-2xl px-4 py-6">
    <a routerLink="/catalogo" class="text-sm text-primary inline-flex items-center gap-1">
        <mat-icon class="!h-5 !w-5 !text-[18px]">arrow_back</mat-icon>Voltar
    </a>

    @if (erro()) {
    <p class="mt-4 rounded-lg px-4 py-3 text-sm selo-vermelho">{{ erro() }}</p>
    } @else if (carro(); as c) {
    <h2 class="mt-2 text-2xl font-semibold">Falar com {{ c.locadora }}</h2>
    <p class="text-sm text-gray-600">Sobre o {{ c.marca }} {{ c.modelo }}. A locadora responde por aqui e avisamos você.</p>

    <form class="mt-4 flex flex-col gap-3" (ngSubmit)="enviar()">
        <mat-form-field subscriptSizing="dynamic">
            <mat-label>Sua mensagem</mat-label>
            <textarea matInput name="texto" rows="4" [(ngModel)]="texto" [maxlength]="limite"
                      placeholder="Ex.: Esse carro está disponível no fim de semana?"></textarea>
            <mat-hint align="end">{{ texto.length }}/{{ limite }}</mat-hint>
        </mat-form-field>
        <div class="flex justify-end">
            <button mat-flat-button type="submit" [disabled]="enviando() || !texto.trim()">
                <mat-icon>send</mat-icon>
                Enviar mensagem
            </button>
        </div>
    </form>
    } @else {
    <p class="mt-4 text-sm text-gray-500">Carregando…</p>
    }
</section>
`,
})
export class NovaConversa implements OnInit {
    private readonly service = inject(ChatService);
    private readonly catalogo = inject(CatalogoService);
    private readonly notificacao = inject(NotificacaoService);
    private readonly router = inject(Router);
    private readonly automovelId = inject(ActivatedRoute).snapshot.queryParamMap.get('automovelId');

    protected readonly limite = TAMANHO_MAXIMO_MENSAGEM;
    protected readonly carro = signal<{ locadora: string; marca: string; modelo: string } | null>(null);
    protected readonly erro = signal<string | null>(null);
    protected readonly enviando = signal(false);
    protected texto = '';

    ngOnInit(): void {
        if (!this.automovelId) {
            this.erro.set('Escolha um carro para falar com a locadora.');
            return;
        }

        this.catalogo.detalhar(this.automovelId).subscribe({
            next: (c) => this.carro.set(c),
            error: (err) => this.erro.set(mensagemDeErro(err, 'Não foi possível abrir este carro.')),
        });
    }

    protected enviar(): void {
        const texto = this.texto.trim();

        if (!texto || !this.automovelId || this.enviando()) return;

        this.enviando.set(true);

        this.service.iniciar(this.automovelId, texto).subscribe({
            next: ({ id }) => this.router.navigate(['/mensagens', id]),
            error: (err) => {
                this.notificacao.erro(mensagemDeErro(err, 'Não foi possível enviar a mensagem.'));
                this.enviando.set(false);
            },
        });
    }
}
