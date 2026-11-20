import { Routes } from '@angular/router';

import { BuscarCarros } from './buscar/buscar-carros';
import { CatalogoService } from './catalogo.service';

export const catalogoRoutes: Routes = [
    {
        path: '',
        component: BuscarCarros,
        providers: [CatalogoService],
    },
];
