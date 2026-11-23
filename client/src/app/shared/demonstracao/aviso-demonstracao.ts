import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { environment } from '../../../environments/environment';

/**
 * Aviso de que o site é uma demonstração: os carros, as locadoras e as reservas não são reais.
 * Aparece no topo de todas as telas; com `detalhado` vira o pedido para não usar dados pessoais verdadeiros,
 * nas telas que pedem esses dados (cadastro e "Meus dados"). Para ligar/desligar, use `demonstracao` nos arquivos de `environments/`.
 */
@Component({
    selector: 'app-aviso-demonstracao',
    imports: [MatIconModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        @if (ativo) {
        <aside role="note" class="selo-ambar flex items-start gap-2 px-4 py-2 text-sm" [class.rounded-lg]="detalhado" [class.my-3]="detalhado">
            <mat-icon class="!h-5 !w-5 !text-[20px] shrink-0">info</mat-icon>
            @if (detalhado) {
            <p>
                <strong>Não informe dados pessoais verdadeiros.</strong>
                Este site é uma demonstração: use CPF, CNH, telefone e e-mail de teste.
            </p>
            } @else {
            <p>
                <strong>Projeto de demonstração.</strong>
                Os carros, as locadoras e as reservas daqui <strong>não são reais</strong>.
            </p>
            }
        </aside>
        }
    `,
    host: { class: 'block' },
})
export class AvisoDemonstracao {
    protected readonly ativo = environment.demonstracao;

    /** Acrescenta o pedido para não usar dados pessoais verdadeiros (telas de cadastro e "Meus dados"). */
    @Input() detalhado = false;
}
