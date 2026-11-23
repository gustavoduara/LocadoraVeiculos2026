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

/** Quanto tempo depois de pôr o mouse em cima vem a primeira troca (uma espera menor, para a pessoa já ver que mexe). */
const PRIMEIRA_TROCA_MS = 1200;

/** Tempo que cada foto fica parada quando o carrossel passa sozinho (mouse em cima). */
const INTERVALO_DO_PASSEIO_MS = 2000;

/** Distância mínima (em pixels) de um arrastar de dedo para contar como "passar a foto". */
const DISTANCIA_DO_TOQUE = 40;

/**
 * Fotos de um carro do catálogo (baixadas com o token) ou um ícone quando o carro não tem foto.
 *
 * Com mais de uma foto vira um carrossel: ao passar o mouse por cima as fotos deslizam sozinhas para o lado, uma a cada
 * ~2 s, como nos stories do Instagram (e voltam à primeira ao tirar o mouse); setas e bolinhas deixam escolher; no
 * celular, as setas ficam sempre visíveis e dá para arrastar. As outras fotos só são baixadas quando a pessoa interage,
 * então a lista de carros continua leve.
 *
 * O "trilho" tem as fotos lado a lado, com uma cópia da primeira no fim: ao passar da última para a primeira a faixa
 * continua deslizando no mesmo sentido (sem varrer tudo de volta) e, quando termina, volta de uma vez para o começo.
 */
@Component({
    selector: 'app-foto-catalogo',
    imports: [MatIconModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        <div class="trilho" [class.sem-transicao]="semTransicao()" [style.transform]="'translateX(' + -posicao() * 100 + '%)'"
             (transitionend)="aoTerminarTransicao($event)">
            @for (foto of slides(); track $index) {
            <div class="slide">
                @if (urls()[foto]; as endereco) {
                <img [src]="endereco" [alt]="descricao + ' (' + (foto + 1) + ' de ' + quantidade + ')'"
                     class="w-full h-full object-cover select-none" draggable="false" />
                } @else {
                <div class="w-full h-full flex items-center justify-center text-gray-400">
                    <mat-icon class="!text-5xl !w-12 !h-12">directions_car</mat-icon>
                </div>
                }
            </div>
            }
        </div>

        @if (quantidade > 1 && urls()[0]) {
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
            <span class="h-1.5 rounded-full bg-white shadow transition-all duration-300"
                  [class.w-4]="$index === indice()" [class.w-1.5]="$index !== indice()"
                  [class.opacity-100]="$index === indice()" [class.opacity-60]="$index !== indice()"></span>
            }
        </div>
        }
    `,
    styles: `
        :host { position: relative; display: block; touch-action: pan-y; }

        .trilho {
            display: flex; width: 100%; height: 100%;
            transition: transform 550ms cubic-bezier(0.4, 0, 0.2, 1);
            will-change: transform;
        }
        .trilho.sem-transicao { transition: none; }
        .slide { flex: 0 0 100%; width: 100%; height: 100%; }

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

        /* Quem pede menos movimento no sistema não vê o deslize (a foto só troca). */
        @media (prefers-reduced-motion: reduce) { .trilho { transition: none; } }
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

    /** Posição no trilho: 0 a quantidade − 1 são as fotos; a posição `quantidade` é a cópia da primeira, no fim. */
    protected readonly posicao = signal(0);
    protected readonly semTransicao = signal(false);

    /** Endereços (blob:) das fotos já baixadas, na ordem; posições ainda não baixadas ficam vazias. */
    protected readonly urls = signal<(string | null)[]>([]);

    /** Qual foto está aparecendo (para as bolinhas): a cópia do fim conta como a primeira. */
    protected readonly indice = () => this.posicao() % Math.max(1, this.quantidade);
    protected readonly posicoes = () => Array.from({ length: Math.max(1, this.quantidade) });

    /** As fotos na ordem do trilho; com mais de uma, a primeira se repete no fim. */
    protected readonly slides = () => {
        const n = Math.max(1, this.quantidade);

        return n > 1 ? [...Array.from({ length: n }, (_, i) => i), 0] : [0];
    };

    private assinaturas: Subscription[] = [];
    private temporizador?: ReturnType<typeof setTimeout>;
    private baixouTodas = false;
    private mouseEmCima = false;
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
        this.mouseEmCima = true;

        if (this.quantidade <= 1 || !this.temFoto) return;

        this.baixarTodas(() => {
            if (this.mouseEmCima) this.agendarTroca(PRIMEIRA_TROCA_MS);
        });
    }

    protected terminarPasseio(): void {
        this.mouseEmCima = false;
        this.pararTemporizador();
        this.voltarAoComeco();
    }

    protected proxima(evento: Event): void {
        evento.stopPropagation();
        this.pararTemporizador();
        this.baixarTodas(() => this.avancar());
    }

    protected anterior(evento: Event): void {
        evento.stopPropagation();
        this.pararTemporizador();
        this.baixarTodas(() => this.retroceder());
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
        this.baixarTodas(() => (delta < 0 ? this.avancar() : this.retroceder()));
    }

    /** Quando o deslize até a cópia da primeira termina, volta de uma vez (sem animar) para a primeira de verdade. */
    protected aoTerminarTransicao(evento: TransitionEvent): void {
        if (evento.target !== evento.currentTarget || evento.propertyName !== 'transform') return;

        if (this.posicao() >= this.quantidade) this.pularPara(0);
    }

    // ---------------- movimento ----------------

    private avancar(): void {
        // Já está na cópia do fim, esperando voltar ao começo.
        if (this.posicao() >= this.quantidade) return;

        this.posicao.update((p) => p + 1);
    }

    private retroceder(): void {
        if (this.posicao() > 0) {
            this.posicao.update((p) => p - 1);

            return;
        }

        // Na primeira: vai (sem animar) para a cópia do fim e desliza de lá para a última.
        this.pularPara(this.quantidade, () => this.posicao.set(this.quantidade - 1));
    }

    private voltarAoComeco(): void {
        if (this.posicao() !== 0) this.pularPara(0);
    }

    /** Muda de posição sem animação; `depois` roda quando a mudança já foi desenhada (com a animação de volta ligada). */
    private pularPara(posicao: number, depois?: () => void): void {
        this.semTransicao.set(true);
        this.posicao.set(posicao);

        requestAnimationFrame(() =>
            requestAnimationFrame(() => {
                this.semTransicao.set(false);
                depois?.();
            }),
        );
    }

    private agendarTroca(atrasoMs: number): void {
        this.pararTemporizador();

        this.temporizador = setTimeout(() => {
            this.avancar();
            this.agendarTroca(INTERVALO_DO_PASSEIO_MS);
        }, atrasoMs);
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
        if (this.temporizador) clearTimeout(this.temporizador);

        this.temporizador = undefined;
    }

    private limpar(): void {
        this.pararTemporizador();
        this.assinaturas.forEach((a) => a.unsubscribe());
        this.assinaturas = [];
        this.baixouTodas = false;
        this.mouseEmCima = false;

        this.urls().forEach((u) => u && URL.revokeObjectURL(u));
        this.urls.set([]);
        this.posicao.set(0);
        this.semTransicao.set(false);
    }
}
