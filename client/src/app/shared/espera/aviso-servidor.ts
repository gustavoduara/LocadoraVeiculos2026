import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { EsperaServidor } from './espera-servidor';

/** Faixa no topo da tela, só enquanto o servidor demora a responder. */
@Component({
    selector: 'app-aviso-servidor',
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        @if (espera.acordando()) {
        <div class="aviso-servidor" role="status" aria-live="polite">
            <span class="roda" aria-hidden="true"></span>
            <span>
                <strong>Acordando o servidor.</strong>
                Na primeira visita isso pode levar até 1 minuto; a página abre sozinha.
            </span>
        </div>
        }
    `,
    styles: `
        .aviso-servidor {
            position: fixed;
            top: 12px;
            left: 50%;
            transform: translateX(-50%);
            z-index: 2000;
            display: flex;
            align-items: center;
            gap: 12px;
            width: max-content;
            max-width: calc(100vw - 24px);
            padding: 12px 18px;
            border-radius: 10px;
            background-color: var(--menu-fundo, #1f3d2b);
            color: #ffffff;
            font-size: 0.95rem;
            line-height: 1.35;
        }

        .roda {
            flex: none;
            width: 18px;
            height: 18px;
            border: 3px solid rgb(255 255 255 / 0.3);
            border-top-color: #ffffff;
            border-radius: 50%;
            animation: girar 0.9s linear infinite;
        }

        @keyframes girar {
            to {
                transform: rotate(360deg);
            }
        }
    `,
})
export class AvisoServidor {
    protected readonly espera = inject(EsperaServidor);
}
