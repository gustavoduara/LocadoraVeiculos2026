import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { PlanoCobrancaForm } from '../form/plano-cobranca-form';
import { AutomovelOpcaoModel, CadastrarPlanoCobrancaModel } from '../plano-cobranca.models';
import { PlanoCobrancaService } from '../plano-cobranca.service';

@Component({
    selector: 'app-cadastrar-plano-cobranca',
    imports: [MatCardModule, MatButtonModule, RouterLink, PlanoCobrancaForm],
    templateUrl: './cadastrar-plano-cobranca.html',
})
export class CadastrarPlanoCobranca {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly planoService = inject(PlanoCobrancaService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly automoveis = (this.route.snapshot.data['automoveis'] ?? []) as AutomovelOpcaoModel[];

    public cadastrar(model: CadastrarPlanoCobrancaModel) {
        this.planoService.cadastrar(model).subscribe({
            next: () => this.notificacaoService.sucesso('O plano de cobrança foi cadastrado com sucesso!'),
            error: (err) =>
                this.notificacaoService.erro(
                    mensagemDeErro(err, 'Não foi possível cadastrar o plano de cobrança.'),
                ),
            complete: () => this.router.navigate(['/planos-cobranca']),
        });
    }
}
