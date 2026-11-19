import { provideNgxMask } from 'ngx-mask';

import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, Routes } from '@angular/router';

import { ClienteService } from '../clientes/cliente.service';
import { CadastrarCondutor } from './cadastrar/cadastrar-condutor';
import { CondutorService } from './condutor.service';
import { EditarCondutor } from './editar/editar-condutor';
import { ExcluirCondutor } from './excluir/excluir-condutor';
import { ListarCondutores } from './listar/listar-condutores';

export const listarCondutoresResolver = () => {
    return inject(CondutorService).selecionarTodos();
};

export const clientesResolver = () => {
    return inject(ClienteService).selecionarTodos();
};

export const detalhesCondutorResolver = (route: ActivatedRouteSnapshot) => {
    const id = route.paramMap.get('id');

    if (!id) throw new Error('O parâmetro id não foi fornecido.');

    return inject(CondutorService).selecionarPorId(id);
};

export const condutorRoutes: Routes = [
    {
        path: '',
        children: [
            {
                path: '',
                component: ListarCondutores,
                resolve: { condutores: listarCondutoresResolver, clientes: clientesResolver },
            },
            {
                path: 'cadastrar',
                component: CadastrarCondutor,
                resolve: { clientes: clientesResolver },
            },
            {
                path: 'editar/:id',
                component: EditarCondutor,
                resolve: { condutor: detalhesCondutorResolver, clientes: clientesResolver },
            },
            {
                path: 'excluir/:id',
                component: ExcluirCondutor,
                resolve: { condutor: detalhesCondutorResolver },
            },
        ],
        providers: [CondutorService, ClienteService, provideNgxMask()],
    },
];
