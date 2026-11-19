import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, Routes } from '@angular/router';

import { CadastrarTaxaServico } from './cadastrar/cadastrar-taxa-servico';
import { EditarTaxaServico } from './editar/editar-taxa-servico';
import { ExcluirTaxaServico } from './excluir/excluir-taxa-servico';
import { ListarTaxasServicos } from './listar/listar-taxas-servicos';
import { TaxaServicoService } from './taxa-servico.service';

export const listarTaxasResolver = () => {
    return inject(TaxaServicoService).selecionarTodos();
};

export const detalhesTaxaResolver = (route: ActivatedRouteSnapshot) => {
    const id = route.paramMap.get('id');

    if (!id) throw new Error('O parâmetro id não foi fornecido.');

    return inject(TaxaServicoService).selecionarPorId(id);
};

export const taxaServicoRoutes: Routes = [
    {
        path: '',
        children: [
            {
                path: '',
                component: ListarTaxasServicos,
                resolve: { taxas: listarTaxasResolver },
            },
            {
                path: 'cadastrar',
                component: CadastrarTaxaServico,
            },
            {
                path: 'editar/:id',
                component: EditarTaxaServico,
                resolve: { taxa: detalhesTaxaResolver },
            },
            {
                path: 'excluir/:id',
                component: ExcluirTaxaServico,
                resolve: { taxa: detalhesTaxaResolver },
            },
        ],
        providers: [TaxaServicoService],
    },
];
