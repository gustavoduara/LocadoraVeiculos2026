import { Component } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

/** Rodapé do sistema: créditos, contato e link para reportar problemas. */
@Component({
    selector: 'app-rodape',
    imports: [MatIconModule],
    template: `
        <footer class="mt-10 border-t border-gray-300/60 px-4 py-5 text-sm text-gray-600">
            <div
                class="mx-auto flex max-w-screen-xl flex-col items-center gap-3 text-center md:flex-row md:justify-between md:text-left"
            >
                <div>
                    <p class="font-medium text-gray-700">
                        © {{ ano }} Locadora de Veículos · v{{ versao }}
                    </p>
                    <p>
                        Desenvolvido por
                        <a
                            class="font-medium underline-offset-2 hover:underline"
                            [href]="perfilGithub"
                            target="_blank"
                            rel="noopener noreferrer"
                            >Gustavo Duara</a
                        >
                    </p>
                </div>

                <nav class="flex flex-wrap items-center justify-center gap-x-5 gap-y-2" aria-label="Contato">
                    <a
                        class="inline-flex items-center gap-1 hover:underline"
                        [href]="'tel:' + telefoneLink"
                    >
                        <mat-icon class="!h-5 !w-5 !text-[18px]">call</mat-icon>
                        {{ telefone }}
                    </a>

                    <a
                        class="hover:underline"
                        [href]="'https://wa.me/' + telefoneLink.replace('+', '')"
                        target="_blank"
                        rel="noopener noreferrer"
                        >WhatsApp</a
                    >

                    <a class="hover:underline" [href]="perfilGithub" target="_blank" rel="noopener noreferrer"
                        >GitHub</a
                    >

                    <a
                        class="inline-flex items-center gap-1 hover:underline"
                        [href]="repositorio + '/issues'"
                        target="_blank"
                        rel="noopener noreferrer"
                    >
                        <mat-icon class="!h-5 !w-5 !text-[18px]">bug_report</mat-icon>
                        Reportar um problema
                    </a>
                </nav>
            </div>
        </footer>
    `,
})
export class Rodape {
    protected readonly ano = new Date().getFullYear();
    protected readonly versao = '1.0.0';

    protected readonly telefone = '(49) 99113-0685';
    /** Formato internacional, usado nos links tel: e WhatsApp. */
    protected readonly telefoneLink = '+5549991130685';

    protected readonly perfilGithub = 'https://github.com/gustavoduara';
    protected readonly repositorio = 'https://github.com/gustavoduara/LocadoraVeiculos2026';
}
