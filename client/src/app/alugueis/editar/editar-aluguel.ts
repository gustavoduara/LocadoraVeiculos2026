import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { ActivatedRoute, Router } from '@angular/router';

import { AutomovelModel } from '../../automoveis/automovel.models';
import { ClienteModel } from '../../clientes/cliente.models';
import { CondutorModel } from '../../condutores/condutor.models';
import { CupomModel } from '../../cupons/cupom.models';
import { SelecionarGruposVeiculosModel } from '../../grupo-veiculos/grupo-veiculos.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { TaxaServicoModel } from '../../taxas-servicos/taxa-servico.models';
import { AluguelModel, DadosAluguelModel } from '../aluguel.models';
import { AluguelService } from '../aluguel.service';
import { AluguelForm } from '../form/aluguel-form';

@Component({
    selector: 'app-editar-aluguel',
    imports: [MatCardModule, AluguelForm],
    templateUrl: './editar-aluguel.html',
})
export class EditarAluguel {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly aluguelService = inject(AluguelService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly aluguel = this.route.snapshot.data['aluguel'] as AluguelModel;
    protected readonly clientes = (this.route.snapshot.data['clientes'] ?? []) as ClienteModel[];
    protected readonly condutores = (this.route.snapshot.data['condutores'] ?? []) as CondutorModel[];
    protected readonly grupos = (this.route.snapshot.data['grupos'] ??
        []) as SelecionarGruposVeiculosModel[];
    protected readonly automoveis = (this.route.snapshot.data['automoveis'] ?? []) as AutomovelModel[];
    protected readonly taxas = (this.route.snapshot.data['taxas'] ?? []) as TaxaServicoModel[];
    protected readonly cupons = (this.route.snapshot.data['cupons'] ?? []) as CupomModel[];

    public editar(model: DadosAluguelModel) {
        this.aluguelService.editar(this.aluguel.id, model).subscribe({
            next: () => this.notificacaoService.sucesso('O aluguel foi editado com sucesso!'),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível editar o aluguel.')),
            complete: () => this.router.navigate(['/alugueis']),
        });
    }
}
