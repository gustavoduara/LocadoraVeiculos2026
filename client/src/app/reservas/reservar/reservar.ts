import { addDays, format } from 'date-fns';

import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { TipoPlanoModel, TipoSeguroModel } from '../../alugueis/aluguel.models';
import { rotuloCombustivel } from '../../automoveis/automovel.models';
import { CatalogoService } from '../../catalogo/catalogo.service';
import { FotoCatalogo } from '../../catalogo/foto/foto-catalogo';
import { descricaoDoDesconto } from '../../cupons/cupom.models';
import { PrecoComDesconto, ratearDesconto } from '../../shared/preco/preco-com-desconto';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import {
    CarroDetalheModel,
    DadosReservaModel,
    EstimativaReservaModel,
    PerfilClienteModel,
} from '../reserva.models';
import { ReservaService } from '../reserva.service';

const FORMATO_ISO = 'yyyy-MM-dd';

interface OpcaoDePlano {
    valor: TipoPlanoModel;
    titulo: string;
    descricao: string;
}

/** O cliente escolhe plano e seguro, vê o valor previsto na hora e envia o pedido à locadora. */
@Component({
    selector: 'app-reservar',
    imports: [
        CurrencyPipe,
        FormsModule,
        MatButtonModule,
        MatCardModule,
        MatCheckboxModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatRadioModule,
        RouterLink,
        FotoCatalogo,
        PrecoComDesconto,
    ],
    template: `
<section class="mx-auto max-w-4xl px-4 py-6">
    <a routerLink="/catalogo" class="text-sm text-primary inline-flex items-center gap-1">
        <mat-icon class="!h-5 !w-5 !text-[18px]">arrow_back</mat-icon>Voltar à busca
    </a>

    @if (erroCarregar()) {
    <p class="mt-4 rounded-lg px-4 py-3 text-sm selo-vermelho">{{ erroCarregar() }}</p>
    } @else if (carro(); as c) {
    <h2 class="mt-2 text-2xl font-semibold">Reservar {{ c.marca }} {{ c.modelo }}</h2>
    <p class="text-sm text-gray-600">{{ c.locadora }} · {{ c.categoria }}</p>
    <a mat-stroked-button class="mt-2" routerLink="/mensagens/nova" [queryParams]="{ automovelId: c.id }">
        <mat-icon>chat</mat-icon>
        Falar com a locadora
    </a>

    @if (perfil() && !perfil()!.completo) {
    <div class="mt-4 rounded-lg px-4 py-3 text-sm selo-ambar flex flex-wrap items-center justify-between gap-3">
        <span>Para reservar, primeiro complete os seus dados (documentos, telefone e endereço).</span>
        <button mat-flat-button type="button" (click)="completarDados()">Completar meus dados</button>
    </div>
    } @else if (perfil()?.cnhVencida) {
    <p class="mt-4 rounded-lg px-4 py-3 text-sm selo-ambar">
        Sua CNH está vencida. Atualize em "Meus dados": a locadora só libera o carro com a CNH válida até a devolução.
    </p>
    }

    <div class="mt-4 grid grid-cols-1 gap-6 md:grid-cols-5">
        <div class="md:col-span-3 flex flex-col gap-4">
            <mat-card class="rounded-2xl shadow-sm">
                <mat-card-content class="p-4">
                    <h3 class="text-base font-medium">Período</h3>
                    <div class="mt-3 grid grid-cols-1 gap-4 sm:grid-cols-2">
                        <mat-form-field>
                            <mat-label>Retirada</mat-label>
                            <input matInput type="date" [min]="hoje" [ngModel]="retirada()"
                                (ngModelChange)="mudarRetirada($event)" />
                        </mat-form-field>
                        <mat-form-field>
                            <mat-label>Devolução</mat-label>
                            <input matInput type="date" [min]="retirada() || hoje" [ngModel]="devolucao()"
                                (ngModelChange)="mudarDevolucao($event)" />
                        </mat-form-field>
                    </div>
                    @if (erroDeDatas()) {
                    <p class="text-sm text-red-600">{{ erroDeDatas() }}</p>
                    }
                </mat-card-content>
            </mat-card>

            <mat-card class="rounded-2xl shadow-sm">
                <mat-card-content class="p-4">
                    <h3 class="text-base font-medium">Plano de cobrança</h3>
                    <mat-radio-group class="mt-2 flex flex-col" [ngModel]="plano()" (ngModelChange)="mudarPlano($event)">
                        @for (p of planos(); track p.valor) {
                        <mat-radio-button [value]="p.valor">
                            <span class="font-medium">{{ p.titulo }}</span>
                            <span class="block text-xs text-gray-600">{{ p.descricao }}</span>
                        </mat-radio-button>
                        }
                    </mat-radio-group>
                </mat-card-content>
            </mat-card>

            <mat-card class="rounded-2xl shadow-sm">
                <mat-card-content class="p-4">
                    <h3 class="text-base font-medium">Seguro</h3>
                    @if (c.seguroDiaria !== null) {
                    <mat-radio-group class="mt-2 flex flex-col" [ngModel]="seguro()" (ngModelChange)="mudarSeguro($event)">
                        <mat-radio-button value="Nenhum">
                            Sem seguro
                            <span class="block text-xs text-gray-600">
                                Total previsto: {{ totalSemSeguro() !== null ? (totalSemSeguro() | currency: 'BRL') : '…' }}
                            </span>
                        </mat-radio-button>
                        <mat-radio-button value="Contratado">
                            Com seguro
                            <span class="text-xs text-gray-600">· {{ c.seguroDiaria | currency: 'BRL' }} por dia</span>
                            <span class="block text-xs text-gray-600">
                                Total previsto: {{ totalComSeguro() !== null ? (totalComSeguro() | currency: 'BRL') : '…' }}
                            </span>
                        </mat-radio-button>
                    </mat-radio-group>
                    } @else {
                    <p class="mt-2 text-sm text-gray-600">A locadora não oferece seguro para este carro.</p>
                    }
                </mat-card-content>
            </mat-card>

            @if (c.taxas.length > 0) {
            <mat-card class="rounded-2xl shadow-sm">
                <mat-card-content class="p-4">
                    <h3 class="text-base font-medium">Taxas e serviços</h3>
                    <div class="mt-2 flex flex-col">
                        @for (t of c.taxas; track t.id) {
                        <mat-checkbox [checked]="taxasEscolhidas().includes(t.id)"
                            (change)="alternarTaxa(t.id, $event.checked)">
                            {{ t.nome }}
                            <span class="text-xs text-gray-600">
                                · {{ t.preco | currency: 'BRL' }}{{ t.tipoCobranca === 'Diario' ? ' por dia' : ' (uma vez)' }}
                            </span>
                        </mat-checkbox>
                        }
                    </div>
                </mat-card-content>
            </mat-card>
            }

            <mat-card class="rounded-2xl shadow-sm">
                <mat-card-content class="p-4">
                    <h3 class="text-base font-medium">Cupom de desconto</h3>
                    <div class="mt-3 flex items-start gap-3">
                        <mat-form-field class="flex-1" subscriptSizing="dynamic">
                            <mat-label>Tem um cupom desta locadora?</mat-label>
                            <input matInput autocomplete="off" [ngModel]="cupomDigitado()"
                                (ngModelChange)="cupomDigitado.set($event)"
                                (keydown.enter)="aplicarCupom(); $event.preventDefault()" />
                        </mat-form-field>
                        <button mat-stroked-button class="mt-2" type="button" (click)="aplicarCupom()">Aplicar</button>
                    </div>

                    @if (erroCupom(); as erro) {
                    <p class="mt-2 text-sm text-red-700">{{ erro }}</p>
                    } @else if (estimativa()?.cupomNome; as nome) {
                    <p class="mt-2 text-sm text-green-700">
                        Cupom {{ nome }} aplicado: {{ descricaoDesconto(estimativa()!.cupomTipo, estimativa()!.cupomValor) }}
                        sobre diárias, taxas e seguro.
                    </p>
                    }
                </mat-card-content>
            </mat-card>
        </div>

        <div class="md:col-span-2 flex flex-col gap-4">
            <mat-card class="overflow-hidden rounded-2xl shadow-sm">
                <div class="aspect-[16/10] w-full">
                    <app-foto-catalogo [carroId]="c.id" [temFoto]="c.temFoto" [quantidade]="c.quantidadeFotos" [setasSempre]="true"
                        [descricao]="'Foto de ' + c.marca + ' ' + c.modelo" />
                </div>
                <mat-card-content class="p-4 text-sm text-gray-600">
                    {{ c.cor }} · {{ c.ano }} · {{ rotuloCombustivel(c.combustivel) }}
                </mat-card-content>
            </mat-card>

            <mat-card class="rounded-2xl shadow-sm">
                <mat-card-content class="p-4">
                    <h3 class="text-base font-medium">Valor previsto</h3>

                    @if (estimativa(); as e) {
                    <dl class="mt-3 flex flex-col gap-1 text-sm">
                        <div class="flex justify-between"><dt>Diárias ({{ e.dias }} {{ e.dias === 1 ? 'dia' : 'dias' }})</dt>
                            <dd><app-preco-com-desconto [original]="e.valorDiarias" [atual]="precos()!.diarias" /></dd></div>
                        @if (e.valorTaxas > 0) {
                        @for (t of e.taxas; track t.taxaServicoId) {
                        <div class="flex justify-between text-gray-600"><dt>{{ t.nome }}</dt><dd>{{ t.valor | currency: 'BRL' }}</dd></div>
                        }
                        <div class="flex justify-between"><dt>Taxas e serviços</dt>
                            <dd><app-preco-com-desconto [original]="e.valorTaxas" [atual]="precos()!.taxas" /></dd></div>
                        }
                        @if (e.valorSeguro > 0) {
                        <div class="flex justify-between"><dt>Seguro</dt>
                            <dd><app-preco-com-desconto [original]="e.valorSeguro" [atual]="precos()!.seguro" /></dd></div>
                        }
                        @if (e.valorDesconto > 0) {
                        <div class="flex justify-between text-green-700"><dt>Desconto do cupom</dt>
                            <dd>- {{ e.valorDesconto | currency: 'BRL' }}</dd></div>
                        }
                        <div class="flex justify-between"><dt>Caução (incluída)</dt>
                            <dd>{{ e.valorCaucao | currency: 'BRL' }}</dd></div>
                        <div class="mt-2 flex justify-between border-t pt-2 text-base font-semibold">
                            <dt>Total previsto</dt>
                            <dd class="text-destaque"><app-preco-com-desconto [original]="precos()!.totalSemCupom" [atual]="e.valorTotalPrevisto" /></dd>
                        </div>
                    </dl>
                    <p class="mt-3 text-xs text-gray-500">
                        Previsão: quilometragem extra, combustível faltante e atrasos são conferidos na devolução. A locadora
                        precisa aprovar o pedido.
                    </p>
                    } @else if (erroEstimativa()) {
                    <p class="mt-2 text-sm text-red-600">{{ erroEstimativa() }}</p>
                    } @else {
                    <p class="mt-2 text-sm text-gray-500">Escolha o período para ver o valor.</p>
                    }

                    <button mat-flat-button class="mt-4 w-full" type="button"
                        [disabled]="!podeEnviar()" (click)="enviar()">
                        <mat-icon class="!text-base mr-1">send</mat-icon>
                        Enviar pedido de reserva
                    </button>
                </mat-card-content>
            </mat-card>
        </div>
    </div>
    } @else {
    <p class="mt-6 text-sm text-gray-500">Carregando…</p>
    }
</section>
`,
})
export class Reservar implements OnInit {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly catalogo = inject(CatalogoService);
    private readonly reservas = inject(ReservaService);
    private readonly notificacao = inject(NotificacaoService);

    protected readonly hoje = format(new Date(), FORMATO_ISO);
    protected readonly rotuloCombustivel = rotuloCombustivel;

    protected readonly carro = signal<CarroDetalheModel | null>(null);
    protected readonly perfil = signal<PerfilClienteModel | null>(null);
    protected readonly erroCarregar = signal<string | null>(null);

    protected readonly retirada = signal(format(addDays(new Date(), 1), FORMATO_ISO));
    protected readonly devolucao = signal(format(addDays(new Date(), 4), FORMATO_ISO));
    protected readonly plano = signal<TipoPlanoModel>('Diario');
    protected readonly seguro = signal<TipoSeguroModel>('Nenhum');

    protected readonly taxasEscolhidas = signal<string[]>([]);

    /** O que está escrito no campo, e o cupom que já foi aplicado (só vale depois de clicar em Aplicar). */
    protected readonly cupomDigitado = signal('');
    private readonly cupomAplicado = signal('');
    protected readonly erroCupom = signal<string | null>(null);

    protected readonly descricaoDesconto = descricaoDoDesconto;

    protected readonly estimativa = signal<EstimativaReservaModel | null>(null);

    /** Total previsto da opção de seguro que NÃO está marcada, para o cliente comparar com e sem seguro. */
    private readonly totalDaOutraOpcao = signal<number | null>(null);
    private pedidoDaOutraOpcao = 0;

    protected readonly totalSemSeguro = computed(() =>
        this.seguro() === 'Nenhum' ? (this.estimativa()?.valorTotalPrevisto ?? null) : this.totalDaOutraOpcao(),
    );

    protected readonly totalComSeguro = computed(() =>
        this.seguro() === 'Contratado' ? (this.estimativa()?.valorTotalPrevisto ?? null) : this.totalDaOutraOpcao(),
    );
    protected readonly erroEstimativa = signal<string | null>(null);
    protected readonly enviando = signal(false);

    protected readonly planos = computed<OpcaoDePlano[]>(() => {
        const c = this.carro();
        const lista: OpcaoDePlano[] = [];

        if (!c) return lista;

        if (c.diario)
            lista.push({
                valor: 'Diario',
                titulo: 'Diário',
                descricao: `${moeda(c.diario.precoDiaria)} por dia + ${moeda(c.diario.precoPorKm)} por km rodado.`,
            });

        if (c.controlado)
            lista.push({
                valor: 'Controlado',
                titulo: 'Km controlado',
                descricao: `${moeda(c.controlado.precoDiaria)} por dia com ${c.controlado.kmDisponiveisPorDia} km/dia incluídos; acima disso ${moeda(c.controlado.precoPorKmExtrapolado)} por km.`,
            });

        if (c.livre)
            lista.push({
                valor: 'Livre',
                titulo: 'Km livre',
                descricao: `${moeda(c.livre.precoDiaria)} por dia, sem cobrança por quilômetro.`,
            });

        return lista;
    });

    /** Com cupom, o desconto é repartido entre as linhas para mostrar cada preço atualizado. */
    protected readonly precos = computed(() => {
        const e = this.estimativa();

        if (!e) return null;

        const [diarias, taxas, seguro] = ratearDesconto([e.valorDiarias, e.valorTaxas, e.valorSeguro], e.valorDesconto);

        return {
            diarias,
            taxas,
            seguro,
            totalSemCupom: Math.round((e.valorTotalPrevisto + e.valorDesconto) * 100) / 100,
        };
    });

    protected readonly erroDeDatas = computed(() => {
        const r = this.retirada();
        const d = this.devolucao();

        if (!r || !d) return 'Informe a retirada e a devolução.';
        if (r < this.hoje) return 'A retirada não pode ser no passado.';
        if (d < r) return 'A devolução não pode ser antes da retirada.';

        return null;
    });

    protected readonly podeEnviar = computed(
        () =>
            !!this.estimativa() &&
            !this.erroCupom() &&
            !this.erroDeDatas() &&
            !this.enviando() &&
            !!this.perfil()?.completo,
    );

    ngOnInit(): void {
        const id = this.route.snapshot.paramMap.get('id')!;
        const consulta = this.route.snapshot.queryParamMap;

        if (consulta.get('retirada')) this.retirada.set(consulta.get('retirada')!);
        if (consulta.get('devolucao')) this.devolucao.set(consulta.get('devolucao')!);

        this.reservas.obterMeusDados().subscribe({ next: (p) => this.perfil.set(p) });

        this.catalogo.detalhar(id).subscribe({
            next: (carro) => {
                this.carro.set(carro);

                const primeiro = this.planos()[0];

                if (primeiro) this.plano.set(primeiro.valor);

                this.recalcular();
            },
            error: (err) => this.erroCarregar.set(mensagemDeErro(err, 'Não foi possível carregar este carro.')),
        });
    }

    protected mudarRetirada(valor: string): void {
        this.retirada.set(valor);

        if (valor && this.devolucao() && this.devolucao() < valor) this.devolucao.set(valor);

        this.recalcular();
    }

    protected mudarDevolucao(valor: string): void {
        this.devolucao.set(valor);
        this.recalcular();
    }

    protected mudarPlano(valor: TipoPlanoModel): void {
        this.plano.set(valor);
        this.recalcular();
    }

    protected mudarSeguro(valor: TipoSeguroModel): void {
        this.seguro.set(valor);
        this.recalcular();
    }

    protected alternarTaxa(id: string, marcada: boolean): void {
        this.taxasEscolhidas.update((ids) => (marcada ? [...ids, id] : ids.filter((x) => x !== id)));
        this.recalcular();
    }

    protected aplicarCupom(): void {
        this.cupomAplicado.set(this.cupomDigitado().trim());
        this.recalcular();
    }

    protected completarDados(): void {
        this.router.navigate(['/meus-dados'], {
            state: { voltarPara: this.router.url },
        });
    }

    private dados(): DadosReservaModel {
        return {
            automovelId: this.carro()!.id,
            retirada: this.retirada(),
            devolucao: this.devolucao(),
            tipoPlano: this.plano(),
            tipoSeguro: this.seguro(),
            taxasIds: this.taxasEscolhidas(),
            cupom: this.cupomAplicado() || null,
        };
    }

    private recalcular(): void {
        this.estimativa.set(null);
        this.totalDaOutraOpcao.set(null);
        this.erroEstimativa.set(null);
        this.erroCupom.set(null);

        if (!this.carro() || this.erroDeDatas()) return;

        this.calcularOutraOpcao();

        this.reservas.calcular(this.dados()).subscribe({
            next: (e) => this.estimativa.set(e),
            error: (err) => {
                const mensagem = mensagemDeErro(err, 'Não foi possível calcular o valor.');

                if (!this.cupomAplicado()) {
                    this.erroEstimativa.set(mensagem);

                    return;
                }

                // Pode ter sido o cupom: mostra o motivo e continua mostrando o valor sem ele.
                this.erroCupom.set(mensagem);

                this.reservas.calcular({ ...this.dados(), cupom: null }).subscribe({
                    next: (e) => this.estimativa.set(e),
                    error: (err2) => {
                        this.erroCupom.set(null);
                        this.erroEstimativa.set(mensagemDeErro(err2, 'Não foi possível calcular o valor.'));
                    },
                });
            },
        });
    }

    /** Calcula o total da outra opção de seguro (com o mesmo plano, taxas e cupom) só para mostrar a diferença. */
    private calcularOutraOpcao(): void {
        const carro = this.carro();

        if (!carro || carro.seguroDiaria === null) return;

        const pedido = ++this.pedidoDaOutraOpcao;
        const outra: TipoSeguroModel = this.seguro() === 'Nenhum' ? 'Contratado' : 'Nenhum';

        const guardar = (total: number | null) => {
            if (pedido === this.pedidoDaOutraOpcao) this.totalDaOutraOpcao.set(total);
        };

        this.reservas.calcular({ ...this.dados(), tipoSeguro: outra }).subscribe({
            next: (e) => guardar(e.valorTotalPrevisto),
            // Se o cupom não vale para esta opção, mostra o valor sem ele (igual ao que a tela principal faz).
            error: () =>
                this.reservas.calcular({ ...this.dados(), tipoSeguro: outra, cupom: null }).subscribe({
                    next: (e) => guardar(e.valorTotalPrevisto),
                    error: () => guardar(null),
                }),
        });
    }

    protected enviar(): void {
        if (!this.podeEnviar()) return;

        this.enviando.set(true);

        this.reservas.criar(this.dados()).subscribe({
            next: () => {
                this.notificacao.sucesso('Pedido enviado! A locadora vai responder em breve.');
                this.router.navigate(['/meus-pedidos']);
            },
            error: (err) => {
                this.enviando.set(false);
                this.notificacao.erro(mensagemDeErro(err, 'Não foi possível enviar o pedido.'));
            },
        });
    }
}

function moeda(valor: number): string {
    return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}
