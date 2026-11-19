import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ClienteModel } from '../../clientes/cliente.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { CadastrarCondutorModel } from '../condutor.models';
import { CondutorService } from '../condutor.service';
import { CondutorForm } from '../form/condutor-form';

@Component({
    selector: 'app-cadastrar-condutor',
    imports: [MatCardModule, MatButtonModule, RouterLink, CondutorForm],
    templateUrl: './cadastrar-condutor.html',
})
export class CadastrarCondutor {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly condutorService = inject(CondutorService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly clientes = (this.route.snapshot.data['clientes'] ?? []) as ClienteModel[];

    public cadastrar(model: CadastrarCondutorModel) {
        this.condutorService.cadastrar(model).subscribe({
            next: () => this.notificacaoService.sucesso(`O condutor "${model.nome}" foi cadastrado com sucesso!`),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível cadastrar o condutor.')),
            complete: () => this.router.navigate(['/condutores']),
        });
    }
}
