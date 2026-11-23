import { Component, EventEmitter, inject, Input, Output } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { RouterLink } from '@angular/router';

import {
    CadastrarPlanoCobrancaModel,
    AutomovelOpcaoModel,
    PlanoCobrancaModel,
} from '../plano-cobranca.models';

const valorMonetario = [Validators.required, Validators.min(0.01), Validators.max(99999.99)];
const kmDisponiveis = [
    Validators.required,
    Validators.min(1),
    Validators.max(100000),
    Validators.pattern(/^\d+$/),
];

/** Formulário único (com uma aba por modalidade) usado no cadastro e na edição. */
@Component({
    selector: 'app-plano-cobranca-form',
    imports: [
        ReactiveFormsModule,
        MatButtonModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatSelectModule,
        MatTabsModule,
        RouterLink,
    ],
    templateUrl: './plano-cobranca-form.html',
})
export class PlanoCobrancaForm {
    private readonly formBuilder = inject(FormBuilder);

    @Input() automoveis: AutomovelOpcaoModel[] = [];

    /** Quando informado, o formulário entra em modo de edição e o automóvel não pode ser trocado. */
    @Input() set plano(valor: PlanoCobrancaModel | undefined) {
        if (!valor) return;

        this.form.patchValue(valor);
        this.form.get('automovelId')?.disable();
        this.edicao = true;
    }

    @Output() salvar = new EventEmitter<CadastrarPlanoCobrancaModel>();

    protected edicao = false;

    protected form: FormGroup = this.formBuilder.group({
        automovelId: ['', [Validators.required]],
        diario: this.formBuilder.group({
            precoDiaria: [null, valorMonetario],
            precoPorKm: [null, valorMonetario],
        }),
        controlado: this.formBuilder.group({
            kmDisponiveis: [null, kmDisponiveis],
            precoDiaria: [null, valorMonetario],
            precoPorKmExtrapolado: [null, valorMonetario],
        }),
        livre: this.formBuilder.group({
            precoDiaria: [null, valorMonetario],
        }),
    });

    protected erroDo(caminho: string): string | null {
        const controle = this.form.get(caminho);

        if (!controle || !controle.errors || !controle.touched) return null;
        if (controle.errors['required']) return 'Campo obrigatório.';
        if (controle.errors['pattern']) return 'Informe um número inteiro.';
        if (controle.errors['min']) return 'O valor deve ser maior que zero.';
        if (controle.errors['max']) return 'O valor informado é alto demais.';

        return 'Valor inválido.';
    }

    /** Indica, na aba, se algum campo dela está inválido. */
    protected abaInvalida(nome: 'diario' | 'controlado' | 'livre'): boolean {
        const grupo = this.form.get(nome);

        return !!grupo && grupo.invalid && grupo.touched;
    }

    public enviar() {
        this.form.markAllAsTouched();

        if (this.form.invalid) return;

        this.salvar.emit(this.form.getRawValue() as CadastrarPlanoCobrancaModel);
    }
}
