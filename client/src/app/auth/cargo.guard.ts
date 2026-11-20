import { map, take } from 'rxjs';

import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';

import { AuthService } from './auth.service';
import { rotaInicial } from './rota-inicial';

/** Permite a rota apenas para usuários com um dos cargos informados (ex.: 'Empresa', 'Funcionario'). */
export const cargoGuard = (...cargosPermitidos: string[]): CanMatchFn => {
    return () => {
        const authService = inject(AuthService);
        const router = inject(Router);

        return authService.obterAccessToken().pipe(
            take(1),
            map((token) =>
                token && cargosPermitidos.includes(token.usuarioAutenticado.cargo)
                    ? true
                    : router.createUrlTree([rotaInicial(token?.usuarioAutenticado.cargo)]),
            ),
        );
    };
};
