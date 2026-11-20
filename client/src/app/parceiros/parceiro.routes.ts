import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, Routes } from '@angular/router';

import { CadastrarParceiro } from './cadastrar/cadastrar-parceiro';
import { EditarParceiro } from './editar/editar-parceiro';
import { ExcluirParceiro } from './excluir/excluir-parceiro';
import { ListarParceiros } from './listar/listar-parceiros';
import { ParceiroService } from './parceiro.service';

export const listarParceirosResolver = () => {
    return inject(ParceiroService).selecionarTodos();
};

export const detalhesParceiroResolver = (route: ActivatedRouteSnapshot) => {
    const id = route.paramMap.get('id');

    if (!id) throw new Error('O parâmetro id não foi fornecido.');

    return inject(ParceiroService).selecionarPorId(id);
};

export const parceiroRoutes: Routes = [
    {
        path: '',
        children: [
            {
                path: '',
                component: ListarParceiros,
                resolve: { parceiros: listarParceirosResolver },
            },
            {
                path: 'cadastrar',
                component: CadastrarParceiro,
            },
            {
                path: 'editar/:id',
                component: EditarParceiro,
                resolve: { parceiro: detalhesParceiroResolver },
            },
            {
                path: 'excluir/:id',
                component: ExcluirParceiro,
                resolve: { parceiro: detalhesParceiroResolver },
            },
        ],
        providers: [ParceiroService],
    },
];
