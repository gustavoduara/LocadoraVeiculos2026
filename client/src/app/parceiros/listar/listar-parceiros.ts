import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { ParceiroModel } from '../parceiro.models';

import { BuscaLista, ListaPaginada, PaginacaoLista } from '../../shared/lista/lista-paginada';

@Component({
    selector: 'app-listar-parceiros',
    imports: [
        BuscaLista,
        PaginacaoLista,MatButtonModule, MatCardModule, MatIconModule, RouterLink],
    templateUrl: './listar-parceiros.html',
})
export class ListarParceiros {
    protected readonly route = inject(ActivatedRoute);

    protected readonly parceiros = (this.route.snapshot.data['parceiros'] ?? []) as ParceiroModel[];

    /** Busca por texto e páginas (a lista toda já está carregada na tela). */
    protected readonly lista = new ListaPaginada(this.parceiros);
}
