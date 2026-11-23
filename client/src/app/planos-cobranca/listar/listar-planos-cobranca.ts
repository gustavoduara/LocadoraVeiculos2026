import { formatarPlaca } from '../../automoveis/automovel.models';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { PlanoCobrancaModel } from '../plano-cobranca.models';

import { BuscaLista, ListaPaginada, PaginacaoLista } from '../../shared/lista/lista-paginada';

@Component({
    selector: 'app-listar-planos-cobranca',
    imports: [
        BuscaLista,
        PaginacaoLista,MatButtonModule, MatIconModule, MatCardModule, RouterLink, CurrencyPipe, DecimalPipe],
    templateUrl: './listar-planos-cobranca.html',
})
export class ListarPlanosCobranca {
    protected readonly route = inject(ActivatedRoute);

    protected readonly planos = (this.route.snapshot.data['planos'] ?? []) as PlanoCobrancaModel[];

    /** Busca por texto e páginas (a lista toda já está carregada na tela). */
    protected readonly lista = new ListaPaginada(this.planos);
    protected readonly formatarPlaca = formatarPlaca;
}
