import { provideNgxMask } from 'ngx-mask';

import { Routes } from '@angular/router';

import { Login } from './login/login';
import { Registro } from './registro/registro';

export const authRoutes: Routes = [
  { path: 'login', component: Login },
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