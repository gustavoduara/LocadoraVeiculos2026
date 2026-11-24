import { addDays, format } from 'date-fns';

import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';

import { COMBUSTIVEIS, rotuloCombustivel, TipoCombustivelModel } from '../../automoveis/automovel.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import {
    FiltrosCatalogoModel,
    OpcoesFiltroCatalogoModel,
    ORDENACOES,
    OrdenacaoCatalogoModel,
    ResultadoCatalogoModel,
} from '../catalogo.models';
import { CatalogoService } from '../catalogo.service';
import { FotoCatalogo } from '../foto/foto-catalogo';
import { Revelar } from '../../shared/revelar/revelar';

const FORMATO_ISO = 'yyyy-MM-dd';
const TAMANHO_DA_PAGINA = 12;

/** O cliente procura carros de todas as locadoras: por período, texto, categoria, combustível, ano, preço e locadora. */
@Component({
    selector: 'app-buscar-carros',
    imports: [
        MatButtonModule,
        MatCardModule,
        MatCheckboxModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatSelectModule,
        CurrencyPipe,
        FotoCatalogo,
        Revelar,
        RouterLink,
    ],
    templateUrl: './buscar-carros.html',
})
export class BuscarCarros implements OnInit {
    private readonly catalogoService = inject(CatalogoService);

    protected readonly hoje = format(new Date(), FORMATO_ISO);

    protected readonly combustiveis = COMBUSTIVEIS;
    protected readonly ordenacoes = ORDENACOES;
    protected readonly rotuloCombustivel = rotuloCombustivel;

    // Campos do formulário.
    /** No celular os filtros ficam recolhidos para os carros aparecerem logo. */
    protected readonly filtrosAbertos = signal(false);

    protected readonly retirada = signal('');
    protected readonly devolucao = signal('');
    protected readonly texto = signal('');
    protected readonly categoria = signal('');
    protected readonly combustivel = signal<TipoCombustivelModel | ''>('');
    protected readonly anoMinimo = signal('');
    protected readonly diariaMaxima = signal('');
    protected readonly locadoraId = signal('');
    protected readonly ordenacao = signal<OrdenacaoCatalogoModel>('MenorPreco');
    protected readonly mostrarIndisponiveis = signal(false);

    // Estado da busca.
    protected readonly opcoes = signal<OpcoesFiltroCatalogoModel>({ categorias: [], marcas: [], locadoras: [] });
    protected readonly resultado = signal<ResultadoCatalogoModel | null>(null);
    protected readonly carregando = signal(false);
    protected readonly erroBusca = signal<string | null>(null);
    private pagina = 1;

    protected readonly erroDeDatas = computed(() => {
        const r = this.retirada();
        const d = this.devolucao();

        if (!r && !d) return null;
        if (!r || !d) return 'Informe a retirada e a devolução juntas.';
        if (r < this.hoje) return 'A retirada não pode ser no passado.';
        if (d < r) return 'A devolução não pode ser antes da retirada.';

        return null;
    });

    protected readonly totalDePaginas = computed(() => {
        const r = this.resultado();

        return r ? Math.max(1, Math.ceil(r.total / r.tamanhoPagina)) : 1;
    });

    protected readonly filtrosAtivos = computed(
        () =>
            !!(
                this.retirada() ||
                this.texto() ||
                this.categoria() ||
                this.combustivel() ||
                this.anoMinimo() ||
                this.diariaMaxima() ||
                this.locadoraId()
            ),
    );

    ngOnInit(): void {
        this.catalogoService.opcoesDeFiltro().subscribe({
            next: (opcoes) => this.opcoes.set(opcoes),
            error: () => this.opcoes.set({ categorias: [], marcas: [], locadoras: [] }),
        });

        this.buscar();
    }

    protected atualizarRetirada(valor: string): void {
        this.retirada.set(valor);

        // Mantém a devolução depois da retirada.
        if (valor && this.devolucao() && this.devolucao() < valor) this.devolucao.set(valor);
    }

    protected sugerirPeriodo(): void {
        this.retirada.set(this.hoje);
        this.devolucao.set(format(addDays(new Date(), 3), FORMATO_ISO));
    }

    protected pesquisar(): void {
        if (this.erroDeDatas()) return;

        this.pagina = 1;
        this.buscar();
    }

    protected limpar(): void {
        this.retirada.set('');
        this.devolucao.set('');
        this.texto.set('');
        this.categoria.set('');
        this.combustivel.set('');
        this.anoMinimo.set('');
        this.diariaMaxima.set('');
        this.locadoraId.set('');
        this.ordenacao.set('MenorPreco');
        this.mostrarIndisponiveis.set(false);

        this.pagina = 1;
        this.buscar();
    }

    protected irParaPagina(pagina: number): void {
        if (pagina < 1 || pagina > this.totalDePaginas()) return;

        this.pagina = pagina;
        this.buscar();
    }

    private buscar(): void {
        const filtros: FiltrosCatalogoModel = {
            retirada: this.retirada() || undefined,
            devolucao: this.devolucao() || undefined,
            texto: this.texto().trim() || undefined,
            categoria: this.categoria() || undefined,
            combustivel: this.combustivel() || undefined,
            anoMinimo: this.anoMinimo() ? Number(this.anoMinimo()) : undefined,
            diariaMaxima: this.diariaMaxima() ? Number(this.diariaMaxima()) : undefined,
            locadoraId: this.locadoraId() || undefined,
            apenasDisponiveis: !this.mostrarIndisponiveis(),
            ordenacao: this.ordenacao(),
            pagina: this.pagina,
            tamanhoPagina: TAMANHO_DA_PAGINA,
        };

        this.carregando.set(true);
        this.erroBusca.set(null);

        this.catalogoService.pesquisar(filtros).subscribe({
            next: (resultado) => {
                this.resultado.set(resultado);
                this.carregando.set(false);
            },
            error: (err) => {
                this.erroBusca.set(mensagemDeErro(err, 'Não foi possível buscar os carros.'));
                this.carregando.set(false);
            },
        });
    }
}
