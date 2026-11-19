import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { PlanoCobrancaModel } from '../plano-cobranca.models';
import { PlanoCobrancaService } from '../plano-cobranca.service';

@Component({
    selector: 'app-excluir-plano-cobranca',
    imports: [MatCardModule, MatButtonModule, MatIconModule, RouterLink],
    templateUrl: './excluir-plano-cobranca.html',
})
export class ExcluirPlanoCobranca {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly planoService = inject(PlanoCobrancaService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly plano = this.route.snapshot.data['plano'] as PlanoCobrancaModel;

    public excluir() {
        this.planoService.excluir(this.plano.id).subscribe({
            next: () => this.notificacaoService.sucesso('O registro foi excluído com sucesso!'),
            error: (err) =>
                this.notificacaoService.erro(
                    mensagemDeErro(err, 'Não foi possível excluir o plano de cobrança.'),
                ),
            complete: () => this.router.navigate(['/planos-cobranca']),
        });
    }
}
