import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { ClienteModel } from '../cliente.models';
import { ClienteService } from '../cliente.service';

@Component({
    selector: 'app-excluir-cliente',
    imports: [FormsModule, MatCardModule, MatButtonModule, MatIconModule, RouterLink],
    templateUrl: './excluir-cliente.html',
})
export class ExcluirCliente {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly clienteService = inject(ClienteService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly cliente = this.route.snapshot.data['cliente'] as ClienteModel;

    public excluir() {
        this.clienteService.excluir(this.cliente.id).subscribe({
            next: () => this.notificacaoService.sucesso('O registro foi excluído com sucesso!'),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível excluir o cliente.')),
            complete: () => this.router.navigate(['/clientes']),
        });
    }
}
