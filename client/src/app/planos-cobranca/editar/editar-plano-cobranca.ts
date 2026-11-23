import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { ActivatedRoute, Router } from '@angular/router';

import { formatarPlaca } from '../../automoveis/automovel.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { PlanoCobrancaForm } from '../form/plano-cobranca-form';
import { CadastrarPlanoCobrancaModel, PlanoCobrancaModel } from '../plano-cobranca.models';
import { PlanoCobrancaService } from '../plano-cobranca.service';

@Component({
    selector: 'app-editar-plano-cobranca',
    imports: [MatCardModule, PlanoCobrancaForm],
    templateUrl: './editar-plano-cobranca.html',
})
export class EditarPlanoCobranca {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly planoService = inject(PlanoCobrancaService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly plano = this.route.snapshot.data['plano'] as PlanoCobrancaModel;

    protected readonly automoveis = [{ id: this.plano.automovelId, nome: `${this.plano.nomeAutomovel} · ${formatarPlaca(this.plano.placa)}` }];

    public editar(model: CadastrarPlanoCobrancaModel) {
        this.planoService
            .editar(this.plano.id, {
                diario: model.diario,
                controlado: model.controlado,
                livre: model.livre,
            })
            .subscribe({
                next: () =>
                    this.notificacaoService.sucesso('O plano de cobrança foi editado com sucesso!'),
                error: (err) =>
                    this.notificacaoService.erro(
                        mensagemDeErro(err, 'Não foi possível editar o plano de cobrança.'),
                    ),
                complete: () => this.router.navigate(['/planos-cobranca']),
            });
    }
}
