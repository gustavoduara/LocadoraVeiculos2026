import { provideNgxMask } from 'ngx-mask';

import { inject } from '@angular/core';
import { Routes } from '@angular/router';

import { FuncionarioService } from '../funcionario.service';
import { MeuPerfil } from './meu-perfil';

export const meuPerfilResolver = () => {
    return inject(FuncionarioService).selecionarMeuPerfil();
};

export const perfilRoutes: Routes = [
    {
        path: '',
        component: MeuPerfil,
        resolve: { perfil: meuPerfilResolver },
        providers: [FuncionarioService, provideNgxMask()],
    },
];
