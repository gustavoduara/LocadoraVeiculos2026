import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { CadastrarClienteModel, ClienteModel, EditarClienteModel } from '../cliente.models';
import { ClienteService } from '../cliente.service';
import { ClienteForm } from '../form/cliente-form';

@Component({
    selector: 'app-editar-cliente',
    imports: [MatCardModule, MatIconModule, ClienteForm],
    templateUrl: './editar-cliente.html',
})
export class EditarCliente {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly clienteService = inject(ClienteService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly cliente = this.route.snapshot.data['cliente'] as ClienteModel;

    protected readonly pessoasJuridicas = (this.route.snapshot.data['pessoasJuridicas'] ??
        []) as ClienteModel[];

    public editar(model: CadastrarClienteModel) {
        // O tipo não faz parte da edição: a API usa o tipo do registro.
        const { tipo: _tipo, ...dados } = model;

        this.clienteService.editar(this.cliente.id, dados as EditarClienteModel).subscribe({
            next: () => this.notificacaoService.sucesso(`O cliente "${model.nome}" foi editado com sucesso!`),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível editar o cliente.')),
            complete: () => this.router.navigate(['/clientes']),
        });
    }
}
