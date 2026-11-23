import { catchError, of, switchMap, tap } from 'rxjs';

import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { SelecionarGruposVeiculosModel } from '../../grupo-veiculos/grupo-veiculos.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { SalvarAutomovelEvento } from '../automovel.models';
import { AutomovelService } from '../automovel.service';
import { AutomovelForm } from '../form/automovel-form';

@Component({
    selector: 'app-cadastrar-automovel',
    imports: [MatCardModule, MatButtonModule, RouterLink, AutomovelForm],
    templateUrl: './cadastrar-automovel.html',
})
export class CadastrarAutomovel {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly automovelService = inject(AutomovelService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly grupos = (this.route.snapshot.data['grupos'] ??
        []) as SelecionarGruposVeiculosModel[];

    private salvarExtras(id: string, evento: SalvarAutomovelEvento) {
        if (evento.fotosExtras.novas.length === 0 && evento.fotosExtras.removidas.length === 0) return of(null);

        return this.automovelService.salvarFotosExtras(id, evento.fotosExtras).pipe(
            tap((falhas) => {
                if (falhas > 0)
                    this.notificacaoService.aviso(
                        `O automóvel foi cadastrado, mas ${falhas} foto(s) extra(s) não foram enviadas. Você pode enviá-las editando o automóvel.`,
                    );
            }),
        );
    }

    public cadastrar(evento: SalvarAutomovelEvento) {
        this.automovelService
            .cadastrar(evento.dados)
            .pipe(
                switchMap((resposta) => {
                    // O veículo já existe: se só a foto falhar, avisa em vez de perder o cadastro.
                    const principal$ =
                        evento.foto.acao !== 'enviar' || !evento.foto.arquivo
                            ? of(null)
                            : this.automovelService.enviarFoto(resposta.id, evento.foto.arquivo).pipe(
                                  catchError((err) => {
                                      this.notificacaoService.aviso(
                                          mensagemDeErro(err, 'O automóvel foi cadastrado, mas a foto não foi enviada.') +
                                              ' Você pode enviá-la editando o automóvel.',
                                      );

                                      return of(null);
                                  }),
                              );

                    return principal$.pipe(switchMap(() => this.salvarExtras(resposta.id, evento)));
                }),
            )
            .subscribe({
                next: () =>
                    this.notificacaoService.sucesso(
                        `O automóvel "${evento.dados.marca} ${evento.dados.modelo}" foi cadastrado com sucesso!`,
                    ),
                error: (err) =>
                    this.notificacaoService.erro(
                        mensagemDeErro(err, 'Não foi possível cadastrar o automóvel.'),
                    ),
                complete: () => this.router.navigate(['/automoveis']),
            });
    }
}
