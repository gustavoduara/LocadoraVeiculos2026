import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { ClienteModel } from '../../clientes/cliente.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { CondutorModel, isoParaDataBr } from '../condutor.models';
import { CondutorService } from '../condutor.service';

import { BuscaLista, ListaPaginada, PaginacaoLista } from '../../shared/lista/lista-paginada';

@Component({
    selector: 'app-listar-condutores',
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
    templateUrl: './listar-condutores.html',
})
export class ListarCondutores {
    protected readonly route = inject(ActivatedRoute);
    protected readonly condutorService = inject(CondutorService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly clientes = (this.route.snapshot.data['clientes'] ?? []) as ClienteModel[];

    protected readonly condutores = signal<CondutorModel[]>(
        (this.route.snapshot.data['condutores'] ?? []) as CondutorModel[],
    );

    protected readonly clienteFiltro = signal<string | null>(null);

    protected readonly dataBr = isoParaDataBr;

    /** Recarrega a lista aplicando o filtro de cliente (vazio = todos). */
    protected filtrar(clienteId: string | null): void {
        this.clienteFiltro.set(clienteId);

        this.condutorService.selecionarTodos(clienteId).subscribe({
            next: (lista) => this.condutores.set(lista),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível filtrar os condutores.')),
        });
    }

    /** Busca por texto e páginas (a lista toda já está carregada na tela). */
    protected readonly lista = new ListaPaginada(this.condutores);
}
