import { NgxMaskDirective } from 'ngx-mask';

import { Component, DestroyRef, EventEmitter, inject, Input, Output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';

import {
    CadastrarClienteModel,
    ClienteModel,
    ESTADOS,
    TIPOS_CLIENTE,
    TipoClienteModel,
} from '../cliente.models';

/** Formulário usado no cadastro e na edição de clientes (pessoa física ou jurídica). */
@Component({
    selector: 'app-cliente-form',
    imports: [
        ReactiveFormsModule,
        MatButtonModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatRadioModule,
        MatSelectModule,
        NgxMaskDirective,
        RouterLink,
    ],
    templateUrl: './cliente-form.html',
})
export class ClienteForm {
    private readonly formBuilder = inject(FormBuilder);
    private readonly destroyRef = inject(DestroyRef);

    /** Pessoas jurídicas que podem ser vinculadas a uma pessoa física. */
    @Input() pessoasJuridicas: ClienteModel[] = [];

    /** Quando informado, o formulário entra em modo de edição (o tipo não pode mudar). */
    @Input() set cliente(valor: ClienteModel | undefined) {
        if (!valor) return;

        this.edicao = true;
        this.idEmEdicao = valor.id;

        this.form.patchValue(valor);
        this.form.get('tipo')?.disable();
        this.aplicarTipo(valor.tipo);
    }

    @Output() salvar = new EventEmitter<CadastrarClienteModel>();

    protected edicao = false;
    protected idEmEdicao: string | null = null;
    protected readonly tipos = TIPOS_CLIENTE;
    protected readonly estados = ESTADOS;

    protected form: FormGroup = this.formBuilder.group({
        tipo: ['PessoaFisica' as TipoClienteModel, [Validators.required]],
        nome: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
        email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]],
        telefone: ['', [Validators.required, Validators.pattern(/^\(\d{2}\) \d{4,5}-\d{4}$/)]],
        cpf: ['', [Validators.required, Validators.pattern(/^\d{3}\.\d{3}\.\d{3}-\d{2}$/)]],
        rg: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(20)]],
        cnh: ['', [Validators.required, Validators.pattern(/^\d{11}$/)]],
        pessoaJuridicaId: [null as string | null],
        cnpj: ['', [Validators.required, Validators.pattern(/^\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}$/)]],
        estado: ['', [Validators.required]],
        cidade: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
        bairro: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
        rua: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(150)]],
        numero: ['', [Validators.required, Validators.maxLength(10)]],
    });

    constructor() {
        this.aplicarTipo('PessoaFisica');

        this.form
            .get('tipo')!
            .valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((tipo) => this.aplicarTipo(tipo));
    }

    protected get tipoAtual(): TipoClienteModel {
        return this.form.getRawValue().tipo;
    }

    protected get ehPessoaFisica(): boolean {
        return this.tipoAtual === 'PessoaFisica';
    }

    /** Pessoas jurídicas elegíveis ao vínculo (nunca o próprio registro). */
    protected get opcoesDeVinculo(): ClienteModel[] {
        return this.pessoasJuridicas.filter((p) => p.id !== this.idEmEdicao);
    }

    /** Ativa só os campos de documento que pertencem ao tipo escolhido. */
    private aplicarTipo(tipo: TipoClienteModel): void {
        const camposFisica = ['cpf', 'rg', 'cnh', 'pessoaJuridicaId'];
        const camposJuridica = ['cnpj'];

        const [ativar, desativar] =
            tipo === 'PessoaFisica' ? [camposFisica, camposJuridica] : [camposJuridica, camposFisica];

        ativar.forEach((campo) => this.form.get(campo)?.enable({ emitEvent: false }));
        desativar.forEach((campo) => this.form.get(campo)?.disable({ emitEvent: false }));
    }

    protected erroDo(campo: string): string | null {
        const controle = this.form.get(campo);

        if (!controle || !controle.errors || !controle.touched) return null;
        if (controle.errors['required']) return 'Campo obrigatório.';
        if (controle.errors['email']) return 'Informe um e-mail válido.';
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
        const fisica = v.tipo === 'PessoaFisica';

        this.salvar.emit({
            tipo: v.tipo,
            nome: v.nome,
            email: v.email,
            telefone: v.telefone,
            cpf: fisica ? v.cpf : null,
            rg: fisica ? v.rg : null,
            cnh: fisica ? v.cnh : null,
            cnpj: fisica ? null : v.cnpj,
            estado: v.estado,
            cidade: v.cidade,
            bairro: v.bairro,
            rua: v.rua,
            numero: v.numero,
            pessoaJuridicaId: fisica ? v.pessoaJuridicaId || null : null,
        });
    }
}
