import { provideNgxMask } from 'ngx-mask';

import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, Routes } from '@angular/router';

import { GrupoVeiculosService } from '../grupo-veiculos/grupo-veiculos.service';
import { AutomovelService } from './automovel.service';
import { CadastrarAutomovel } from './cadastrar/cadastrar-automovel';
import { EditarAutomovel } from './editar/editar-automovel';
import { ExcluirAutomovel } from './excluir/excluir-automovel';
import { ListarAutomoveis } from './listar/listar-automoveis';

export const listarAutomoveisResolver = () => {
    return inject(AutomovelService).selecionarTodos();
};

export const gruposResolver = () => {
    return inject(GrupoVeiculosService).selecionarTodos();
};

export const detalhesAutomovelResolver = (route: ActivatedRouteSnapshot) => {
    const id = route.paramMap.get('id');

    if (!id) throw new Error('O parâmetro id não foi fornecido.');

    return inject(AutomovelService).selecionarPorId(id);
};

export const automovelRoutes: Routes = [
    {
        path: '',
        children: [
            {
                path: '',
                component: ListarAutomoveis,
                resolve: { automoveis: listarAutomoveisResolver, grupos: gruposResolver },
            },
            {
                path: 'cadastrar',
                component: CadastrarAutomovel,
                resolve: { grupos: gruposResolver },
            },
            {
                path: 'editar/:id',
                component: EditarAutomovel,
                resolve: { automovel: detalhesAutomovelResolver, grupos: gruposResolver },
            },
            {
                path: 'excluir/:id',
                component: ExcluirAutomovel,
                resolve: { automovel: detalhesAutomovelResolver },
            },
        ],
        providers: [AutomovelService, GrupoVeiculosService, provideNgxMask()],
    },
];
