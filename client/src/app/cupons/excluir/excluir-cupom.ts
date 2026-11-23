import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { CupomModel } from '../cupom.models';
import { CupomService } from '../cupom.service';

@Component({
    selector: 'app-excluir-cupom',
    imports: [FormsModule, MatCardModule, MatButtonModule, MatIconModule, RouterLink],
    templateUrl: './excluir-cupom.html',
})
export class ExcluirCupom {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly cupomService = inject(CupomService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly cupom = this.route.snapshot.data['cupom'] as CupomModel;

    public excluir() {
        this.cupomService.excluir(this.cupom.id).subscribe({
            next: () => this.notificacaoService.sucesso('O registro foi excluído com sucesso!'),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível excluir o cupom.')),
            complete: () => this.router.navigate(['/cupons']),
        });
    }
}
