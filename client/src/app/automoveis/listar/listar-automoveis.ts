import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { SelecionarGruposVeiculosModel } from '../../grupo-veiculos/grupo-veiculos.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { AutomovelModel, formatarPlaca, rotuloCombustivel } from '../automovel.models';
import { AutomovelService } from '../automovel.service';
import { FotoAutomovel } from '../foto/foto-automovel';

import { BuscaLista, ListaPaginada, PaginacaoLista } from '../../shared/lista/lista-paginada';

@Component({
    selector: 'app-listar-automoveis',
    imports: [
        CurrencyPipe,
        BuscaLista,
        PaginacaoLista,
        MatButtonModule,
        MatCardModule,
        MatFormFieldModule,
        MatIconModule,
        MatSelectModule,
        RouterLink,
        FotoAutomovel,
    ],
    templateUrl: './listar-automoveis.html',
})
export class ListarAutomoveis {
    protected readonly route = inject(ActivatedRoute);
    protected readonly automovelService = inject(AutomovelService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly grupos = (this.route.snapshot.data['grupos'] ??
        []) as SelecionarGruposVeiculosModel[];

    protected readonly automoveis = signal<AutomovelModel[]>(
        (this.route.snapshot.data['automoveis'] ?? []) as AutomovelModel[],
    );

    protected readonly grupoFiltro = signal<string | null>(null);

    protected readonly formatarPlaca = formatarPlaca;
    protected readonly rotuloCombustivel = rotuloCombustivel;

    /** Recarrega a frota aplicando o filtro de grupo (vazio = todos os grupos). */
    protected filtrar(grupoId: string | null): void {
        this.grupoFiltro.set(grupoId);

        this.automovelService.selecionarTodos(grupoId).subscribe({
            next: (lista) => this.automoveis.set(lista),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível filtrar os automóveis.')),
        });
    }

    /** Busca por texto e páginas (a lista toda já está carregada na tela). */
    protected readonly lista = new ListaPaginada(this.automoveis);
}
