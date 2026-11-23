import { addDays, format } from 'date-fns';
import { forkJoin, map } from 'rxjs';

import { AsyncPipe, CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';

import { AluguelModel } from '../alugueis/aluguel.models';
import { AluguelService } from '../alugueis/aluguel.service';
import { AuthService } from '../auth/auth.service';
import { AutomovelModel, formatarPlaca, rotuloCombustivel } from '../automoveis/automovel.models';
import { AutomovelService } from '../automoveis/automovel.service';
import { FotoAutomovel } from '../automoveis/foto/foto-automovel';
import { PlanoCobrancaModel } from '../planos-cobranca/plano-cobranca.models';
import { PlanoCobrancaService } from '../planos-cobranca/plano-cobranca.service';
import { mensagemDeErro } from '../shared/erros/mensagem-erro';

const FORMATO_ISO = 'yyyy-MM-dd';

/** Um carro da frota com o preço do plano do grupo e a disponibilidade no período pesquisado. */
interface CarroVitrine {
    automovel: AutomovelModel;
    /** Menor diária entre os três planos do grupo; vazio quando o grupo não tem plano de cobrança. */
    diariaAPartirDe: number | null;
    /** Só é preenchido depois de uma pesquisa. */
    disponivel: boolean | null;
}

@Component({
    selector: 'app-inicio',
    imports: [
        MatCardModule,
        MatButtonModule,
        MatIconModule,
        MatDividerModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatTooltipModule,
        AsyncPipe,
        CurrencyPipe,
        RouterLink,
        FotoAutomovel,
    ],
    providers: [AutomovelService, PlanoCobrancaService, AluguelService],
    templateUrl: './inicio.html',
    styleUrl: './inicio.scss',
})
export class Inicio {
    private readonly automovelService = inject(AutomovelService);
    private readonly planoService = inject(PlanoCobrancaService);
    private readonly aluguelService = inject(AluguelService);

    protected readonly usuarioAutenticado$ = inject(AuthService)
        .obterAccessToken()
        .pipe(map((x) => x?.usuarioAutenticado));

    protected readonly formatarPlaca = formatarPlaca;
    protected readonly rotuloCombustivel = rotuloCombustivel;

    protected readonly hoje = format(new Date(), FORMATO_ISO);

    // Dados reais da frota.
    protected readonly carregando = signal(true);
    protected readonly erroCarga = signal<string | null>(null);
    private readonly automoveis = signal<AutomovelModel[]>([]);
    private readonly planos = signal<PlanoCobrancaModel[]>([]);
    private readonly alugueisEmAberto = signal<AluguelModel[]>([]);

    // Campos da pesquisa (datas em yyyy-MM-dd, o formato do input de data).
    protected readonly retirada = signal(this.hoje);
    protected readonly devolucao = signal(format(addDays(new Date(), 3), FORMATO_ISO));
    protected readonly grupoId = signal('');

    /** Resultado da última pesquisa: os cards mostram a disponibilidade e o total do período. */
    protected readonly pesquisa = signal<{
        retirada: string;
        devolucao: string;
        dias: number;
        grupoId: string;
    } | null>(null);

    /** Grupos de veículos que têm automóveis (para o filtro de categoria). */
    protected readonly grupos = computed(() => {
        const vistos = new Map<string, string>();

        for (const a of this.automoveis()) vistos.set(a.grupoVeiculosId, a.nomeGrupo);

        return [...vistos].map(([id, nome]) => ({ id, nome })).sort((a, b) => a.nome.localeCompare(b.nome));
    });

    protected readonly erro = computed(() => {
        if (!this.retirada() || !this.devolucao()) return 'Informe as duas datas.';
        if (this.retirada() < this.hoje) return 'A retirada não pode ser no passado.';
        if (this.devolucao() < this.retirada()) return 'A devolução não pode ser antes da retirada.';

        return null;
    });

    protected readonly carros = computed<CarroVitrine[]>(() => {
        const busca = this.pesquisa();
        const menorDiariaPorCarro = new Map(
            this.planos().map((p) => [
                p.automovelId,
                Math.min(p.diario.precoDiaria, p.controlado.precoDiaria, p.livre.precoDiaria),
            ]),
        );

        const lista = this.automoveis()
            .filter((a) => !busca?.grupoId || a.grupoVeiculosId === busca.grupoId)
            .map<CarroVitrine>((automovel) => ({
                automovel,
                diariaAPartirDe: menorDiariaPorCarro.get(automovel.id) ?? null,
                disponivel: busca ? this.estaDisponivel(automovel.id, busca.retirada, busca.devolucao) : null,
            }));

        // Depois de pesquisar, os disponíveis vêm primeiro.
        return busca ? lista.sort((a, b) => Number(b.disponivel) - Number(a.disponivel)) : lista;
    });

    protected readonly totalDisponiveis = computed(() => this.carros().filter((c) => c.disponivel).length);

    constructor() {
        forkJoin({
            automoveis: this.automovelService.selecionarTodos(),
            planos: this.planoService.selecionarTodos(),
            alugueis: this.aluguelService.selecionarTodos('EmAberto'),
        }).subscribe({
            next: ({ automoveis, planos, alugueis }) => {
                this.automoveis.set(automoveis);
                this.planos.set(planos);
                this.alugueisEmAberto.set(alugueis);
                this.carregando.set(false);
            },
            error: (err) => {
                this.erroCarga.set(mensagemDeErro(err, 'Não foi possível carregar a frota.'));
                this.carregando.set(false);
            },
        });
    }

    /**
     * Mesma regra do servidor: o carro está ocupado por um aluguel em aberto cujo período se sobrepõe ao pedido.
     * Um aluguel vencido e ainda não devolvido continua ocupando o carro até hoje.
     */
    private estaDisponivel(automovelId: string, retirada: string, devolucao: string): boolean {
        return !this.alugueisEmAberto().some((a) => {
            if (a.automovelId !== automovelId) return false;

            const fimEfetivo = a.dataRetornoPrevista >= this.hoje ? a.dataRetornoPrevista : this.hoje;

            return a.dataSaida <= devolucao && fimEfetivo >= retirada;
        });
    }

    protected atualizarRetirada(valor: string): void {
        this.retirada.set(valor);

        // Mantém a devolução depois da retirada.
        if (valor && this.devolucao() < valor) this.devolucao.set(valor);
    }

    protected pesquisar(): void {
        if (this.erro()) return;

        // Mesma regra do aluguel: diferença entre as datas, no mínimo 1 diária.
        const dias = Math.max(
            1,
            Math.round((Date.parse(this.devolucao()) - Date.parse(this.retirada())) / 86_400_000),
        );

        this.pesquisa.set({
            retirada: this.retirada(),
            devolucao: this.devolucao(),
            dias,
            grupoId: this.grupoId(),
        });
    }

    protected limpar(): void {
        this.pesquisa.set(null);
        this.grupoId.set('');
    }

    /** Dados que levam o carro e as datas escolhidos para o formulário de aluguel. */
    protected parametrosDoAluguel(carro: CarroVitrine): Record<string, string> {
        const busca = this.pesquisa();

        return busca
            ? { automovelId: carro.automovel.id, retirada: busca.retirada, devolucao: busca.devolucao }
            : { automovelId: carro.automovel.id };
    }
}
