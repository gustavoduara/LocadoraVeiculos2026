import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import {
    ClienteModel,
    rotuloTipoCliente,
    TIPOS_CLIENTE,
    TipoClienteModel,
} from '../cliente.models';
import { ClienteService } from '../cliente.service';

import { BuscaLista, ListaPaginada, PaginacaoLista } from '../../shared/lista/lista-paginada';

@Component({
    selector: 'app-listar-clientes',
    imports: [
        BuscaLista,
        PaginacaoLista,
        MatButtonModule,
        MatCardModule,
        MatFormFieldModule,
        MatIconModule,
        MatSelectModule,
        RouterLink,
    ],
    templateUrl: './listar-clientes.html',
})
export class ListarClientes {
    protected readonly route = inject(ActivatedRoute);
    protected readonly clienteService = inject(ClienteService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly clientes = signal<ClienteModel[]>(
        (this.route.snapshot.data['clientes'] ?? []) as ClienteModel[],
    );

    protected readonly tipoFiltro = signal<TipoClienteModel | null>(null);

    protected readonly tipos = TIPOS_CLIENTE;
    protected readonly rotuloTipoCliente = rotuloTipoCliente;

    /** Recarrega a lista aplicando o filtro de tipo (vazio = todos). */
    protected filtrar(tipo: TipoClienteModel | null): void {
        this.tipoFiltro.set(tipo);

        this.clienteService.selecionarTodos(tipo).subscribe({
            next: (lista) => this.clientes.set(lista),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível filtrar os clientes.')),
        });
    }

    /** Busca por texto e páginas (a lista toda já está carregada na tela). */
    protected readonly lista = new ListaPaginada(this.clientes);
}
