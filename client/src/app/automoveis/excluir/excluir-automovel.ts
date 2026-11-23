import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { AutomovelModel, formatarPlaca } from '../automovel.models';
import { AutomovelService } from '../automovel.service';

@Component({
    selector: 'app-excluir-automovel',
    imports: [FormsModule, MatCardModule, MatButtonModule, MatIconModule, RouterLink],
    templateUrl: './excluir-automovel.html',
})
export class ExcluirAutomovel {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly automovelService = inject(AutomovelService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly automovel = this.route.snapshot.data['automovel'] as AutomovelModel;

    protected readonly formatarPlaca = formatarPlaca;

    public excluir() {
        this.automovelService.excluir(this.automovel.id).subscribe({
            next: () => this.notificacaoService.sucesso('O registro foi excluído com sucesso!'),
            error: (err) =>
                this.notificacaoService.erro(
                    mensagemDeErro(err, 'Não foi possível excluir o automóvel.'),
                ),
            complete: () => this.router.navigate(['/automoveis']),
        });
    }
}
