import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { ParceiroModel } from '../parceiro.models';

@Component({
    selector: 'app-listar-parceiros',
    imports: [MatButtonModule, MatCardModule, MatIconModule, RouterLink],
    templateUrl: './listar-parceiros.html',
})
export class ListarParceiros {
    protected readonly route = inject(ActivatedRoute);

    protected readonly parceiros = (this.route.snapshot.data['parceiros'] ?? []) as ParceiroModel[];
}
