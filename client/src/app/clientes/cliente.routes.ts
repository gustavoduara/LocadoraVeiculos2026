import { provideNgxMask } from 'ngx-mask';

import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, Routes } from '@angular/router';

import { ClienteService } from './cliente.service';
import { EditarCliente } from './editar/editar-cliente';
import { ExcluirCliente } from './excluir/excluir-cliente';
import { ListarClientes } from './listar/listar-clientes';

export const listarClientesResolver = () => {
    return inject(ClienteService).selecionarTodos();
};

/** Pessoas jurídicas disponíveis para o vínculo de uma pessoa física. */
export const pessoasJuridicasResolver = () => {
    return inject(ClienteService).selecionarTodos('PessoaJuridica');
};

export const detalhesClienteResolver = (route: ActivatedRouteSnapshot) => {
    const id = route.paramMap.get('id');

    if (!id) throw new Error('O parâmetro id não foi fornecido.');

    return inject(ClienteService).selecionarPorId(id);
};

export const clienteRoutes: Routes = [
    {
        path: '',
        children: [
            {
                path: '',
                component: ListarClientes,
                resolve: { clientes: listarClientesResolver },
            },
            {
                path: 'editar/:id',
                component: EditarCliente,
                resolve: { cliente: detalhesClienteResolver, pessoasJuridicas: pessoasJuridicasResolver },
            },
            {
                path: 'excluir/:id',
                component: ExcluirCliente,
                resolve: { cliente: detalhesClienteResolver },
            },
        ],
        providers: [ClienteService, provideNgxMask()],
    },
];
