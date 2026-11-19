import { Component, EventEmitter, inject, Input, Output } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { RouterLink } from '@angular/router';

import {
    CadastrarTaxaServicoModel,
    TaxaServicoModel,
    TIPOS_COBRANCA,
} from '../taxa-servico.models';

/** Formulário usado no cadastro e na edição de taxas e serviços. */
@Component({
    selector: 'app-taxa-servico-form',
    imports: [
        ReactiveFormsModule,
        MatButtonModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatRadioModule,
        RouterLink,
    ],
    templateUrl: './taxa-servico-form.html',
})
export class TaxaServicoForm {
    private readonly formBuilder = inject(FormBuilder);

    /** Quando informado, o formulário entra em modo de edição. */
    @Input() set taxa(valor: TaxaServicoModel | undefined) {
        if (!valor) return;

        this.edicao = true;
        this.form.patchValue(valor);
    }

    @Output() salvar = new EventEmitter<CadastrarTaxaServicoModel>();

    protected edicao = false;
    protected readonly tipos = TIPOS_COBRANCA;

    protected form: FormGroup = this.formBuilder.group({
        nome: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
        preco: [null, [Validators.required, Validators.min(0.01), Validators.max(99999.99)]],
        tipoCobranca: ['Fixo', [Validators.required]],
    });

    protected erroDo(campo: string): string | null {
        const controle = this.form.get(campo);

        if (!controle || !controle.errors || !controle.touched) return null;
        if (controle.errors['required']) return 'Campo obrigatório.';
        if (controle.errors['minlength'])
            return `Informe no mínimo ${controle.errors['minlength'].requiredLength} caracteres.`;
        if (controle.errors['maxlength'])
            return `Informe no máximo ${controle.errors['maxlength'].requiredLength} caracteres.`;
        if (controle.errors['min']) return 'O preço deve ser maior que zero.';
        if (controle.errors['max']) return 'O preço deve ser menor que 100000.';

        return 'Valor inválido.';
    }

    public enviar(): void {
        this.form.markAllAsTouched();

        if (this.form.invalid) return;

        this.salvar.emit(this.form.getRawValue() as CadastrarTaxaServicoModel);
    }
}
