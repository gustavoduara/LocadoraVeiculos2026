import { Component, EventEmitter, inject, Input, Output } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { RouterLink } from '@angular/router';

import { CadastrarParceiroModel, ParceiroModel } from '../parceiro.models';

/** Formulário usado no cadastro e na edição de parceiros (só o nome). */
@Component({
    selector: 'app-parceiro-form',
    imports: [
        ReactiveFormsModule,
        MatButtonModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        RouterLink,
    ],
    templateUrl: './parceiro-form.html',
})
export class ParceiroForm {
    private readonly formBuilder = inject(FormBuilder);

    @Input() set parceiro(valor: ParceiroModel | undefined) {
        if (!valor) return;

        this.edicao = true;
        this.form.patchValue(valor);
    }

    @Output() salvar = new EventEmitter<CadastrarParceiroModel>();

    protected edicao = false;

    protected form: FormGroup = this.formBuilder.group({
        nome: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    });

    protected erroDo(campo: string): string | null {
        const controle = this.form.get(campo);

        if (!controle || !controle.errors || !controle.touched) return null;
        if (controle.errors['required']) return 'Campo obrigatório.';
        if (controle.errors['minlength'])
            return `Informe no mínimo ${controle.errors['minlength'].requiredLength} caracteres.`;
        if (controle.errors['maxlength'])
            return `Informe no máximo ${controle.errors['maxlength'].requiredLength} caracteres.`;

        return 'Valor inválido.';
    }

    public enviar(): void {
        this.form.markAllAsTouched();

        if (this.form.invalid) return;

        this.salvar.emit(this.form.getRawValue() as CadastrarParceiroModel);
    }
}
