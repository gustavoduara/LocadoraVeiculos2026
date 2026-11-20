import { provideNgxMask } from 'ngx-mask';

import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, Routes } from '@angular/router';

import { ParceiroService } from '../parceiros/parceiro.service';
import { CadastrarCupom } from './cadastrar/cadastrar-cupom';
import { CupomService } from './cupom.service';
import { EditarCupom } from './editar/editar-cupom';
import { ExcluirCupom } from './excluir/excluir-cupom';
import { ListarCupons } from './listar/listar-cupons';

export const listarCuponsResolver = () => inject(CupomService).selecionarTodos();

export const rankingCuponsResolver = () => inject(CupomService).maisUtilizados();

export const parceirosResolver = () => inject(ParceiroService).selecionarTodos();

export const detalhesCupomResolver = (route: ActivatedRouteSnapshot) => {
    const id = route.paramMap.get('id');

    if (!id) throw new Error('O parâmetro id não foi fornecido.');

    return inject(CupomService).selecionarPorId(id);
};

export const cupomRoutes: Routes = [
    {
        path: '',
        children: [
            {
                path: '',
                component: ListarCupons,
                resolve: { cupons: listarCuponsResolver, ranking: rankingCuponsResolver },
            },
            {
                path: 'cadastrar',
                component: CadastrarCupom,
                resolve: { parceiros: parceirosResolver },
            },
            {
                path: 'editar/:id',
                component: EditarCupom,
                resolve: { cupom: detalhesCupomResolver, parceiros: parceirosResolver },
            },
            {
                path: 'excluir/:id',
                component: ExcluirCupom,
                resolve: { cupom: detalhesCupomResolver },
            },
        ],
        providers: [CupomService, ParceiroService, provideNgxMask()],
    },
];
