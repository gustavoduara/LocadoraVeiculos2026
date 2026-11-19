import { CurrencyPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { rotuloTipoCobranca, TaxaServicoModel } from '../taxa-servico.models';

@Component({
    selector: 'app-listar-taxas-servicos',
    imports: [MatButtonModule, MatCardModule, MatIconModule, RouterLink, CurrencyPipe],
    templateUrl: './listar-taxas-servicos.html',
})
export class ListarTaxasServicos {
    protected readonly route = inject(ActivatedRoute);

    protected readonly taxas = (this.route.snapshot.data['taxas'] ?? []) as TaxaServicoModel[];

    protected readonly rotuloTipoCobranca = rotuloTipoCobranca;
}
