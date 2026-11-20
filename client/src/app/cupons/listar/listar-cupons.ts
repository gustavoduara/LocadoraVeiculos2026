import { CurrencyPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { isoParaDataBr } from '../../condutores/condutor.models';
import { CupomModel, CuponsMaisUtilizadosModel } from '../cupom.models';

@Component({
    selector: 'app-listar-cupons',
    imports: [MatButtonModule, MatCardModule, MatIconModule, RouterLink, CurrencyPipe],
    templateUrl: './listar-cupons.html',
})
export class ListarCupons {
    protected readonly route = inject(ActivatedRoute);

    protected readonly cupons = (this.route.snapshot.data['cupons'] ?? []) as CupomModel[];

    protected readonly ranking = (this.route.snapshot.data['ranking'] ?? {
        cupons: [],
        parceiros: [],
    }) as CuponsMaisUtilizadosModel;

    protected readonly dataBr = isoParaDataBr;
}
