import { Component, computed, input, Signal, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';

/** Sem acento e em minúsculas: "Fábio" encontra "fabio". */
function normalizar(valor: string): string {
    return valor
        .normalize('NFD')
        .replace(/[̀-ͯ]/g, '')
        .toLowerCase();
}

/** Texto pesquisável de um item: todos os textos e números do primeiro nível (ids ficam de fora). */
function textoDoItem(item: unknown): string {
    if (item === null || typeof item !== 'object') return normalizar(String(item ?? ''));

    const partes: string[] = [];

    for (const [chave, valor] of Object.entries(item as Record<string, unknown>)) {
        if (/id$/i.test(chave)) continue;

        if (typeof valor === 'string' || typeof valor === 'number') partes.push(String(valor));
    }

    return normalizar(partes.join(' '));
}

/**
 * Busca por texto + paginação de uma lista que já está na tela.
 * Todas as palavras digitadas precisam aparecer em algum campo do item (sem diferenciar acento nem maiúscula).
 */
export class ListaPaginada<T> {
    private readonly fonte: Signal<T[]>;

    readonly texto = signal('');
    private readonly paginaPedida = signal(1);

    constructor(
        fonte: T[] | Signal<T[]>,
        readonly tamanhoDaPagina = 12,
    ) {
        this.fonte = typeof fonte === 'function' ? fonte : signal(fonte);
    }

    /** Quantos itens a lista tem, sem a busca. */
    readonly totalGeral = computed(() => this.fonte().length);

    readonly filtrados = computed(() => {
        const palavras = normalizar(this.texto()).split(/\s+/).filter(Boolean);
        const itens = this.fonte();

        if (palavras.length === 0) return itens;

        return itens.filter((item) => {
            const texto = textoDoItem(item);

            return palavras.every((p) => texto.includes(p));
        });
    });

    readonly total = computed(() => this.filtrados().length);

    readonly totalDePaginas = computed(() => Math.max(1, Math.ceil(this.total() / this.tamanhoDaPagina)));

    /** A página pedida nunca passa da última (a busca ou uma exclusão pode encolher a lista). */
    readonly pagina = computed(() => Math.min(this.paginaPedida(), this.totalDePaginas()));

    readonly itens = computed(() => {
        const inicio = (this.pagina() - 1) * this.tamanhoDaPagina;

        return this.filtrados().slice(inicio, inicio + this.tamanhoDaPagina);
    });

    buscar(texto: string): void {
        this.texto.set(texto);
        this.paginaPedida.set(1);
    }

    irPara(pagina: number): void {
        this.paginaPedida.set(Math.min(Math.max(1, pagina), this.totalDePaginas()));
    }
}

/** Campo de busca acima da lista, com a contagem de resultados. Some quando a lista está vazia. */
@Component({
    selector: 'app-busca-lista',
    imports: [MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule],
    template: `
        @if (lista().totalGeral() > 0) {
        <div class="mb-4 flex flex-wrap items-center gap-3">
            <mat-form-field class="w-full sm:w-96" subscriptSizing="dynamic">
                <mat-label>{{ rotulo() }}</mat-label>
                <mat-icon matPrefix class="mr-2">search</mat-icon>
                <input matInput type="search" autocomplete="off" [value]="lista().texto()"
                    (input)="lista().buscar($any($event.target).value)" />
                @if (lista().texto()) {
                <button matSuffix mat-icon-button type="button" aria-label="Limpar a busca" (click)="lista().buscar('')">
                    <mat-icon>close</mat-icon>
                </button>
                }
            </mat-form-field>

            <p class="text-sm text-gray-600" aria-live="polite">
                @if (lista().texto()) {
                {{ lista().total() }} de {{ lista().totalGeral() }}
                {{ lista().totalGeral() === 1 ? 'resultado' : 'resultados' }}
                } @else {
                {{ lista().totalGeral() }} {{ lista().totalGeral() === 1 ? 'registro' : 'registros' }}
                }
            </p>
        </div>
        }
    `,
})
export class BuscaLista {
    readonly lista = input.required<ListaPaginada<any>>();
    readonly rotulo = input('Buscar');
}

/** Páginas da lista e o aviso de "nenhum resultado" quando a busca não acha nada. */
@Component({
    selector: 'app-paginacao-lista',
    imports: [MatButtonModule, MatIconModule],
    template: `
        @if (lista().totalGeral() > 0 && lista().total() === 0) {
        <p class="py-6 text-center text-gray-600">
            Nenhum resultado para "{{ lista().texto() }}".
            <button mat-button type="button" (click)="lista().buscar('')">Limpar a busca</button>
        </p>
        }

        @if (lista().totalDePaginas() > 1) {
        <nav class="mt-6 flex items-center justify-center gap-3" aria-label="Páginas da lista">
            <button mat-stroked-button type="button" [disabled]="lista().pagina() <= 1"
                (click)="lista().irPara(lista().pagina() - 1)">
                <mat-icon>chevron_left</mat-icon>
                Anterior
            </button>

            <span class="text-sm text-gray-600">Página {{ lista().pagina() }} de {{ lista().totalDePaginas() }}</span>

            <button mat-stroked-button type="button" [disabled]="lista().pagina() >= lista().totalDePaginas()"
                (click)="lista().irPara(lista().pagina() + 1)">
                Próxima
                <mat-icon iconPositionEnd>chevron_right</mat-icon>
            </button>
        </nav>
        }
    `,
})
export class PaginacaoLista {
    readonly lista = input.required<ListaPaginada<any>>();
}
