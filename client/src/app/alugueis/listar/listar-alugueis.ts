import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { isoParaDataBr } from '../../condutores/condutor.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { PrecoComDesconto } from '../../shared/preco/preco-com-desconto';
import {
    AluguelModel,
    rotuloPlano,
    rotuloSeguro,
    rotuloStatus,
    StatusAluguelModel,
} from '../aluguel.models';
import { AluguelService } from '../aluguel.service';

import { BuscaLista, ListaPaginada, PaginacaoLista } from '../../shared/lista/lista-paginada';

@Component({
    selector: 'app-listar-alugueis',
    imports: [
        BuscaLista,
        PaginacaoLista,
        MatButtonModule,
        MatCardModule,
        MatFormFieldModule,
        MatIconModule,
        MatSelectModule,
        RouterLink,
        CurrencyPipe,
        PrecoComDesconto,
    ],
    templateUrl: './listar-alugueis.html',
})
export class ListarAlugueis {
    protected readonly route = inject(ActivatedRoute);
    protected readonly aluguelService = inject(AluguelService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly alugueis = signal<AluguelModel[]>(
        (this.route.snapshot.data['alugueis'] ?? []) as AluguelModel[],
    );

    protected readonly statusFiltro = signal<StatusAluguelModel | null>(null);

    protected readonly dataBr = isoParaDataBr;
    protected readonly rotuloPlano = rotuloPlano;
    protected readonly rotuloSeguro = rotuloSeguro;
    protected readonly rotuloStatus = rotuloStatus;

    protected filtrar(status: StatusAluguelModel | null): void {
        this.statusFiltro.set(status);

        this.aluguelService.selecionarTodos(status).subscribe({
            next: (lista) => this.alugueis.set(lista),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível filtrar os aluguéis.')),
        });
    }

    /** Busca por texto e páginas (a lista toda já está carregada na tela). */
    protected readonly lista = new ListaPaginada(this.alugueis);
}
