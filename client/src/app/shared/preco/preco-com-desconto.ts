import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

function arredondar(valor: number): number {
    return Math.round((valor + Number.EPSILON) * 100) / 100;
}

/**
 * Reparte o desconto de um cupom entre as linhas que ele atinge, na proporção de cada uma.
 * A soma das linhas novas é exatamente a soma das antigas menos o desconto (os centavos que sobram
 * vão para a maior linha), então os números da tela fecham com o total calculado pela API.
 */
export function ratearDesconto(valores: number[], desconto: number): number[] {
    const total = valores.reduce((soma, v) => soma + v, 0);

    if (desconto <= 0 || total <= 0) return [...valores];

    const alvo = arredondar(total - Math.min(desconto, total));
    const novos = valores.map((v) => arredondar((v * alvo) / total));
    const sobra = arredondar(alvo - novos.reduce((soma, v) => soma + v, 0));

    if (sobra !== 0) {
        const maior = valores.indexOf(Math.max(...valores));

        novos[maior] = arredondar(novos[maior] + sobra);
    }

    return novos;
}

/** Um preço em reais: se tem desconto, mostra o valor original riscado e o valor atualizado ao lado. */
@Component({
    selector: 'app-preco-com-desconto',
    imports: [CurrencyPipe],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        @if (comDesconto()) {
        <span class="inline-flex flex-wrap items-baseline justify-end gap-x-2">
            <span class="preco-antigo line-through text-gray-500" aria-label="Preço original">{{
                original() | currency: 'BRL'
            }}</span>
            <span class="preco-novo text-green-700 font-medium">{{ atual() | currency: 'BRL' }}</span>
        </span>
        } @else {
        {{ original() | currency: 'BRL' }}
        }
    `,
})
export class PrecoComDesconto {
    readonly original = input.required<number>();

    /** Valor depois do cupom. Sem informar, é o mesmo valor (nada riscado). */
    readonly atual = input<number | null>(null);

    protected readonly comDesconto = computed(() => {
        const atual = this.atual();

        return atual !== null && atual !== this.original();
    });
}
