import { catchError, of, switchMap } from 'rxjs';

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

                    if (evento.foto.acao === 'enviar' && evento.foto.arquivo)
                        return this.automovelService
                            .enviarFoto(this.automovel.id, evento.foto.arquivo)
                            .pipe(catchError(falhaDaFoto));

                    if (evento.foto.acao === 'remover')
                        return this.automovelService
                            .removerFoto(this.automovel.id)
                            .pipe(catchError(falhaDaFoto));

                    return of(null);
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
