import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { ActivatedRoute, Router } from '@angular/router';

import { ParceiroModel } from '../../parceiros/parceiro.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { CadastrarCupomModel, CupomModel } from '../cupom.models';
import { CupomService } from '../cupom.service';
import { CupomForm } from '../form/cupom-form';

@Component({
    selector: 'app-editar-cupom',
    imports: [MatCardModule, CupomForm],
    templateUrl: './editar-cupom.html',
})
export class EditarCupom {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly cupomService = inject(CupomService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly cupom = this.route.snapshot.data['cupom'] as CupomModel;
    protected readonly parceiros = (this.route.snapshot.data['parceiros'] ?? []) as ParceiroModel[];

    public editar(model: CadastrarCupomModel) {
        this.cupomService.editar(this.cupom.id, model).subscribe({
            next: () => this.notificacaoService.sucesso(`O cupom "${model.nome}" foi editado com sucesso!`),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível editar o cupom.')),
            complete: () => this.router.navigate(['/cupons']),
        });
    }
}
