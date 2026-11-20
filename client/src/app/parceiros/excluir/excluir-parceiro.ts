import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { ParceiroModel } from '../parceiro.models';
import { ParceiroService } from '../parceiro.service';

@Component({
    selector: 'app-excluir-parceiro',
    imports: [MatCardModule, MatButtonModule, MatIconModule, RouterLink],
    templateUrl: './excluir-parceiro.html',
})
export class ExcluirParceiro {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly parceiroService = inject(ParceiroService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly parceiro = this.route.snapshot.data['parceiro'] as ParceiroModel;

    public excluir() {
        this.parceiroService.excluir(this.parceiro.id).subscribe({
            next: () => this.notificacaoService.sucesso('O registro foi excluído com sucesso!'),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível excluir o parceiro.')),
            complete: () => this.router.navigate(['/parceiros']),
        });
    }
}
