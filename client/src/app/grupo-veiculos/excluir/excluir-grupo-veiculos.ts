import { filter, map, Observer, shareReplay, switchMap, take } from 'rxjs';

import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { DetalhesGrupoVeiculosModel } from '../grupo-veiculos.models';
import { GrupoVeiculosService } from '../grupo-veiculos.service';

@Component({
    selector: 'app-excluir-grupo-veiculos',
    imports: [MatCardModule, MatButtonModule, MatIconModule, RouterLink, AsyncPipe],
    templateUrl: './excluir-grupo-veiculos.html',
})
export class ExcluirGrupoVeiculos {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly grupoService = inject(GrupoVeiculosService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly grupo$ = this.route.data.pipe(
        filter((data) => data['grupo']),
        map((data) => data['grupo'] as DetalhesGrupoVeiculosModel),
        shareReplay({ bufferSize: 1, refCount: true }),
    );

    public excluir() {
        const observer: Observer<null> = {
            next: () => this.notificacaoService.sucesso('O registro foi excluído com sucesso!'),
            error: (err) =>
                this.notificacaoService.erro(
                    mensagemDeErro(err, 'Não foi possível excluir o grupo de veículos.'),
                ),
            complete: () => this.router.navigate(['/grupos-veiculos']),
        };

        this.grupo$
            .pipe(
                take(1),
                switchMap((grupo) => this.grupoService.excluir(grupo.id)),
            )
            .subscribe(observer);
    }
}
