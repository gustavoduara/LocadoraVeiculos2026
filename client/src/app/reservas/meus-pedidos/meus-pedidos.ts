import { CurrencyPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';

import { isoParaDataBr } from '../../condutores/condutor.models';
import { rotuloPlano, rotuloSeguro } from '../../alugueis/aluguel.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { classeDoStatusReserva, ReservaModel, rotuloStatusReserva } from '../reserva.models';
import { ReservaService } from '../reserva.service';

/** Os pedidos de reserva do cliente e a resposta de cada locadora. */
@Component({
    selector: 'app-meus-pedidos',
    imports: [CurrencyPipe, MatButtonModule, MatCardModule, MatIconModule, RouterLink],
    template: `
<section class="mx-auto max-w-4xl px-4 py-6">
    <div class="flex flex-wrap items-center justify-between gap-3">
        <div>
            <h2 class="text-2xl font-semibold">Meus pedidos</h2>
            <p class="text-sm text-gray-600">Acompanhe as reservas que você pediu e a resposta das locadoras.</p>
        </div>
        <a mat-stroked-button routerLink="/catalogo">
            <mat-icon>search</mat-icon>
            Buscar carros
        </a>
    </div>
    <hr class="my-4 border-muted/30" />

    @if (erro()) {
    <p class="rounded-lg px-4 py-3 text-sm selo-vermelho">{{ erro() }}</p>
    } @else if (carregando()) {
    <p class="text-sm text-gray-500">Carregando…</p>
    } @else if (pedidos().length === 0) {
    <mat-card class="rounded-2xl p-6 text-center shadow-sm">
        <p class="text-gray-600">Você ainda não fez nenhum pedido de reserva.</p>
        <a mat-flat-button routerLink="/catalogo" class="mt-4 self-center">Encontrar um carro</a>
    </mat-card>
    } @else {
    <div class="flex flex-col gap-4">
        @for (p of pedidos(); track p.id) {
        <mat-card class="rounded-2xl shadow-sm">
            <mat-card-content class="p-4">
                <div class="flex flex-wrap items-start justify-between gap-2">
                    <div>
                        <h3 class="text-lg font-semibold">{{ p.descricaoVeiculo }}</h3>
                        <p class="text-sm text-gray-600">{{ p.nomeLocadora }} · {{ p.nomeGrupo }}</p>
                    </div>
                    <span class="rounded-full px-3 py-1 text-xs font-medium" [class]="classe(p.status)">
                        {{ rotulo(p.status) }}
                    </span>
                </div>

                <p class="mt-3 text-sm">
                    {{ data(p.retirada) }} até {{ data(p.devolucao) }}
                    ({{ p.dias }} {{ p.dias === 1 ? 'dia' : 'dias' }}) ·
                    {{ plano(p.tipoPlano) }} · {{ seguro(p.tipoSeguro) }}
                </p>
                <p class="mt-1 text-sm">
                    Total previsto:
                    <strong class="text-destaque">{{ p.valorTotalPrevisto | currency: 'BRL' }}</strong>
                    <span class="text-gray-500"> (com caução de {{ p.valorCaucao | currency: 'BRL' }})</span>
                </p>

                @if (p.status === 'Aprovada') {
                <p class="mt-3 rounded-lg px-3 py-2 text-sm selo-verde">
                    Reserva aprovada! Retire o carro na data combinada
                    @if (p.telefoneLocadora) { e fale com a locadora pelo telefone <strong>{{ p.telefoneLocadora }}</strong> }
                    .
                </p>
                } @else if (p.status === 'Recusada') {
                <p class="mt-3 rounded-lg px-3 py-2 text-sm selo-vermelho">
                    A locadora recusou o pedido{{ p.motivoRecusa ? ': ' + p.motivoRecusa : '.' }}
                </p>
                } @else if (p.status === 'Pendente') {
                <div class="mt-3 flex justify-end">
                    <button mat-stroked-button type="button" [disabled]="cancelando() === p.id" (click)="cancelar(p)">
                        <mat-icon>close</mat-icon>
                        Cancelar pedido
                    </button>
                </div>
                }
            </mat-card-content>
        </mat-card>
        }
    </div>
    }
</section>
`,
})
export class MeusPedidos implements OnInit {
    private readonly service = inject(ReservaService);
    private readonly notificacao = inject(NotificacaoService);

    protected readonly pedidos = signal<ReservaModel[]>([]);
    protected readonly carregando = signal(true);
    protected readonly erro = signal<string | null>(null);
    protected readonly cancelando = signal<string | null>(null);

    protected readonly rotulo = rotuloStatusReserva;
    protected readonly classe = classeDoStatusReserva;
    protected readonly plano = rotuloPlano;
    protected readonly seguro = rotuloSeguro;
    protected readonly data = isoParaDataBr;

    ngOnInit(): void {
        this.carregar();
    }

    private carregar(): void {
        this.service.minhasReservas().subscribe({
            next: (lista) => {
                this.pedidos.set(lista);
                this.carregando.set(false);
            },
            error: (err) => {
                this.erro.set(mensagemDeErro(err, 'Não foi possível carregar seus pedidos.'));
                this.carregando.set(false);
            },
        });
    }

    protected cancelar(pedido: ReservaModel): void {
        if (!confirm('Cancelar este pedido de reserva?')) return;

        this.cancelando.set(pedido.id);

        this.service.cancelar(pedido.id).subscribe({
            next: () => {
                this.cancelando.set(null);
                this.notificacao.sucesso('Pedido cancelado.');
                this.carregar();
            },
            error: (err) => {
                this.cancelando.set(null);
                this.notificacao.erro(mensagemDeErro(err, 'Não foi possível cancelar o pedido.'));
            },
        });
    }
}
