import { Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';

interface LinkRodape {
    rotulo: string;
    rota: string;
}

/**
 * Rodapé do sistema, com fundo próprio: marca, atalhos (só para quem está logado, conforme o perfil),
 * contato e projeto, e uma linha final com créditos.
 */
@Component({
    selector: 'app-rodape',
    imports: [MatIconModule, RouterLink],
    template: `
        <footer class="mt-12 rounded-t-[32px] bg-[var(--rodape-fundo)] px-6 pb-6 pt-10 text-sm text-[color:var(--rodape-texto)]">
            <div class="mx-auto max-w-screen-xl">
                <!-- Marca -->
                <img class="h-14 w-auto" src="img/serradrive-logo.png" alt="SerraDrive - Locadora de Veículos" />
                <p class="mt-2 max-w-xl">
                    Gestão de frota, clientes e aluguéis em um só lugar, com cálculo automático de diárias,
                    taxas, seguro, cupons e devolução.
                </p>

                <!-- Colunas -->
                <div class="mt-8 grid grid-cols-2 gap-x-6 gap-y-8 md:grid-cols-4">
                    @if (cargo() === 'Cliente') {
                    <nav aria-label="Cliente">
                        <h3 class="mb-3 font-semibold text-[color:var(--rodape-titulo)]">Para você</h3>
                        <ul class="space-y-2">
                            <li><a class="link-rodape" routerLink="/catalogo">Buscar carros</a></li>
                        </ul>
                    </nav>
                    } @else if (cargo()) {
                    <nav aria-label="Operação">
                        <h3 class="mb-3 font-semibold text-[color:var(--rodape-titulo)]">Operação</h3>
                        <ul class="space-y-2">
                            @for (link of operacao; track link.rota) {
                            <li><a class="link-rodape" [routerLink]="link.rota">{{ link.rotulo }}</a></li>
                            }
                        </ul>
                    </nav>

                    <nav aria-label="Gestão">
                        <h3 class="mb-3 font-semibold text-[color:var(--rodape-titulo)]">{{ cargo() === 'Empresa' ? 'Gestão' : 'Conta' }}</h3>
                        <ul class="space-y-2">
                            @if (cargo() === 'Empresa') { @for (link of gestao; track link.rota) {
                            <li><a class="link-rodape" [routerLink]="link.rota">{{ link.rotulo }}</a></li>
                            } } @else {
                            <li><a class="link-rodape" routerLink="/perfil">Meu Perfil</a></li>
                            }
                        </ul>
                    </nav>
                    }

                    <nav aria-label="Contato">
                        <h3 class="mb-3 font-semibold text-[color:var(--rodape-titulo)]">Contato</h3>
                        <ul class="space-y-2">
                            <li>
                                <a class="link-rodape" [href]="'tel:' + telefoneLink">{{ telefone }}</a>
                            </li>
                            <li>
                                <a
                                    class="link-rodape"
                                    [href]="'https://wa.me/' + telefoneLink.replace('+', '')"
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    >WhatsApp</a
                                >
                            </li>
                        </ul>
                    </nav>

                    <nav aria-label="Projeto">
                        <h3 class="mb-3 font-semibold text-[color:var(--rodape-titulo)]">Projeto</h3>
                        <ul class="space-y-2">
                            <li><a class="link-rodape" routerLink="/termos-de-uso">Termos de Uso</a></li>
                            <li><a class="link-rodape" routerLink="/privacidade">Privacidade</a></li>
                            <li>
                                <a class="link-rodape" [href]="perfilGithub" target="_blank" rel="noopener noreferrer"
                                    >GitHub</a
                                >
                            </li>
                            <li>
                                <a class="link-rodape" [href]="repositorio" target="_blank" rel="noopener noreferrer"
                                    >Código-fonte</a
                                >
                            </li>
                            <li>
                                <a
                                    class="link-rodape"
                                    [href]="repositorio + '/issues'"
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    >Reportar um problema</a
                                >
                            </li>
                        </ul>
                    </nav>
                </div>

                <!-- Linha final -->
                <div
                    class="mt-8 flex flex-col gap-2 border-t border-[color:var(--rodape-linha)] pt-5 text-xs md:flex-row md:items-center md:justify-between"
                >
                    <p>
                        © {{ ano }} SerraDrive · v{{ versao }} · Desenvolvido por
                        <a
                            class="font-medium text-[color:var(--rodape-titulo)] hover:underline"
                            [href]="perfilGithub"
                            target="_blank"
                            rel="noopener noreferrer"
                            >Gustavo Duara</a
                        >
                    </p>
                    <p>.NET 8 · Angular 20 · SQL Server</p>
                </div>
            </div>
        </footer>
    `,
    styles: `
        .link-rodape {
            color: var(--rodape-link);
        }
        .link-rodape:hover {
            text-decoration: underline;
        }
    `,
})
export class Rodape {
    /** Perfil do usuário logado ("Empresa" ou "Funcionario"); vazio nas telas de login e registro. */
    public readonly cargo = input<string | null>(null);

    protected readonly ano = new Date().getFullYear();
    protected readonly versao = '1.0.0';

    protected readonly telefone = '(49) 99113-0685';
    /** Formato internacional, usado nos links tel: e WhatsApp. */
    protected readonly telefoneLink = '+5549991130685';

    protected readonly perfilGithub = 'https://github.com/gustavoduara';
    protected readonly repositorio = 'https://github.com/gustavoduara/LocadoraVeiculos2026';

    protected readonly operacao: LinkRodape[] = [
        { rotulo: 'Aluguéis', rota: '/alugueis' },
        { rotulo: 'Clientes', rota: '/clientes' },
        { rotulo: 'Condutores', rota: '/condutores' },
        { rotulo: 'Automóveis', rota: '/automoveis' },
    ];

    protected readonly gestao: LinkRodape[] = [
        { rotulo: 'Planos de Cobrança', rota: '/planos-cobranca' },
        { rotulo: 'Taxas e Serviços', rota: '/taxas-servicos' },
        { rotulo: 'Parceiros', rota: '/parceiros' },
        { rotulo: 'Cupons', rota: '/cupons' },
        { rotulo: 'Funcionários', rota: '/funcionarios' },
        { rotulo: 'Configurações', rota: '/configuracoes' },
    ];
}
