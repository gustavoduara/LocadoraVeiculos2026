import { catchError, Observable, of, switchMap, tap } from 'rxjs';

import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { ActivatedRoute, Router } from '@angular/router';

import { SelecionarGruposVeiculosModel } from '../../grupo-veiculos/grupo-veiculos.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { AutomovelModel, SalvarAutomovelEvento } from '../automovel.models';
import { AutomovelService } from '../automovel.service';
import { AutomovelForm } from '../form/automovel-form';

@Component({
    selector: 'app-editar-automovel',
    imports: [MatCardModule, AutomovelForm],
    templateUrl: './editar-automovel.html',
})
export class EditarAutomovel {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly automovelService = inject(AutomovelService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly automovel = this.route.snapshot.data['automovel'] as AutomovelModel;

    protected readonly grupos = (this.route.snapshot.data['grupos'] ??
        []) as SelecionarGruposVeiculosModel[];

    private salvarExtras(evento: SalvarAutomovelEvento) {
        if (evento.fotosExtras.novas.length === 0 && evento.fotosExtras.removidas.length === 0) return of(null);

        return this.automovelService.salvarFotosExtras(this.automovel.id, evento.fotosExtras).pipe(
            tap((falhas) => {
                if (falhas > 0)
                    this.notificacaoService.aviso(
                        `Os dados foram salvos, mas ${falhas} mudança(s) nas fotos extras não puderam ser feitas. Tente de novo.`,
                    );
            }),
        );
    }

    public editar(evento: SalvarAutomovelEvento) {
        this.automovelService
            .editar(this.automovel.id, evento.dados)
            .pipe(
                switchMap(() => {
                    const falhaDaFoto = (err: unknown) => {
                        this.notificacaoService.aviso(
                            mensagemDeErro(err, 'Os dados foram salvos, mas a foto não pôde ser atualizada.'),
                        );

                        return of(null);
                    };

                    let principal$: Observable<unknown> = of(null);

                    if (evento.foto.acao === 'enviar' && evento.foto.arquivo)
                        principal$ = this.automovelService
                            .enviarFoto(this.automovel.id, evento.foto.arquivo)
                            .pipe(catchError(falhaDaFoto));
                    else if (evento.foto.acao === 'remover')
                        principal$ = this.automovelService
                            .removerFoto(this.automovel.id)
                            .pipe(catchError(falhaDaFoto));

                    return principal$.pipe(switchMap(() => this.salvarExtras(evento)));
                }),
            )
            .subscribe({
                next: () =>
                    this.notificacaoService.sucesso(
                        `O automóvel "${evento.dados.marca} ${evento.dados.modelo}" foi editado com sucesso!`,
                    ),
                error: (err) =>
                    this.notificacaoService.erro(
                        mensagemDeErro(err, 'Não foi possível editar o automóvel.'),
                    ),
                complete: () => this.router.navigate(['/automoveis']),
            });
    }
}
