import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ParceiroModel } from '../../parceiros/parceiro.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { CadastrarCupomModel } from '../cupom.models';
import { CupomService } from '../cupom.service';
import { CupomForm } from '../form/cupom-form';

@Component({
    selector: 'app-cadastrar-cupom',
    imports: [MatCardModule, MatButtonModule, RouterLink, CupomForm],
    templateUrl: './cadastrar-cupom.html',
})
export class CadastrarCupom {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly cupomService = inject(CupomService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly parceiros = (this.route.snapshot.data['parceiros'] ?? []) as ParceiroModel[];

    public cadastrar(model: CadastrarCupomModel) {
        this.cupomService.cadastrar(model).subscribe({
            next: () => this.notificacaoService.sucesso(`O cupom "${model.nome}" foi cadastrado com sucesso!`),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível cadastrar o cupom.')),
            complete: () => this.router.navigate(['/cupons']),
        });
    }
}
