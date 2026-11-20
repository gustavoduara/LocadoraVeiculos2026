import { NgxMaskDirective } from 'ngx-mask';

import { Component, EventEmitter, inject, Input, Output } from '@angular/core';
import {
    AbstractControl,
    FormBuilder,
    FormGroup,
    ReactiveFormsModule,
    ValidationErrors,
    Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';

import { dataBrParaIso, isoParaDataBr } from '../../condutores/condutor.models';
import { ParceiroModel } from '../../parceiros/parceiro.models';
import { CadastrarCupomModel, CupomModel } from '../cupom.models';

function dataBrValida(controle: AbstractControl): ValidationErrors | null {
    const valor = (controle.value ?? '') as string;

    if (!valor) return null;

    return dataBrParaIso(valor) ? null : { dataInvalida: true };
}

/** Formulário usado no cadastro e na edição de cupons. */
@Component({
    selector: 'app-cupom-form',
    imports: [
        ReactiveFormsModule,
        MatButtonModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatSelectModule,
        NgxMaskDirective,
        RouterLink,
    ],
    templateUrl: './cupom-form.html',
})
export class CupomForm {
    private readonly formBuilder = inject(FormBuilder);

    @Input() parceiros: ParceiroModel[] = [];

    @Input() set cupom(valor: CupomModel | undefined) {
        if (!valor) return;

        this.edicao = true;
        this.form.patchValue({ ...valor, dataValidade: isoParaDataBr(valor.dataValidade) });
    }

    @Output() salvar = new EventEmitter<CadastrarCupomModel>();

    protected edicao = false;

    protected form: FormGroup = this.formBuilder.group({
        nome: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
        valor: [null, [Validators.required, Validators.min(0.01), Validators.max(99999.99)]],
        dataValidade: ['', [Validators.required, dataBrValida]],
        parceiroId: ['', [Validators.required]],
    });

    protected erroDo(campo: string): string | null {
        const controle = this.form.get(campo);

        if (!controle || !controle.errors || !controle.touched) return null;
        if (controle.errors['required']) return 'Campo obrigatório.';
        if (controle.errors['dataInvalida']) return 'Informe uma data válida (dd/mm/aaaa).';
        if (controle.errors['minlength'])
            return `Informe no mínimo ${controle.errors['minlength'].requiredLength} caracteres.`;
        if (controle.errors['maxlength'])
            return `Informe no máximo ${controle.errors['maxlength'].requiredLength} caracteres.`;
        if (controle.errors['min']) return 'O valor deve ser maior que zero.';
        if (controle.errors['max']) return 'O valor deve ser menor que 100000.';

        return 'Valor inválido.';
    }

    public enviar(): void {
        this.form.markAllAsTouched();

        if (this.form.invalid) return;

        const v = this.form.getRawValue();

        this.salvar.emit({
            nome: v.nome,
            valor: Number(v.valor),
            dataValidade: dataBrParaIso(v.dataValidade)!,
            parceiroId: v.parceiroId,
        });
    }
}
