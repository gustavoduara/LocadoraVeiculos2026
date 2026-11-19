import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { Router } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { TaxaServicoForm } from '../form/taxa-servico-form';
import { CadastrarTaxaServicoModel } from '../taxa-servico.models';
import { TaxaServicoService } from '../taxa-servico.service';

@Component({
    selector: 'app-cadastrar-taxa-servico',
    imports: [MatCardModule, TaxaServicoForm],
    templateUrl: './cadastrar-taxa-servico.html',
})
export class CadastrarTaxaServico {
    protected readonly router = inject(Router);
    protected readonly taxaService = inject(TaxaServicoService);
    protected readonly notificacaoService = inject(NotificacaoService);

    public cadastrar(model: CadastrarTaxaServicoModel) {
        this.taxaService.cadastrar(model).subscribe({
            next: () =>
                this.notificacaoService.sucesso(`"${model.nome}" foi cadastrado com sucesso!`),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível cadastrar a taxa.')),
            complete: () => this.router.navigate(['/taxas-servicos']),
        });
    }
}
