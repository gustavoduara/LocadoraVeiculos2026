import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { ActivatedRoute, Router } from '@angular/router';

import { ClienteModel } from '../../clientes/cliente.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { CadastrarCondutorModel, CondutorModel, EditarCondutorModel } from '../condutor.models';
import { CondutorService } from '../condutor.service';
import { CondutorForm } from '../form/condutor-form';

@Component({
    selector: 'app-editar-condutor',
    imports: [MatCardModule, CondutorForm],
    templateUrl: './editar-condutor.html',
})
export class EditarCondutor {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly condutorService = inject(CondutorService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly condutor = this.route.snapshot.data['condutor'] as CondutorModel;
    protected readonly clientes = (this.route.snapshot.data['clientes'] ?? []) as ClienteModel[];

    public editar(model: CadastrarCondutorModel) {
        // O cliente do condutor não faz parte da edição.
        const { clienteId: _clienteId, ...dados } = model;

        this.condutorService.editar(this.condutor.id, dados as EditarCondutorModel).subscribe({
            next: () => this.notificacaoService.sucesso(`O condutor "${model.nome}" foi editado com sucesso!`),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível editar o condutor.')),
            complete: () => this.router.navigate(['/condutores']),
        });
    }
}
