import { forkJoin, Subscription } from 'rxjs';

import {
    ChangeDetectionStrategy,
    Component,
    inject,
    Input,
    OnChanges,
    OnDestroy,
    signal,
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { CatalogoService } from '../catalogo.service';

/** Tempo que cada foto fica na tela quando o carrossel passa sozinho (mouse em cima). */
const INTERVALO_DO_PASSEIO_MS = 1100;

/** Distância mínima (em pixels) de um arrastar de dedo para contar como "passar a foto". */
const DISTANCIA_DO_TOQUE = 40;

/**
 * Fotos de um carro do catálogo (baixadas com o token) ou um ícone quando o carro não tem foto.
 *
 * Com mais de uma foto vira um carrossel: ao passar o mouse por cima as fotos vão passando sozinhas (e voltam à primeira
 * ao tirar o mouse); setas e bolinhas deixam escolher; no celular, as setas ficam sempre visíveis e dá para arrastar.
 * As outras fotos só são baixadas quando a pessoa interage, então a lista de carros continua leve.
 */
@Component({
    selector: 'app-foto-catalogo',
    imports: [MatIconModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        @if (urlAtual(); as endereco) {
        <img [src]="endereco" [alt]="descricao + ' (' + (indice() + 1) + ' de ' + quantidade + ')'"
             class="w-full h-full object-cover select-none" draggable="false" />
        } @else {
        <div class="w-full h-full flex items-center justify-center text-gray-400">
            <mat-icon class="!text-5xl !w-12 !h-12">directions_car</mat-icon>
        </div>
        }

        @if (quantidade > 1 && urlAtual()) {
        <button type="button" class="seta seta-esquerda" [class.seta-fixa]="setasSempre" aria-label="Foto anterior"
                (click)="anterior($event)">
            <mat-icon>chevron_left</mat-icon>
        </button>
        <button type="button" class="seta seta-direita" [class.seta-fixa]="setasSempre" aria-label="Próxima foto"
                (click)="proxima($event)">
            <mat-icon>chevron_right</mat-icon>
        </button>

        <div class="absolute bottom-2 left-0 right-0 flex justify-center gap-1.5 pointer-events-none" aria-hidden="true">
            @for (_ of posicoes(); track $index) {
            <span class="h-1.5 w-1.5 rounded-full bg-white shadow"
                  [class.opacity-100]="$index === indice()" [class.opacity-50]="$index !== indice()"
                  [class.scale-125]="$index === indice()"></span>
            }
        </div>
        }
    `,
    styles: `
        :host { position: relative; display: block; touch-action: pan-y; }

        .seta {
            position: absolute; top: 50%; transform: translateY(-50%);
            display: flex; align-items: center; justify-content: center;
            width: 32px; height: 32px; border: 0; border-radius: 9999px; cursor: pointer;
            background: rgb(0 0 0 / 0.5); color: #fff;
            opacity: 0; transition: opacity 150ms;
        }
        .seta-esquerda { left: 8px; }
        .seta-direita { right: 8px; }
        .seta mat-icon { width: 24px; height: 24px; font-size: 24px; }

        /* No computador as setas aparecem com o mouse em cima (ou com o teclado); no celular (sem mouse) ficam sempre à vista. */
        :host(:hover) .seta, .seta:focus-visible, .seta-fixa { opacity: 1; }
        @media (hover: none) { .seta { opacity: 1; } }
    `,
    host: {
        class: 'block overflow-hidden bg-gray-100',
        '(mouseenter)': 'comecarPasseio()',
        '(mouseleave)': 'terminarPasseio()',
        '(touchstart)': 'tocarInicio($event)',
        '(touchend)': 'tocarFim($event)',
    },
})
export class FotoCatalogo implements OnChanges, OnDestroy {
    private readonly catalogoService = inject(CatalogoService);

    @Input({ required: true }) carroId!: string;
    @Input() temFoto = false;
    /** Quantas fotos o carro tem (capa + extras). Sem informar, vale uma só. */
    @Input() quantidade = 1;
    @Input() descricao = 'Foto do carro';
    /** Deixa as setas sempre visíveis (ex.: na tela do carro, onde a pessoa quer folhear). */
    @Input() setasSempre = false;

    protected readonly indice = signal(0);

    /** Endereços (blob:) das fotos já baixadas, na ordem; posições ainda não baixadas ficam vazias. */
    private readonly urls = signal<(string | null)[]>([]);

    protected readonly urlAtual = () => this.urls()[this.indice()] ?? null;
    protected readonly posicoes = () => Array.from({ length: Math.max(1, this.quantidade) });

    private assinaturas: Subscription[] = [];
    private temporizador?: ReturnType<typeof setInterval>;
    private baixouTodas = false;
    private toqueX: number | null = null;

    ngOnChanges(): void {
        this.limpar();

        if (!this.temFoto || !this.carroId) return;

        this.urls.set(Array.from({ length: Math.max(1, this.quantidade) }, () => null));
        this.baixar(0);
    }

    ngOnDestroy(): void {
        this.limpar();
    }

    // ---------------- interação ----------------

    protected comecarPasseio(): void {
        if (this.quantidade <= 1 || !this.temFoto) return;

        this.baixarTodas(() => {
            this.pararTemporizador();
            this.temporizador = setInterval(() => this.irPara((this.indice() + 1) % this.quantidade), INTERVALO_DO_PASSEIO_MS);
        });
    }

    protected terminarPasseio(): void {
        this.pararTemporizador();
        this.indice.set(0);
    }

    protected proxima(evento: Event): void {
        evento.stopPropagation();
        this.pararTemporizador();
        this.baixarTodas(() => this.irPara((this.indice() + 1) % this.quantidade));
    }

    protected anterior(evento: Event): void {
        evento.stopPropagation();
        this.pararTemporizador();
        this.baixarTodas(() => this.irPara((this.indice() - 1 + this.quantidade) % this.quantidade));
    }

    protected tocarInicio(evento: TouchEvent): void {
        this.toqueX = evento.changedTouches[0]?.clientX ?? null;
    }

    protected tocarFim(evento: TouchEvent): void {
        if (this.toqueX === null || this.quantidade <= 1) return;

        const delta = (evento.changedTouches[0]?.clientX ?? this.toqueX) - this.toqueX;
        this.toqueX = null;

        if (Math.abs(delta) < DISTANCIA_DO_TOQUE) return;

        this.pararTemporizador();
        this.baixarTodas(() => this.irPara(delta < 0 ? (this.indice() + 1) % this.quantidade : (this.indice() - 1 + this.quantidade) % this.quantidade));
    }

    private irPara(posicao: number): void {
        // Só mostra a foto quando ela já chegou; senão fica na atual até chegar.
        if (this.urls()[posicao]) this.indice.set(posicao);
    }

    // ---------------- download ----------------

    private baixar(posicao: number): void {
        this.assinaturas.push(
            this.catalogoService.obterFotoDaGaleria(this.carroId, posicao).subscribe({
                next: (blob) => this.urls.update((lista) => lista.map((u, i) => (i === posicao ? URL.createObjectURL(blob) : u))),
                error: () => undefined,
            }),
        );
    }

    /** Baixa as fotos que ainda faltam (uma vez só) e depois executa a ação. */
    private baixarTodas(depois: () => void): void {
        if (this.baixouTodas) {
            depois();
            return;
        }

        this.baixouTodas = true;

        const faltam = this.urls()
            .map((u, i) => (u ? -1 : i))
            .filter((i) => i >= 0);

        if (faltam.length === 0) {
            depois();
            return;
        }

        this.assinaturas.push(
            forkJoin(faltam.map((i) => this.catalogoService.obterFotoDaGaleria(this.carroId, i))).subscribe({
                next: (blobs) => {
                    this.urls.update((lista) => {
                        const nova = [...lista];

                        faltam.forEach((posicao, n) => (nova[posicao] = URL.createObjectURL(blobs[n])));

                        return nova;
                    });
                    depois();
                },
                error: () => (this.baixouTodas = false),
            }),
        );
    }

    private pararTemporizador(): void {
        if (this.temporizador) clearInterval(this.temporizador);

        this.temporizador = undefined;
    }

    private limpar(): void {
        this.pararTemporizador();
        this.assinaturas.forEach((a) => a.unsubscribe());
        this.assinaturas = [];
        this.baixouTodas = false;

        this.urls().forEach((u) => u && URL.revokeObjectURL(u));
        this.urls.set([]);
        this.indice.set(0);
    }
}
