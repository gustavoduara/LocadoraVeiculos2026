import { provideNgxMask } from 'ngx-mask';

import { Routes } from '@angular/router';

import { ConfirmarEmail } from './confirmar-email/confirmar-email';
import { Login } from './login/login';
import { EsqueciSenha } from './esqueci-senha/esqueci-senha';
import { RedefinirSenha } from './redefinir-senha/redefinir-senha';
import { Registro } from './registro/registro';

export const authRoutes: Routes = [
  { path: 'login', component: Login },
  { path: 'esqueci-senha', component: EsqueciSenha },
  { path: 'redefinir-senha', component: RedefinirSenha },
  { path: 'confirmar-email', component: ConfirmarEmail },
  // Conta de locadora (administrador da empresa).
  { path: 'registro', component: Registro, providers: [provideNgxMask()] },
  // Conta de cliente: procura carros no catálogo.
  {
    path: 'registro-cliente',
    component: Registro,
    providers: [provideNgxMask()],
    data: { tipo: 'cliente' },
  },
];