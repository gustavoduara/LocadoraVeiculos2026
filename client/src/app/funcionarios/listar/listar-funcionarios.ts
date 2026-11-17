import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { SelecionarFuncionariosModel } from '../funcionario.models';
import { FuncionarioService } from '../funcionario.service';

@Component({
    selector: 'app-listar-funcionarios',
    imports: [MatButtonModule, MatIconModule, MatCardModule, RouterLink, CurrencyPipe, DatePipe],
    templateUrl: './listar-funcionarios.html',
})
export class ListarFuncionarios {
    protected readonly route = inject(ActivatedRoute);
    protected readonly funcionarioService = inject(FuncionarioService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly funcionarios = signal<SelecionarFuncionariosModel[]>(
        (this.route.snapshot.data['funcionarios'] as SelecionarFuncionariosModel[]) ?? [],
    );

    protected alternarStatus(funcionario: SelecionarFuncionariosModel): void {
        const novoStatus = !funcionario.ativo;

        this.funcionarioService.alterarStatus(funcionario.id, novoStatus).subscribe({
            next: () => {
                this.funcionarios.update((lista) =>
                    lista.map((f) => (f.id === funcionario.id ? { ...f, ativo: novoStatus } : f)),
                );

                this.notificacaoService.sucesso(
                    `A conta de "${funcionario.nomeCompleto}" foi ${novoStatus ? 'ativada' : 'desativada'}.`,
                );
            },
            error: (err) =>
                this.notificacaoService.erro(
                    mensagemDeErro(err, 'Não foi possível alterar o status do funcionário.'),
                ),
        });
    }
}
