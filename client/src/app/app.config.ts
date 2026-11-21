import { provideNgxMask } from 'ngx-mask';
import { map, take } from 'rxjs';

import {
  ApplicationConfig, DEFAULT_CURRENCY_CODE, inject, LOCALE_ID, provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection
} from '@angular/core';
import { CanActivateFn, provideRouter, Router, Routes } from '@angular/router';

import { provideAuth } from './auth/auth.provider';
import { AuthService } from './auth/auth.service';
import { cargoGuard } from './auth/cargo.guard';
import { rotaInicial } from './auth/rota-inicial';
import { CatalogoService } from './catalogo/catalogo.service';
import { ReservaService } from './reservas/reserva.service';
import { provideNotifications } from './shared/notificacao/notificacao.provider';

const usuarioDesconhecidoGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.obterAccessToken().pipe(
    take(1),
    map((token) =>
      !token ? true : router.createUrlTree([rotaInicial(token.usuarioAutenticado.cargo)]),
    ),
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

  // Textos legais: abertos a todos, logados ou não (o cadastro e o rodapé apontam para eles).
  {
    path: 'termos-de-uso',
    loadComponent: () => import('./legal/termos-de-uso').then((c) => c.TermosDeUso),
  },
  {
    path: 'privacidade',
    loadComponent: () =>
      import('./legal/politica-de-privacidade').then((c) => c.PoliticaDePrivacidade),
  },
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
  // Automóveis: cadastro operacional, liberado para o administrador e para os funcionários.
  {
    path: 'automoveis',
    loadChildren: () => import('./automoveis/automovel.routes').then((r) => r.automovelRoutes),
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Empresa', 'Funcionario')],
  },

  // Clientes (pessoa física e jurídica): cadastro operacional, liberado para administrador e funcionários.
  {
    path: 'clientes',
    loadChildren: () => import('./clientes/cliente.routes').then((r) => r.clienteRoutes),
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Empresa', 'Funcionario')],
  },

  // Condutores: cadastro operacional, liberado para administrador e funcionários.
  {
    path: 'condutores',
    loadChildren: () => import('./condutores/condutor.routes').then((r) => r.condutorRoutes),
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Empresa', 'Funcionario')],
  },

  // Aluguéis (registro, edição e devolução): operação diária, liberada para administrador e funcionários.
  {
    path: 'alugueis',
    loadChildren: () => import('./alugueis/aluguel.routes').then((r) => r.aluguelRoutes),
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Empresa', 'Funcionario')],
  },

  // Parceiros e cupons de desconto (inclui o ranking de uso): somente o administrador da empresa.
  {
    path: 'parceiros',
    loadChildren: () => import('./parceiros/parceiro.routes').then((r) => r.parceiroRoutes),
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Empresa')],
  },
  {
    path: 'cupons',
    loadChildren: () => import('./cupons/cupom.routes').then((r) => r.cupomRoutes),
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Empresa')],
  },

  // Catálogo de taxas e serviços (GPS, cadeirinha...): somente o administrador da empresa gerencia.
  {
    path: 'taxas-servicos',
    loadChildren: () =>
      import('./taxas-servicos/taxa-servico.routes').then((r) => r.taxaServicoRoutes),
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Empresa')],
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
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Empresa', 'Funcionario')],
  },

  // Qualquer pessoa logada troca a própria senha.
  {
    path: 'conta/senha',
    loadComponent: () =>
      import('./conta/alterar-senha/alterar-senha').then((c) => c.AlterarSenha),
    canMatch: [usuarioAutenticadoGuard],
  },

  // Excluir a própria conta (LGPD): o cliente na hora, o administrador da locadora com prazo de 7 dias.
  {
    path: 'conta/excluir',
    loadComponent: () => import('./conta/excluir-conta/excluir-conta').then((c) => c.ExcluirConta),
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Cliente', 'Empresa')],
  },

  // Catálogo: o cliente procura carros de todas as locadoras.
  {
    path: 'catalogo',
    loadChildren: () => import('./catalogo/catalogo.routes').then((r) => r.catalogoRoutes),
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Cliente')],
  },

  // Reserva online: o cliente completa os dados, reserva e acompanha; a locadora responde.
  {
    path: 'meus-dados',
    loadComponent: () => import('./reservas/meus-dados/meus-dados').then((c) => c.MeusDados),
    providers: [ReservaService, provideNgxMask()],
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Cliente')],
  },
  {
    path: 'reservar/:id',
    loadComponent: () => import('./reservas/reservar/reservar').then((c) => c.Reservar),
    providers: [ReservaService, CatalogoService],
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Cliente')],
  },
  {
    path: 'meus-pedidos',
    loadComponent: () => import('./reservas/meus-pedidos/meus-pedidos').then((c) => c.MeusPedidos),
    providers: [ReservaService],
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Cliente')],
  },
  {
    path: 'reservas',
    loadComponent: () =>
      import('./reservas/recebidas/reservas-recebidas').then((c) => c.ReservasRecebidas),
    providers: [ReservaService],
    canMatch: [usuarioAutenticadoGuard, cargoGuard('Empresa', 'Funcionario')],
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
