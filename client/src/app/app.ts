import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';

import { AuthService } from './auth/auth.service';
import { NotificacaoService } from './shared/notificacao/notificacao.service';
import { AvisoDemonstracao } from './shared/demonstracao/aviso-demonstracao';
import { AvisoServidor } from './shared/espera/aviso-servidor';
import { Rodape } from './shared/rodape/rodape';
import { TemaService } from './shared/tema/tema.service';
import { ShellComponent } from './shared/shell/shell.component';

@Component({
  selector: 'app-root',
  imports: [ShellComponent, RouterOutlet, AsyncPipe, Rodape, AvisoDemonstracao, AvisoServidor],
  template: `
    @if (accessToken$ | async; as accessToken) {
      <app-shell
        [usuarioAutenticado]="accessToken.usuarioAutenticado"
        (logoutRequisitado)="logout()"
      >
        <router-outlet></router-outlet>
      </app-shell>
    } @else {
      <app-aviso-demonstracao />
      <header class="marca-entrada">
        <img src="img/serradrive-logo.png" alt="SerraDrive - Locadora de Veículos" />
      </header>
      <main class="container-fluid py-3">
        <router-outlet></router-outlet>
      </main>
      <app-rodape />
    }

    <app-aviso-servidor />
  `,
})
export class App {
  protected readonly router = inject(Router);
  protected readonly authService = inject(AuthService);
  protected readonly notificacaoService = inject(NotificacaoService);

  // Só de injetar, o modo noturno salvo já é aplicado ao abrir o sistema (inclusive no login).
  protected readonly tema = inject(TemaService);

  protected readonly accessToken$ = this.authService.obterAccessToken();

  protected logout() {
    this.authService.sair().subscribe({
      error: (err) => this.notificacaoService.erro(err.message),
      complete: () => this.router.navigate(['/auth', 'login']),
    });
  }
}