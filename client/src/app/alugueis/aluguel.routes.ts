import { provideNgxMask } from 'ngx-mask';

import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, Routes } from '@angular/router';

import { AutomovelService } from '../automoveis/automovel.service';
import { ClienteService } from '../clientes/cliente.service';
import { CupomService } from '../cupons/cupom.service';
import { CondutorService } from '../condutores/condutor.service';
import { GrupoVeiculosService } from '../grupo-veiculos/grupo-veiculos.service';
import { TaxaServicoService } from '../taxas-servicos/taxa-servico.service';
import { AluguelService } from './aluguel.service';
import { CadastrarAluguel } from './cadastrar/cadastrar-aluguel';
import { DevolucaoAluguel } from './devolucao/devolucao-aluguel';
import { EditarAluguel } from './editar/editar-aluguel';
import { ExcluirAluguel } from './excluir/excluir-aluguel';
import { ListarAlugueis } from './listar/listar-alugueis';

export const listarAlugueisResolver = () => inject(AluguelService).selecionarTodos();

export const detalhesAluguelResolver = (route: ActivatedRouteSnapshot) => {
    const id = route.paramMap.get('id');

    if (!id) throw new Error('O parâmetro id não foi fornecido.');

    return inject(AluguelService).selecionarPorId(id);
};

/** Tudo o que o formulário precisa para montar as listas de seleção. */
const dadosDoFormulario = {
    clientes: () => inject(ClienteService).selecionarTodos(),
    condutores: () => inject(CondutorService).selecionarTodos(),
    grupos: () => inject(GrupoVeiculosService).selecionarTodos(),
    automoveis: () => inject(AutomovelService).selecionarTodos(),
    taxas: () => inject(TaxaServicoService).selecionarTodos(),
    cupons: () => inject(CupomService).selecionarTodos(),
};

export const aluguelRoutes: Routes = [
    {
        path: '',
        children: [
            {
                path: '',
                component: ListarAlugueis,
                resolve: { alugueis: listarAlugueisResolver },
            },
            {
                path: 'cadastrar',
                component: CadastrarAluguel,
                resolve: dadosDoFormulario,
            },
            {
                path: 'editar/:id',
                component: EditarAluguel,
                resolve: { aluguel: detalhesAluguelResolver, ...dadosDoFormulario },
            },
            {
                path: 'devolucao/:id',
                component: DevolucaoAluguel,
                resolve: { aluguel: detalhesAluguelResolver },
            },
            {
                path: 'excluir/:id',
                component: ExcluirAluguel,
                resolve: { aluguel: detalhesAluguelResolver },
            },
        ],
        providers: [
            AluguelService,
            ClienteService,
            CondutorService,
            GrupoVeiculosService,
            AutomovelService,
            TaxaServicoService,
            CupomService,
            provideNgxMask(),
        ],
    },
];
