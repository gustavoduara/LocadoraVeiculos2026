import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { ActivatedRoute, Router } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { ParceiroForm } from '../form/parceiro-form';
import { CadastrarParceiroModel, ParceiroModel } from '../parceiro.models';
import { ParceiroService } from '../parceiro.service';

@Component({
    selector: 'app-editar-parceiro',
    imports: [MatCardModule, ParceiroForm],
    templateUrl: './editar-parceiro.html',
})
export class EditarParceiro {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly parceiroService = inject(ParceiroService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly parceiro = this.route.snapshot.data['parceiro'] as ParceiroModel;

    public editar(model: CadastrarParceiroModel) {
        this.parceiroService.editar(this.parceiro.id, model).subscribe({
            next: () => this.notificacaoService.sucesso(`O parceiro "${model.nome}" foi editado com sucesso!`),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível editar o parceiro.')),
            complete: () => this.router.navigate(['/parceiros']),
        });
    }
}
