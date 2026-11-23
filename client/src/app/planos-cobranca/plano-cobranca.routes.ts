import { forkJoin, map } from 'rxjs';

import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, Routes } from '@angular/router';

import { AutomovelOpcaoModel } from './plano-cobranca.models';
import { AutomovelService } from '../automoveis/automovel.service';
import { formatarPlaca } from '../automoveis/automovel.models';
import { CadastrarPlanoCobranca } from './cadastrar/cadastrar-plano-cobranca';
import { EditarPlanoCobranca } from './editar/editar-plano-cobranca';
import { ExcluirPlanoCobranca } from './excluir/excluir-plano-cobranca';
import { ListarPlanosCobranca } from './listar/listar-planos-cobranca';
import { PlanoCobrancaService } from './plano-cobranca.service';

export const listarPlanosResolver = () => {
    return inject(PlanoCobrancaService).selecionarTodos();
};

/** Só os carros que ainda não têm plano (há um plano por carro). */
export const automoveisSemPlanoResolver = () => {
    return forkJoin([inject(AutomovelService).selecionarTodos(), inject(PlanoCobrancaService).selecionarTodos()]).pipe(
        map(([automoveis, planos]): AutomovelOpcaoModel[] => {
            const comPlano = new Set(planos.map((p) => p.automovelId));

            return automoveis
                .filter((a) => !comPlano.has(a.id))
                .map((a) => ({ id: a.id, nome: `${a.marca} ${a.modelo} · ${formatarPlaca(a.placa)}` }));
        }),
    );
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
                resolve: { automoveis: automoveisSemPlanoResolver },
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
        providers: [PlanoCobrancaService, AutomovelService],
    },
];
