import { NgxMaskDirective } from 'ngx-mask';

import { Component, DestroyRef, EventEmitter, inject, Input, Output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
    AbstractControl,
    FormBuilder,
    FormGroup,
    ReactiveFormsModule,
    ValidationErrors,
    Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';

import {
    CadastrarCondutorModel,
    ClienteOpcaoModel,
    CondutorModel,
    dataBrParaIso,
    isoParaDataBr,
} from '../condutor.models';

/** Campos que ficam travados quando "o cliente é o condutor" (vêm do cadastro do cliente). */
const CAMPOS_DO_CLIENTE = ['nome', 'email', 'telefone', 'cpf', 'cnh'];

function dataBrValida(controle: AbstractControl): ValidationErrors | null {
    const valor = (controle.value ?? '') as string;

    if (!valor) return null; // o "obrigatório" é tratado pelo Validators.required

    return dataBrParaIso(valor) ? null : { dataInvalida: true };
}

/** Formulário usado no cadastro e na edição de condutores. */
@Component({
    selector: 'app-condutor-form',
    imports: [
        ReactiveFormsModule,
        MatButtonModule,
        MatCheckboxModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatSelectModule,
        NgxMaskDirective,
        RouterLink,
    ],
    templateUrl: './condutor-form.html',
})
export class CondutorForm {
    private readonly formBuilder = inject(FormBuilder);
    private readonly destroyRef = inject(DestroyRef);

    @Input() clientes: ClienteOpcaoModel[] = [];

    /** Quando informado, o formulário entra em modo de edição (o cliente não pode mudar). */
    @Input() set condutor(valor: CondutorModel | undefined) {
        if (!valor) return;

        this.edicao = true;

        this.form.patchValue({ ...valor, validadeCnh: isoParaDataBr(valor.validadeCnh) });
        this.form.get('clienteId')?.disable();
    }

    @Output() salvar = new EventEmitter<CadastrarCondutorModel>();

    protected edicao = false;

    protected form: FormGroup = this.formBuilder.group({
        clienteId: ['', [Validators.required]],
        clienteECondutor: [false],
        nome: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
        email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]],
        telefone: ['', [Validators.required, Validators.pattern(/^\(\d{2}\) \d{4,5}-\d{4}$/)]],
        cpf: ['', [Validators.required, Validators.pattern(/^\d{3}\.\d{3}\.\d{3}-\d{2}$/)]],
        cnh: ['', [Validators.required, Validators.pattern(/^\d{11}$/)]],
        validadeCnh: ['', [Validators.required, dataBrValida]],
    });

    constructor() {
        // Trocar o cliente com "cliente é condutor" marcado: refaz o preenchimento (ou desmarca, se for PJ).
        this.form
            .get('clienteId')!
            .valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => this.sincronizarClienteComoCondutor());

        this.form
            .get('clienteECondutor')!
            .valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => this.sincronizarClienteComoCondutor());
    }

    /** O cliente selecionado (no cadastro) — usado para decidir se a opção "cliente é condutor" aparece. */
    protected get clienteSelecionado(): ClienteOpcaoModel | undefined {
        const id = this.form.getRawValue().clienteId;

        return this.clientes.find((c) => c.id === id);
    }

    /** Só uma pessoa física pode ser o próprio condutor. */
    protected get podeSerOProprioCondutor(): boolean {
        return !this.edicao && this.clienteSelecionado?.tipo === 'PessoaFisica';
    }

    private sincronizarClienteComoCondutor(): void {
        const marcado = !!this.form.get('clienteECondutor')!.value;
        const cliente = this.clienteSelecionado;

        if (marcado && cliente?.tipo === 'PessoaFisica') {
            this.form.patchValue(
                {
                    nome: cliente.nome ?? '',
                    email: cliente.email ?? '',
                    telefone: cliente.telefone ?? '',
                    cpf: cliente.cpf ?? '',
                    cnh: cliente.cnh ?? '',
                },
                { emitEvent: false },
            );

            CAMPOS_DO_CLIENTE.forEach((c) => this.form.get(c)?.disable({ emitEvent: false }));
            return;
        }

        if (marcado) {
            // Trocou para uma pessoa jurídica: ela não pode ser condutora.
            this.form.get('clienteECondutor')!.setValue(false, { emitEvent: false });
        }

        CAMPOS_DO_CLIENTE.forEach((c) => this.form.get(c)?.enable({ emitEvent: false }));
    }

    protected erroDo(campo: string): string | null {
        const controle = this.form.get(campo);

        if (!controle || !controle.errors || !controle.touched) return null;
        if (controle.errors['required']) return 'Campo obrigatório.';
        if (controle.errors['email']) return 'Informe um e-mail válido.';
        if (controle.errors['dataInvalida']) return 'Informe uma data válida (dd/mm/aaaa).';
        if (controle.errors['pattern']) return 'Formato inválido.';
        if (controle.errors['minlength'])
            return `Informe no mínimo ${controle.errors['minlength'].requiredLength} caracteres.`;
        if (controle.errors['maxlength'])
            return `Informe no máximo ${controle.errors['maxlength'].requiredLength} caracteres.`;

        return 'Valor inválido.';
    }

    public enviar(): void {
        this.form.markAllAsTouched();

        if (this.form.invalid) return;

        const v = this.form.getRawValue();

        this.salvar.emit({
            clienteId: v.clienteId,
            nome: v.nome,
            email: v.email,
            telefone: v.telefone,
            cpf: v.cpf,
            cnh: v.cnh,
            validadeCnh: dataBrParaIso(v.validadeCnh)!,
        });
    }
}
