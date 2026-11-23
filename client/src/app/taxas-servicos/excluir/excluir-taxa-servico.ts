import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { TaxaServicoModel } from '../taxa-servico.models';
import { TaxaServicoService } from '../taxa-servico.service';

@Component({
    selector: 'app-excluir-taxa-servico',
    imports: [FormsModule, MatCardModule, MatButtonModule, MatIconModule, RouterLink],
    templateUrl: './excluir-taxa-servico.html',
})
export class ExcluirTaxaServico {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly taxaService = inject(TaxaServicoService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly taxa = this.route.snapshot.data['taxa'] as TaxaServicoModel;

    public excluir() {
        this.taxaService.excluir(this.taxa.id).subscribe({
            next: () => this.notificacaoService.sucesso('O registro foi excluído com sucesso!'),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível excluir a taxa.')),
            complete: () => this.router.navigate(['/taxas-servicos']),
        });
    }
}
