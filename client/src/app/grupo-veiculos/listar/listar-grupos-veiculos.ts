import { filter, map } from 'rxjs';

import { AsyncPipe, CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { SelecionarGruposVeiculosModel } from '../grupo-veiculos.models';

import { BuscaLista, ListaPaginada, PaginacaoLista } from '../../shared/lista/lista-paginada';

@Component({
    selector: 'app-listar-grupos-veiculos',
    imports: [
        BuscaLista,
        PaginacaoLista,
        MatButtonModule,
        MatIconModule,
        MatCardModule,
        RouterLink,
        AsyncPipe,
        CurrencyPipe,
        DecimalPipe,
    ],
    templateUrl: './listar-grupos-veiculos.html',
})
export class ListarGruposVeiculos {
    protected readonly route = inject(ActivatedRoute);

    protected readonly grupos$ = this.route.data.pipe(
        filter((data) => data['grupos']),
        map((data) => data['grupos'] as SelecionarGruposVeiculosModel[]),
    );

    /** Busca por texto e páginas (a lista toda já está carregada na tela). */
    protected readonly lista = new ListaPaginada(
        toSignal(this.grupos$, { initialValue: [] as SelecionarGruposVeiculosModel[] }),
    );
}
