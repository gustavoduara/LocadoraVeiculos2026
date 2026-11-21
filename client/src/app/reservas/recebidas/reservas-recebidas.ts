import { CurrencyPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

import { rotuloPlano, rotuloSeguro } from '../../alugueis/aluguel.models';
import { isoParaDataBr } from '../../condutores/condutor.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import {
    classeDoStatusReserva,
    ReservaModel,
    rotuloStatusReserva,
    STATUS_RESERVA,
    StatusReservaModel,
} from '../reserva.models';
import { ReservaService } from '../reserva.service';
import { BuscaLista, ListaPaginada, PaginacaoLista } from '../../shared/lista/lista-paginada';

/** A locadora vê os pedidos dos clientes e aprova (vira aluguel) ou recusa. */
@Component({
    selector: 'app-reservas-recebidas',
    imports: [
        BuscaLista,
        PaginacaoLista,
        CurrencyPipe,
        FormsModule,
        MatButtonModule,
        MatCardModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatSelectModule,
    ],
    template: `
<section class="mx-auto max-w-5xl px-4 py-6">
    <div class="flex flex-wrap items-end justify-between gap-3">
        <div>
            <h2 class="text-2xl font-semibold">Reservas</h2>
            <p class="text-sm text-gray-600">
                Pedidos feitos pelos clientes pelo site. Ao aprovar, o aluguel é criado automaticamente.
            </p>
        </div>

        <mat-form-field class="w-56">
            <mat-label>Situação</mat-label>
            <mat-select [ngModel]="filtro()" (ngModelChange)="mudarFiltro($event)">
                <mat-option value="">Todas</mat-option>
                @for (s of situacoes; track s.valor) {
                <mat-option [value]="s.valor">{{ s.rotulo }}</mat-option>
                }
            </mat-select>
        </mat-form-field>
    </div>
    <hr class="mb-4 border-muted/30" />

    @if (erro()) {
    <p class="rounded-lg px-4 py-3 text-sm selo-vermelho">{{ erro() }}</p>
    } @else if (carregando()) {
    <p class="text-sm text-gray-500">Carregando…</p>
    } @else if (pedidos().length === 0) {
    <p class="text-sm text-gray-600">Nenhum pedido por aqui.</p>
    } @else {
    <app-busca-lista [lista]="lista" rotulo="Buscar por cliente, veículo ou CPF" />

    <div class="flex flex-col gap-4">
        @for (p of lista.itens(); track p.id) {
        <mat-card class="rounded-2xl shadow-sm">
            <mat-card-content class="p-4">
                <div class="flex flex-wrap items-start justify-between gap-2">
                    <div>
                        <h3 class="text-lg font-semibold">{{ p.descricaoVeiculo }}</h3>
                        <p class="text-sm text-gray-600">{{ p.nomeGrupo }}</p>
                    </div>
                    <span class="rounded-full px-3 py-1 text-xs font-medium" [class]="classe(p.status)">
                        {{ rotulo(p.status) }}
                    </span>
                </div>

                <div class="mt-3 grid grid-cols-1 gap-4 text-sm sm:grid-cols-2">
                    <div>
                        <p class="font-medium">Cliente</p>
                        <p>{{ p.clienteNome }}</p>
                        <p class="text-gray-600">{{ p.clienteEmail }} · {{ p.clienteTelefone }}</p>
                        <p class="text-gray-600">CPF {{ p.clienteCpf }} · {{ p.clienteCidade }}/{{ p.clienteEstado }}</p>
                        <p class="text-gray-600">
                            CNH {{ p.clienteCnh }}, válida até {{ data(p.clienteValidadeCnh) }}
                        </p>
                    </div>
                    <div>
                        <p class="font-medium">Pedido</p>
                        <p>{{ data(p.retirada) }} até {{ data(p.devolucao) }} ({{ p.dias }} {{ p.dias === 1 ? 'dia' : 'dias' }})</p>
                        <p class="text-gray-600">{{ plano(p.tipoPlano) }} · {{ seguro(p.tipoSeguro) }}</p>
                        <p>
                            Total previsto:
                            <strong class="text-destaque">{{ p.valorTotalPrevisto | currency: 'BRL' }}</strong>
                        </p>
                    </div>
                </div>

                @if (p.status === 'Recusada' && p.motivoRecusa) {
                <p class="mt-3 text-sm text-gray-600">Motivo da recusa: {{ p.motivoRecusa }}</p>
                }

                @if (p.status === 'Pendente') {
                @if (respondendo()?.id === p.id) {
                <div class="mt-4 rounded-lg border p-3">
                    @if (respondendo()!.acao === 'aprovar') {
                    <mat-form-field class="w-full sm:w-64">
                        <mat-label>Quilometragem atual do carro</mat-label>
                        <input matInput type="number" min="0" [(ngModel)]="kmInicial" />
                    </mat-form-field>
                    } @else {
                    <mat-form-field class="w-full">
                        <mat-label>Motivo (opcional, o cliente vê)</mat-label>
                        <input matInput maxlength="300" [(ngModel)]="motivo" />
                    </mat-form-field>
                    }

                    <div class="flex justify-end gap-3">
                        <button mat-stroked-button type="button" (click)="respondendo.set(null)">Voltar</button>
                        <button mat-flat-button type="button" [disabled]="enviando()" (click)="confirmar(p)">
                            {{ respondendo()!.acao === 'aprovar' ? 'Confirmar aprovação' : 'Confirmar recusa' }}
                        </button>
                    </div>
                </div>
                } @else {
                <div class="mt-4 flex justify-end gap-3">
                    <button mat-stroked-button type="button" (click)="iniciar(p, 'recusar')">
                        <mat-icon>close</mat-icon>Recusar
                    </button>
                    <button mat-flat-button type="button" (click)="iniciar(p, 'aprovar')">
                        <mat-icon>check</mat-icon>Aprovar
                    </button>
                </div>
                }
                }
            </mat-card-content>
        </mat-card>
        }
    </div>

    <app-paginacao-lista [lista]="lista" />
    }
</section>
`,
})
export class ReservasRecebidas implements OnInit {
    private readonly service = inject(ReservaService);
    private readonly notificacao = inject(NotificacaoService);

    protected readonly situacoes = STATUS_RESERVA;
    protected readonly rotulo = rotuloStatusReserva;
    protected readonly classe = classeDoStatusReserva;
    protected readonly plano = rotuloPlano;
    protected readonly seguro = rotuloSeguro;
    protected readonly data = isoParaDataBr;

    protected readonly filtro = signal<StatusReservaModel | ''>('Pendente');
    protected readonly pedidos = signal<ReservaModel[]>([]);
    protected readonly lista = new ListaPaginada(this.pedidos);
    protected readonly carregando = signal(true);
    protected readonly erro = signal<string | null>(null);
    protected readonly enviando = signal(false);
    protected readonly respondendo = signal<{ id: string; acao: 'aprovar' | 'recusar' } | null>(null);

    protected kmInicial: number | null = null;
    protected motivo = '';

    ngOnInit(): void {
        this.carregar();
    }

    protected mudarFiltro(valor: StatusReservaModel | ''): void {
        this.filtro.set(valor);
        this.carregar();
    }

    private carregar(): void {
        this.carregando.set(true);
        this.respondendo.set(null);

        this.service.recebidas(this.filtro() || null).subscribe({
            next: (lista) => {
                this.pedidos.set(lista);
                this.carregando.set(false);
            },
            error: (err) => {
                this.erro.set(mensagemDeErro(err, 'Não foi possível carregar as reservas.'));
                this.carregando.set(false);
            },
        });
    }

    protected iniciar(pedido: ReservaModel, acao: 'aprovar' | 'recusar'): void {
        this.kmInicial = null;
        this.motivo = '';
        this.respondendo.set({ id: pedido.id, acao });
    }

    protected confirmar(pedido: ReservaModel): void {
        const resposta = this.respondendo();

        if (!resposta) return;

        if (resposta.acao === 'aprovar' && (this.kmInicial === null || this.kmInicial < 0)) {
            this.notificacao.aviso('Informe a quilometragem atual do carro.');
            return;
        }

        this.enviando.set(true);

        const chamada =
            resposta.acao === 'aprovar'
                ? this.service.aprovar(pedido.id, this.kmInicial!)
                : this.service.recusar(pedido.id, this.motivo.trim() || null);

        chamada.subscribe({
            next: () => {
                this.enviando.set(false);
                this.notificacao.sucesso(
                    resposta.acao === 'aprovar'
                        ? 'Reserva aprovada: o aluguel foi criado.'
                        : 'Reserva recusada.',
                );
                this.carregar();
            },
            error: (err) => {
                this.enviando.set(false);
                this.notificacao.erro(mensagemDeErro(err, 'Não foi possível responder ao pedido.'));
            },
        });
    }
}
