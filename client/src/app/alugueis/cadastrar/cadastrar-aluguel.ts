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
import { DadosAluguelModel } from '../aluguel.models';
import { AluguelService } from '../aluguel.service';
import { AluguelForm } from '../form/aluguel-form';

@Component({
    selector: 'app-cadastrar-aluguel',
    imports: [MatCardModule, AluguelForm],
    templateUrl: './cadastrar-aluguel.html',
})
export class CadastrarAluguel {
    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly aluguelService = inject(AluguelService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly clientes = (this.route.snapshot.data['clientes'] ?? []) as ClienteModel[];
    protected readonly condutores = (this.route.snapshot.data['condutores'] ?? []) as CondutorModel[];
    protected readonly grupos = (this.route.snapshot.data['grupos'] ??
        []) as SelecionarGruposVeiculosModel[];
    protected readonly automoveis = (this.route.snapshot.data['automoveis'] ?? []) as AutomovelModel[];
    protected readonly taxas = (this.route.snapshot.data['taxas'] ?? []) as TaxaServicoModel[];
    protected readonly cupons = (this.route.snapshot.data['cupons'] ?? []) as CupomModel[];

    public cadastrar(model: DadosAluguelModel) {
        this.aluguelService.cadastrar(model).subscribe({
            next: () => this.notificacaoService.sucesso('O aluguel foi registrado com sucesso!'),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível registrar o aluguel.')),
            complete: () => this.router.navigate(['/alugueis']),
        });
    }
}
