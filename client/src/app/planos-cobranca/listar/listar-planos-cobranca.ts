import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { PlanoCobrancaModel } from '../plano-cobranca.models';

@Component({
    selector: 'app-listar-planos-cobranca',
    imports: [MatButtonModule, MatIconModule, MatCardModule, RouterLink, CurrencyPipe, DecimalPipe],
    templateUrl: './listar-planos-cobranca.html',
})
export class ListarPlanosCobranca {
    protected readonly route = inject(ActivatedRoute);

    protected readonly planos = (this.route.snapshot.data['planos'] ?? []) as PlanoCobrancaModel[];
}
