import { Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';

import { DATA_DOS_TERMOS } from './legal.constantes';

/** Moldura das páginas de texto legal: título, data da versão, o texto e os links para as duas páginas. */
@Component({
    selector: 'app-pagina-legal',
    imports: [MatIconModule, RouterLink],
    template: `
        <section class="mx-auto max-w-3xl px-4 py-8">
            <h1 class="text-2xl font-semibold">{{ titulo() }}</h1>
            <p class="mt-1 text-sm text-gray-600">Versão de {{ data }}</p>
            <hr class="my-4 border-muted/30" />

            <article class="texto-legal">
                <ng-content />
            </article>

            <nav class="mt-8 flex flex-wrap gap-x-6 gap-y-2 border-t border-muted/30 pt-4 text-sm" aria-label="Textos legais">
                <a class="text-primary hover:underline" routerLink="/termos-de-uso">Termos de Uso</a>
                <a class="text-primary hover:underline" routerLink="/privacidade">Política de Privacidade</a>
            </nav>
        </section>
    `,
})
export class PaginaLegal {
    readonly titulo = input.required<string>();

    protected readonly data = DATA_DOS_TERMOS;
}
