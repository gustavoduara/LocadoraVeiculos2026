import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { ConfiguracaoModel } from '../configuracao.models';
import { ConfiguracaoService } from '../configuracao.service';

const validadoresPreco = [Validators.required, Validators.min(0.01), Validators.max(999.99)];

@Component({
    selector: 'app-editar-configuracao',
    imports: [
        MatCardModule,
        MatButtonModule,
        MatIconModule,
        MatFormFieldModule,
        MatInputModule,
        RouterLink,
        ReactiveFormsModule,
    ],
    templateUrl: './editar-configuracao.html',
})
export class EditarConfiguracao {
    protected readonly formBuilder = inject(FormBuilder);
    protected readonly route = inject(ActivatedRoute);
    protected readonly configuracaoService = inject(ConfiguracaoService);
    protected readonly notificacaoService = inject(NotificacaoService);

    private readonly atual = this.route.snapshot.data['configuracao'] as ConfiguracaoModel;

    // Empresa que ainda não configurou recebe zeros da API: o campo começa vazio.
    private valorInicial(preco?: number): number | null {
        return preco && preco > 0 ? preco : null;
    }

    protected configuracaoForm: FormGroup = this.formBuilder.group({
        precoGasolina: [this.valorInicial(this.atual?.precoGasolina), validadoresPreco],
        precoGas: [this.valorInicial(this.atual?.precoGas), validadoresPreco],
        precoDiesel: [this.valorInicial(this.atual?.precoDiesel), validadoresPreco],
        precoAlcool: [this.valorInicial(this.atual?.precoAlcool), validadoresPreco],
        seguroClienteDiaria: [this.valorInicial(this.atual?.seguroClienteDiaria), validadoresPreco],
        seguroTerceirosDiaria: [this.valorInicial(this.atual?.seguroTerceirosDiaria), validadoresPreco],
    });

    protected readonly combustiveis = [
        { campo: 'precoGasolina', rotulo: 'Gasolina' },
        { campo: 'precoGas', rotulo: 'Gás' },
        { campo: 'precoDiesel', rotulo: 'Diesel' },
        { campo: 'precoAlcool', rotulo: 'Álcool' },
    ];

    protected readonly seguros = [
        { campo: 'seguroClienteDiaria', rotulo: 'Seguro para o cliente (por dia)' },
        { campo: 'seguroTerceirosDiaria', rotulo: 'Seguro para terceiros (por dia)' },
    ];

    protected erroDo(campo: string): string | null {
        const erros = this.configuracaoForm.get(campo)?.errors;

        if (!erros) return null;
        if (erros['required']) return 'O preço é obrigatório.';
        if (erros['min']) return 'O preço deve ser maior que zero.';
        if (erros['max']) return 'O preço deve ser menor que 1000.';

        return 'Valor inválido.';
    }

    public salvar() {
        if (this.configuracaoForm.invalid) return;

        const model: ConfiguracaoModel = this.configuracaoForm.value;

        this.configuracaoService.editar(model).subscribe({
            next: () =>
                this.notificacaoService.sucesso('Os preços dos combustíveis foram salvos com sucesso!'),
            error: (err) =>
                this.notificacaoService.erro(
                    mensagemDeErro(err, 'Não foi possível salvar as configurações.'),
                ),
        });
    }
}
