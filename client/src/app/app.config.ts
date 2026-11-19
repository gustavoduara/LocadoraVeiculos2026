import { map, take } from 'rxjs';

import {
  ApplicationConfig, DEFAULT_CURRENCY_CODE, inject, LOCALE_ID, provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection
} from '@angular/core';
import { CanActivateFn, provideRouter, Router, Routes } from '@angular/router';

import { provideAuth } from './auth/auth.provider';
import { AuthService } from './auth/auth.service';
import { cargoGuard } from './auth/cargo.guard';
import { provideNotifications } from './shared/notificacao/notificacao.provider';

const usuarioDesconhecidoGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.obterAccessToken().pipe(
    take(1),
    map((token) => (!token ? true : router.createUrlTree(['/inicio']))),
  );
};

const usuarioAutenticadoGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.obterAccessToken().pipe(
    take(1),
    map((token) => (token ? true : router.createUrlTree(['/auth/login']))),
  );
};

const routes: Routes = [
  { path: '', redirectTo: 'auth/login', pathMatch: 'full' },
  {
    path: 'auth',
    loadChildren: () => import('./auth/auth.routes').then((r) => r.authRoutes),
    canMatch: [usuarioDesconhecidoGuard],
  },

  // Somente o administrador da empresa gerencia funcionários.
  {
    path: 'funcionarios',
    loadChildren: () =>
      import('./funcionarios/funcionario.routes').then((r) => r.funcionarioRoutes),
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Empresa')],
  },

  // Cada funcionário edita apenas o próprio perfil.
  {
    path: 'perfil',
    loadChildren: () =>
      import('./funcionarios/perfil/perfil.routes').then((r) => r.perfilRoutes),
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Funcionario')],
  },

  {
    path: 'grupos-veiculos',
    loadChildren: () =>
      import('./grupo-veiculos/grupo-veiculos.routes').then((r) => r.grupoVeiculosRoutes),
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Empresa', 'Funcionario')],
  },
  // Planos de cobrança por grupo de veículos: somente o administrador da empresa gerencia.
  {
    path: 'planos-cobranca',
    loadChildren: () =>
      import('./planos-cobranca/plano-cobranca.routes').then((r) => r.planoCobrancaRoutes),
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Empresa')],
  },

  // Preço dos combustíveis: somente o administrador da empresa altera.
  {
    path: 'configuracoes',
    loadChildren: () =>
      import('./configuracoes/configuracao.routes').then((r) => r.configuracaoRoutes),
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Empresa')],
  },
  {
    path: 'inicio',
    loadComponent: () => import('./inicio/inicio').then((c) => c.Inicio),
    canMatch: [usuarioAutenticadoGuard],
  },
];

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),

    { provide: LOCALE_ID, useValue: 'pt-BR' },
    { provide: DEFAULT_CURRENCY_CODE, useValue: 'BRL' },

    provideNotifications(),
    provideAuth(),
  ],
};
