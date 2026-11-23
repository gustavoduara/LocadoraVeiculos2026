import { Observable } from 'rxjs';
import { map, shareReplay } from 'rxjs/operators';

import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { AsyncPipe } from '@angular/common';
import { Component, EventEmitter, inject, Input, Output, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { UsuarioAutenticadoModel } from '../../auth/auth.models';
import { ChatTempoRealService } from '../../chat/chat-tempo-real.service';
import { ContaService } from '../../conta/conta.service';
import { AvisoDemonstracao } from '../demonstracao/aviso-demonstracao';
import { Rodape } from '../rodape/rodape';
import { TemaService } from '../tema/tema.service';

@Component({
    selector: 'app-shell',
    templateUrl: './shell.component.html',
    styleUrl: './shell.component.scss',
    imports: [
        MatToolbarModule,
        MatButtonModule,
        MatSidenavModule,
        MatListModule,
        MatIconModule,
        MatMenuModule,
        AsyncPipe,
        RouterLink,
        RouterLinkActive,
        Rodape,
        AvisoDemonstracao,
    ],
})
export class ShellComponent {
    private readonly breakpointObserver = inject(BreakpointObserver);

    protected readonly tema = inject(TemaService);

    public isHandset$: Observable<boolean> = this.breakpointObserver
        .observe([Breakpoints.XSmall, Breakpoints.Small, Breakpoints.Handset])
        .pipe(
            map((result) => result.matches),
            shareReplay(),
        );

    public itensNavbar = [{ titulo: 'Início', icone: 'home', link: '/inicio' }];

    private readonly contaService = inject(ContaService);
    private readonly chat = inject(ChatTempoRealService);

    /** Mensagens não lidas (selo do menu). */
    protected readonly naoLidas = this.chat.naoLidas;

    /** Locadora com exclusão agendada: o dia em que ela acontece (aparece como aviso no topo). */
    protected readonly exclusaoAgendada = signal<string | null>(null);

    private usuario?: UsuarioAutenticadoModel;

    @Input({ required: true }) set usuarioAutenticado(valor: UsuarioAutenticadoModel | undefined) {
        this.usuario = valor;

        // Chat em tempo real: abre a conexão do lado certo enquanto a pessoa está logada (e fecha ao sair).
        if (valor?.cargo === 'Cliente') {
            void this.chat.conectar('cliente');
        } else if (valor?.cargo === 'Empresa' || valor?.cargo === 'Funcionario') {
            void this.chat.conectar('locadora');
        } else {
            void this.chat.desconectar();
        }

        // Só o administrador da locadora tem exclusão agendada: não pergunta para os outros.
        if (valor?.cargo === 'Empresa') {
            this.contaService.situacao().subscribe({
                next: (situacao) => this.exclusaoAgendada.set(situacao.exclusaoAgendadaParaUtc),
                error: () => this.exclusaoAgendada.set(null),
            });
        } else {
            this.exclusaoAgendada.set(null);
        }
    }

    get usuarioAutenticado(): UsuarioAutenticadoModel | undefined {
        return this.usuario;
    }

    protected dataBr(iso: string): string {
        return new Date(iso).toLocaleDateString('pt-BR');
    }

    @Output() logoutRequisitado = new EventEmitter<void>();
}