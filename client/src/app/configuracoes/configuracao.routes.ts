import { inject } from '@angular/core';
import { Routes } from '@angular/router';

import { ConfiguracaoService } from './configuracao.service';
import { EditarConfiguracao } from './editar/editar-configuracao';

export const configuracaoResolver = () => {
    return inject(ConfiguracaoService).selecionar();
};

export const configuracaoRoutes: Routes = [
    {
        path: '',
        component: EditarConfiguracao,
        resolve: { configuracao: configuracaoResolver },
        providers: [ConfiguracaoService],
    },
];
