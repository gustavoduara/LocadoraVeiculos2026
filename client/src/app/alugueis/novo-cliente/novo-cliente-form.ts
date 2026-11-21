import { NgxMaskDirective } from 'ngx-mask';

import { Component, DestroyRef, EventEmitter, inject, Output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
    AbstractControl,
    FormBuilder,
    FormGroup,
    ReactiveFormsModule,
    ValidationErrors,
    Validators,
} from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';

import { ESTADOS, TIPOS_CLIENTE, TipoClienteModel } from '../../clientes/cliente.models';
import { dataBrParaIso } from '../../condutores/condutor.models';
import { NovoClienteAluguelModel } from '../aluguel.models';

function dataBrValida(controle: AbstractControl): ValidationErrors | null {
    const valor = (controle.value ?? '') as string;

    if (!valor) return null;

    return dataBrParaIso(valor) ? null : { dataInvalida: true };
}

export interface NovoClienteAlterado {
    dados: NovoClienteAluguelModel;
    valido: boolean;
}

const TELEFONE = /^\(\d{2}\) \d{4,5}-\d{4}$/;
const CPF = /^\d{3}\.\d{3}\.\d{3}-\d{2}$/;

/**
 * Dados do cliente que ainda não existe na locadora: ele é cadastrado junto com o aluguel.
 * Pessoa física é o próprio condutor; pessoa jurídica informa o condutor.
 */
@Component({
    selector: 'app-novo-cliente-form',
    imports: [
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatRadioModule,
        MatSelectModule,
        NgxMaskDirective,
    ],
    template: `
<div [formGroup]="form" class="space-y-4 rounded-2xl border border-muted/30 p-4">
    <div>
        <h3 class="text-base font-medium">Dados do cliente</h3>
        <p class="text-xs text-gray-500">
            O cliente é cadastrado junto com o aluguel e fica só nesta locadora.
        </p>
    </div>

    <mat-radio-group formControlName="tipo" class="flex gap-6">
        @for (t of tipos; track t.valor) {
        <mat-radio-button [value]="t.valor">{{ t.rotulo }}</mat-radio-button>
        }
    </mat-radio-group>

    <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <mat-form-field class="sm:col-span-2">
            <mat-label>{{ fisica ? 'Nome completo' : 'Razão social' }}</mat-label>
            <input matInput formControlName="nome" />
            @if (erroDo('nome'); as erro) { <mat-error>{{ erro }}</mat-error> }
        </mat-form-field>

        <mat-form-field>
            <mat-label>E-mail</mat-label>
            <input matInput formControlName="email" type="email" />
            @if (erroDo('email'); as erro) { <mat-error>{{ erro }}</mat-error> }
        </mat-form-field>

        <mat-form-field>
            <mat-label>Telefone</mat-label>
            <input matInput formControlName="telefone" mask="(00) 0000-0000||(00) 00000-0000"
                [dropSpecialCharacters]="false" placeholder="(00) 00000-0000" />
            @if (erroDo('telefone'); as erro) { <mat-error>{{ erro }}</mat-error> }
        </mat-form-field>

        @if (fisica) {
        <mat-form-field>
            <mat-label>CPF</mat-label>
            <input matInput formControlName="cpf" mask="000.000.000-00" [dropSpecialCharacters]="false" />
            @if (erroDo('cpf'); as erro) { <mat-error>{{ erro }}</mat-error> }
        </mat-form-field>

        <mat-form-field>
            <mat-label>RG</mat-label>
            <input matInput formControlName="rg" maxlength="20" />
            @if (erroDo('rg'); as erro) { <mat-error>{{ erro }}</mat-error> }
        </mat-form-field>

        <mat-form-field>
            <mat-label>CNH</mat-label>
            <input matInput formControlName="cnh" mask="00000000000" placeholder="11 dígitos" />
            @if (erroDo('cnh'); as erro) { <mat-error>{{ erro }}</mat-error> }
        </mat-form-field>

        <mat-form-field>
            <mat-label>Validade da CNH</mat-label>
            <input matInput formControlName="validadeCnh" mask="00/00/0000" [dropSpecialCharacters]="false"
                placeholder="dd/mm/aaaa" />
            @if (erroDo('validadeCnh'); as erro) { <mat-error>{{ erro }}</mat-error> }
        </mat-form-field>
        } @else {
        <mat-form-field class="sm:col-span-2">
            <mat-label>CNPJ</mat-label>
            <input matInput formControlName="cnpj" mask="00.000.000/0000-00" [dropSpecialCharacters]="false" />
            @if (erroDo('cnpj'); as erro) { <mat-error>{{ erro }}</mat-error> }
        </mat-form-field>
        }

        <mat-form-field>
            <mat-label>Estado</mat-label>
            <mat-select formControlName="estado">
                @for (uf of estados; track uf) {
                <mat-option [value]="uf">{{ uf }}</mat-option>
                }
            </mat-select>
            @if (erroDo('estado')) { <mat-error>Selecione o estado.</mat-error> }
        </mat-form-field>

        <mat-form-field>
            <mat-label>Cidade</mat-label>
            <input matInput formControlName="cidade" />
            @if (erroDo('cidade'); as erro) { <mat-error>{{ erro }}</mat-error> }
        </mat-form-field>

        <mat-form-field>
            <mat-label>Bairro</mat-label>
            <input matInput formControlName="bairro" />
            @if (erroDo('bairro'); as erro) { <mat-error>{{ erro }}</mat-error> }
        </mat-form-field>

        <mat-form-field>
            <mat-label>Rua</mat-label>
            <input matInput formControlName="rua" />
            @if (erroDo('rua'); as erro) { <mat-error>{{ erro }}</mat-error> }
        </mat-form-field>

        <mat-form-field>
            <mat-label>Número</mat-label>
            <input matInput formControlName="numero" maxlength="10" />
            @if (erroDo('numero'); as erro) { <mat-error>{{ erro }}</mat-error> }
        </mat-form-field>
    </div>

    @if (!fisica) {
    <div formGroupName="condutor" class="space-y-2">
        <h4 class="text-sm font-medium">Condutor (quem vai dirigir)</h4>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <mat-form-field class="sm:col-span-2">
                <mat-label>Nome do condutor</mat-label>
                <input matInput formControlName="nome" />
                @if (erroDoCondutor('nome'); as erro) { <mat-error>{{ erro }}</mat-error> }
            </mat-form-field>

            <mat-form-field>
                <mat-label>E-mail</mat-label>
                <input matInput formControlName="email" type="email" />
                @if (erroDoCondutor('email'); as erro) { <mat-error>{{ erro }}</mat-error> }
            </mat-form-field>

            <mat-form-field>
                <mat-label>Telefone</mat-label>
                <input matInput formControlName="telefone" mask="(00) 0000-0000||(00) 00000-0000"
                    [dropSpecialCharacters]="false" placeholder="(00) 00000-0000" />
                @if (erroDoCondutor('telefone'); as erro) { <mat-error>{{ erro }}</mat-error> }
            </mat-form-field>

            <mat-form-field>
                <mat-label>CPF</mat-label>
                <input matInput formControlName="cpf" mask="000.000.000-00" [dropSpecialCharacters]="false" />
                @if (erroDoCondutor('cpf'); as erro) { <mat-error>{{ erro }}</mat-error> }
            </mat-form-field>

            <mat-form-field>
                <mat-label>CNH</mat-label>
                <input matInput formControlName="cnh" mask="00000000000" placeholder="11 dígitos" />
                @if (erroDoCondutor('cnh'); as erro) { <mat-error>{{ erro }}</mat-error> }
            </mat-form-field>

            <mat-form-field>
                <mat-label>Validade da CNH</mat-label>
                <input matInput formControlName="validadeCnh" mask="00/00/0000" [dropSpecialCharacters]="false"
                    placeholder="dd/mm/aaaa" />
                @if (erroDoCondutor('validadeCnh'); as erro) { <mat-error>{{ erro }}</mat-error> }
            </mat-form-field>
        </div>
    </div>
    }
</div>
`,
})
export class NovoClienteForm {
    private readonly formBuilder = inject(FormBuilder);
    private readonly destroyRef = inject(DestroyRef);

    /** Sempre que algo muda: os dados (mesmo incompletos, para a simulação de preço) e se já estão válidos. */
    @Output() alterado = new EventEmitter<NovoClienteAlterado>();

    protected readonly tipos = TIPOS_CLIENTE;
    protected readonly estados = ESTADOS;

    protected readonly form: FormGroup = this.formBuilder.group({
        tipo: ['PessoaFisica' as TipoClienteModel, [Validators.required]],
        nome: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
        email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]],
        telefone: ['', [Validators.required, Validators.pattern(TELEFONE)]],
        cpf: ['', [Validators.required, Validators.pattern(CPF)]],
        rg: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(20)]],
        cnh: ['', [Validators.required, Validators.pattern(/^\d{11}$/)]],
        validadeCnh: ['', [Validators.required, dataBrValida]],
        cnpj: ['', [Validators.required, Validators.pattern(/^\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}$/)]],
        estado: ['', [Validators.required]],
        cidade: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
        bairro: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
        rua: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(150)]],
        numero: ['', [Validators.required, Validators.maxLength(10)]],
        condutor: this.formBuilder.group({
            nome: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
            email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]],
            telefone: ['', [Validators.required, Validators.pattern(TELEFONE)]],
            cpf: ['', [Validators.required, Validators.pattern(CPF)]],
            cnh: ['', [Validators.required, Validators.pattern(/^\d{11}$/)]],
            validadeCnh: ['', [Validators.required, dataBrValida]],
        }),
    });

    constructor() {
        this.aplicarTipo('PessoaFisica');

        this.form
            .get('tipo')!
            .valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((tipo) => {
                this.aplicarTipo(tipo);
                this.emitir();
            });

        this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.emitir());

        // Avisa já na criação (o formulário do aluguel precisa de algo para simular o preço).
        queueMicrotask(() => this.emitir());
    }

    protected get fisica(): boolean {
        return this.form.getRawValue().tipo === 'PessoaFisica';
    }

    /** Ativa só os campos que pertencem ao tipo escolhido. */
    private aplicarTipo(tipo: TipoClienteModel): void {
        const fisica = tipo === 'PessoaFisica';

        for (const campo of ['cpf', 'rg', 'cnh', 'validadeCnh'])
            fisica ? this.form.get(campo)!.enable({ emitEvent: false }) : this.form.get(campo)!.disable({ emitEvent: false });

        fisica ? this.form.get('cnpj')!.disable({ emitEvent: false }) : this.form.get('cnpj')!.enable({ emitEvent: false });
        fisica ? this.form.get('condutor')!.disable({ emitEvent: false }) : this.form.get('condutor')!.enable({ emitEvent: false });
    }

    protected erroDo(campo: string): string | null {
        return this.mensagem(this.form.get(campo));
    }

    protected erroDoCondutor(campo: string): string | null {
        return this.mensagem(this.form.get('condutor')?.get(campo));
    }

    private mensagem(controle: AbstractControl | null | undefined): string | null {
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

    /** Mostra os erros de todos os campos (o formulário do aluguel chama ao tentar enviar). */
    public marcarComoTocado(): void {
        this.form.markAllAsTouched();
    }

    private emitir(): void {
        const v = this.form.getRawValue();
        const fisica = v.tipo === 'PessoaFisica';

        const dados: NovoClienteAluguelModel = {
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
            validadeCnh: dataBrParaIso(fisica ? v.validadeCnh : v.condutor.validadeCnh) ?? '',
            condutor: fisica
                ? null
                : {
                      nome: v.condutor.nome,
                      email: v.condutor.email,
                      telefone: v.condutor.telefone,
                      cpf: v.condutor.cpf,
                      cnh: v.condutor.cnh,
                      validadeCnh: dataBrParaIso(v.condutor.validadeCnh) ?? '',
                  },
        };

        this.alterado.emit({ dados, valido: this.form.valid });
    }
}
