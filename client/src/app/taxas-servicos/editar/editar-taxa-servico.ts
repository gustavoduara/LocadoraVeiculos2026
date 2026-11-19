import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { ActivatedRoute, Router } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { TaxaServicoForm } from '../form/taxa-servico-form';
import { CadastrarTaxaServicoModel, TaxaServicoModel } from '../taxa-servico.models';
import { TaxaServicoService } from '../taxa-servico.service';

@Component({
    selector: 'app-editar-taxa-servico',
    imports: [MatCardModule, TaxaServicoForm],
    templateUrl: './editar-taxa-servico.html',
})
export class EditarTaxaServico {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly taxaService = inject(TaxaServicoService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly taxa = this.route.snapshot.data['taxa'] as TaxaServicoModel;

    public editar(model: CadastrarTaxaServicoModel) {
        this.taxaService.editar(this.taxa.id, model).subscribe({
            next: () => this.notificacaoService.sucesso(`"${model.nome}" foi editado com sucesso!`),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível editar a taxa.')),
            complete: () => this.router.navigate(['/taxas-servicos']),
        });
    }
}
