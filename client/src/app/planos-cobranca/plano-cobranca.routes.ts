import { map } from 'rxjs';

import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, Routes } from '@angular/router';

import { GrupoVeiculosService } from '../grupo-veiculos/grupo-veiculos.service';
import { CadastrarPlanoCobranca } from './cadastrar/cadastrar-plano-cobranca';
import { EditarPlanoCobranca } from './editar/editar-plano-cobranca';
import { ExcluirPlanoCobranca } from './excluir/excluir-plano-cobranca';
import { ListarPlanosCobranca } from './listar/listar-planos-cobranca';
import { PlanoCobrancaService } from './plano-cobranca.service';

export const listarPlanosResolver = () => {
    return inject(PlanoCobrancaService).selecionarTodos();
};

/** Só os grupos que ainda não têm plano (há um plano por grupo). */
export const gruposSemPlanoResolver = () => {
    return inject(GrupoVeiculosService)
        .selecionarTodos()
        .pipe(map((grupos) => grupos.filter((g) => !g.plano)));
};

export const detalhesPlanoResolver = (route: ActivatedRouteSnapshot) => {
    const id = route.paramMap.get('id');

    if (!id) throw new Error('O parâmetro id não foi fornecido.');

    return inject(PlanoCobrancaService).selecionarPorId(id);
};

export const planoCobrancaRoutes: Routes = [
    {
        path: '',
        children: [
            {
                path: '',
                component: ListarPlanosCobranca,
                resolve: { planos: listarPlanosResolver },
            },
            {
                path: 'cadastrar',
                component: CadastrarPlanoCobranca,
                resolve: { grupos: gruposSemPlanoResolver },
            },
            {
                path: 'editar/:id',
                component: EditarPlanoCobranca,
                resolve: { plano: detalhesPlanoResolver },
            },
            {
                path: 'excluir/:id',
                component: ExcluirPlanoCobranca,
                resolve: { plano: detalhesPlanoResolver },
            },
        ],
        providers: [PlanoCobrancaService, GrupoVeiculosService],
    },
];
