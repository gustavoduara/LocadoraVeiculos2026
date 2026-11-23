import { NgxMaskDirective } from 'ngx-mask';

import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { TemaService } from '../../shared/tema/tema.service';
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
        MatSlideToggleModule,
        RouterLink,
        ReactiveFormsModule,
        NgxMaskDirective,
    ],
    templateUrl: './editar-configuracao.html',
})
export class EditarConfiguracao {
    protected readonly tema = inject(TemaService);
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
    });

    protected readonly combustiveis: { campo: string; rotulo: string; dica?: string }[] = [
        { campo: 'precoGasolina', rotulo: 'Gasolina' },
        {
            campo: 'precoGas',
            rotulo: 'GNV (gás natural)',
            dica: 'Só para carros convertidos para gás natural veicular. Se nenhum carro seu usa, pode deixar o valor que quiser.',
        },
        { campo: 'precoDiesel', rotulo: 'Diesel' },
        { campo: 'precoAlcool', rotulo: 'Álcool' },
    ];

    // Nome e telefone da locadora: o que o cliente vê no catálogo e depois que a reserva é aprovada.
    protected locadoraForm: FormGroup = this.formBuilder.group({
        nomeFantasia: ['', [Validators.maxLength(150)]],
        telefone: ['', [Validators.pattern(/^\(\d{2}\) \d{4,5}-\d{4}$/)]],
    });

    constructor() {
        this.configuracaoService.obterDadosDaLocadora().subscribe({
            next: (dados) =>
                this.locadoraForm.patchValue({
                    nomeFantasia: dados.nomeFantasia ?? '',
                    telefone: dados.telefone ?? '',
                }),
        });
    }

    public salvarLocadora() {
        if (this.locadoraForm.invalid) return;

        const v = this.locadoraForm.value;

        this.configuracaoService
            .salvarDadosDaLocadora({
                nomeFantasia: v.nomeFantasia?.trim() || null,
                telefone: v.telefone || null,
            })
            .subscribe({
                next: () => this.notificacaoService.sucesso('Os dados da locadora foram salvos!'),
                error: (err) =>
                    this.notificacaoService.erro(
                        mensagemDeErro(err, 'Não foi possível salvar os dados da locadora.'),
                    ),
            });
    }

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
