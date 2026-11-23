import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { CondutorModel } from '../condutor.models';
import { CondutorService } from '../condutor.service';

@Component({
    selector: 'app-excluir-condutor',
    imports: [FormsModule, MatCardModule, MatButtonModule, MatIconModule, RouterLink],
    templateUrl: './excluir-condutor.html',
})
export class ExcluirCondutor {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly condutorService = inject(CondutorService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly condutor = this.route.snapshot.data['condutor'] as CondutorModel;

    public excluir() {
        this.condutorService.excluir(this.condutor.id).subscribe({
            next: () => this.notificacaoService.sucesso('O registro foi excluído com sucesso!'),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível excluir o condutor.')),
            complete: () => this.router.navigate(['/condutores']),
        });
    }
}
