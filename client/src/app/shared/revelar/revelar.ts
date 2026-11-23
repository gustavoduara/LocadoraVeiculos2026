import { Directive, ElementRef, inject, OnDestroy, OnInit } from '@angular/core';

/** Distância (em pixels) de onde o item "sobe" até o lugar dele. */
const DESLOCAMENTO_PX = 28;

/** Duração da entrada de cada item. */
const DURACAO_MS = 550;

/** Espera extra entre itens que aparecem juntos (a "cascata"), com um teto para a lista não demorar a surgir. */
const ESPACO_ENTRE_ITENS_MS = 90;
const MAXIMO_DE_ITENS_NA_CASCATA = 6;

/** Grupos de itens que aparecem juntos: um único observador por tela, para a lista de 50 cards não criar 50. */
const grupos = new Map<Element, (posicao: number) => void>();
let observador: IntersectionObserver | null = null;

function observar(elemento: Element, revelar: (posicao: number) => void): void {
    observador ??= new IntersectionObserver(
        (entradas) => {
            const visiveis = entradas
                .filter((e) => e.isIntersecting)
                // Ordem de leitura (de cima para baixo, da esquerda para a direita), para a cascata fazer sentido.
                .sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top || a.boundingClientRect.left - b.boundingClientRect.left);

            visiveis.forEach((entrada, posicao) => {
                const aoRevelar = grupos.get(entrada.target);

                grupos.delete(entrada.target);
                observador?.unobserve(entrada.target);

                aoRevelar?.(posicao);
            });
        },
        // Só conta quando uns 40 px do item já entraram na tela (aparece "ao chegar", não só encostando na borda).
        { threshold: 0.1, rootMargin: '0px 0px -40px 0px' },
    );

    grupos.set(elemento, revelar);
    observador.observe(elemento);
}

/**
 * Faz o item surgir (subindo e ganhando cor) quando ele entra na tela ao rolar a página, em vez de já aparecer todo de uma vez.
 *
 * Itens que entram juntos (a primeira fileira, por exemplo) aparecem em "cascata". Quem pede menos movimento no sistema
 * (prefers-reduced-motion) ou usa um navegador sem IntersectionObserver vê tudo normal, sem animação nenhuma.
 *
 * Use num elemento que não tenha o próprio `opacity`/`transform`: o ideal é um `<div>` em volta do card.
 */
@Directive({ selector: '[appRevelar]' })
export class Revelar implements OnInit, OnDestroy {
    private readonly elemento: HTMLElement = inject(ElementRef).nativeElement;

    ngOnInit(): void {
        const semMovimento = typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;

        if (semMovimento || typeof IntersectionObserver === 'undefined' || typeof this.elemento.animate !== 'function') return;

        // Escondido até o observador avisar; o `backwards` da animação segura esse estado durante a espera da cascata.
        this.elemento.style.opacity = '0';

        observar(this.elemento, (posicao) => this.revelar(posicao));
    }

    ngOnDestroy(): void {
        grupos.delete(this.elemento);
        observador?.unobserve(this.elemento);
    }

    private revelar(posicao: number): void {
        this.elemento.animate(
            [
                { opacity: 0, transform: `translateY(${DESLOCAMENTO_PX}px) scale(0.98)` },
                { opacity: 1, transform: 'none' },
            ],
            {
                duration: DURACAO_MS,
                delay: Math.min(posicao, MAXIMO_DE_ITENS_NA_CASCATA) * ESPACO_ENTRE_ITENS_MS,
                easing: 'cubic-bezier(0.2, 0.7, 0.2, 1)',
                fill: 'backwards',
            },
        );

        this.elemento.style.opacity = '';
    }
}
