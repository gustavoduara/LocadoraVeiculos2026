import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { Router } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { ParceiroForm } from '../form/parceiro-form';
import { CadastrarParceiroModel } from '../parceiro.models';
import { ParceiroService } from '../parceiro.service';

@Component({
    selector: 'app-cadastrar-parceiro',
    imports: [MatCardModule, ParceiroForm],
    templateUrl: './cadastrar-parceiro.html',
})
export class CadastrarParceiro {
    protected readonly router = inject(Router);
    protected readonly parceiroService = inject(ParceiroService);
    protected readonly notificacaoService = inject(NotificacaoService);

    public cadastrar(model: CadastrarParceiroModel) {
        this.parceiroService.cadastrar(model).subscribe({
            next: () => this.notificacaoService.sucesso(`O parceiro "${model.nome}" foi cadastrado com sucesso!`),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível cadastrar o parceiro.')),
            complete: () => this.router.navigate(['/parceiros']),
        });
    }
}
