import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { isoParaDataBr } from '../../condutores/condutor.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { AluguelModel } from '../aluguel.models';
import { AluguelService } from '../aluguel.service';

@Component({
    selector: 'app-excluir-aluguel',
    imports: [MatCardModule, MatButtonModule, MatIconModule, RouterLink],
    templateUrl: './excluir-aluguel.html',
})
export class ExcluirAluguel {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly aluguelService = inject(AluguelService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly aluguel = this.route.snapshot.data['aluguel'] as AluguelModel;

    protected readonly dataBr = isoParaDataBr;

    public excluir() {
        this.aluguelService.excluir(this.aluguel.id).subscribe({
            next: () => this.notificacaoService.sucesso('O registro foi excluído com sucesso!'),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível excluir o aluguel.')),
            complete: () => this.router.navigate(['/alugueis']),
        });
    }
}
