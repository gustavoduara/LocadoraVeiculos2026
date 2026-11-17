import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, Routes } from '@angular/router';

import { CadastrarGrupoVeiculos } from './cadastrar/cadastrar-grupo-veiculos';
import { EditarGrupoVeiculos } from './editar/editar-grupo-veiculos';
import { ExcluirGrupoVeiculos } from './excluir/excluir-grupo-veiculos';
import { GrupoVeiculosService } from './grupo-veiculos.service';
import { ListarGruposVeiculos } from './listar/listar-grupos-veiculos';

export const listarGruposVeiculosResolver = () => {
    return inject(GrupoVeiculosService).selecionarTodos();
};

export const detalhesGrupoVeiculosResolver = (route: ActivatedRouteSnapshot) => {
    const id = route.paramMap.get('id');

    if (!id) throw new Error('O parâmetro id não foi fornecido.');

    return inject(GrupoVeiculosService).selecionarPorId(id);
};

export const grupoVeiculosRoutes: Routes = [
    {
        path: '',
        children: [
            {
                path: '',
                component: ListarGruposVeiculos,
                resolve: { grupos: listarGruposVeiculosResolver },
            },
            {
                path: 'cadastrar',
                component: CadastrarGrupoVeiculos,
            },
            {
                path: 'editar/:id',
                component: EditarGrupoVeiculos,
                resolve: { grupo: detalhesGrupoVeiculosResolver },
            },
            {
                path: 'excluir/:id',
                component: ExcluirGrupoVeiculos,
                resolve: { grupo: detalhesGrupoVeiculosResolver },
            },
        ],
        providers: [GrupoVeiculosService],
    },
];
