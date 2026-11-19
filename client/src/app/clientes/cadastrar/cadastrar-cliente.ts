import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { ActivatedRoute, Router } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { CadastrarClienteModel, ClienteModel } from '../cliente.models';
import { ClienteService } from '../cliente.service';
import { ClienteForm } from '../form/cliente-form';

@Component({
    selector: 'app-cadastrar-cliente',
    imports: [MatCardModule, ClienteForm],
    templateUrl: './cadastrar-cliente.html',
})
export class CadastrarCliente {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly clienteService = inject(ClienteService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly pessoasJuridicas = (this.route.snapshot.data['pessoasJuridicas'] ??
        []) as ClienteModel[];

    public cadastrar(model: CadastrarClienteModel) {
        this.clienteService.cadastrar(model).subscribe({
            next: () => this.notificacaoService.sucesso(`O cliente "${model.nome}" foi cadastrado com sucesso!`),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível cadastrar o cliente.')),
            complete: () => this.router.navigate(['/clientes']),
        });
    }
}
