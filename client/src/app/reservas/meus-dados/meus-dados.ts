import { NgxMaskDirective } from 'ngx-mask';

import { Component, inject, OnInit, signal } from '@angular/core';
import {
    AbstractControl,
    FormBuilder,
    ReactiveFormsModule,
    ValidationErrors,
    Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Router } from '@angular/router';

import { ESTADOS } from '../../clientes/cliente.models';
import { dataBrParaIso, isoParaDataBr } from '../../condutores/condutor.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { AvisoDemonstracao } from '../../shared/demonstracao/aviso-demonstracao';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { ReservaService } from '../reserva.service';

function dataBrValida(controle: AbstractControl): ValidationErrors | null {
    const valor = (controle.value ?? '') as string;

    if (!valor) return null;

    return dataBrParaIso(valor) ? null : { dataInvalida: true };
}

/** O cliente guarda aqui os dados que a locadora precisa para liberar o carro (documentos, telefone e endereço). */
@Component({
    selector: 'app-meus-dados',
    imports: [
        AvisoDemonstracao,
        ReactiveFormsModule,
        MatButtonModule,
        MatCardModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatSelectModule,
        NgxMaskDirective,
    ],
    template: `
<section class="mx-auto max-w-3xl px-4 py-6">
    <h2 class="text-2xl font-semibold">Meus dados</h2>
    <app-aviso-demonstracao [detalhado]="true" />
    <p class="text-sm text-gray-600">
        A locadora usa estes dados para liberar o carro. Preencha uma vez e use em todas as suas reservas.
    </p>
    <hr class="my-4 border-muted/30" />

    @if (aviso()) {
    <p class="mb-4 rounded-lg px-4 py-3 text-sm selo-ambar">{{ aviso() }}</p>
    }

    <mat-card class="p-4 rounded-2xl shadow-sm">
        <form [formGroup]="form" (ngSubmit)="salvar()" class="flex flex-col gap-2">
            <h3 class="text-base font-medium">Documentos</h3>
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
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
            </div>

            <h3 class="text-base font-medium pt-2">Contato</h3>
            <mat-form-field>
                <mat-label>Telefone</mat-label>
                <input matInput formControlName="telefone" mask="(00) 0000-0000||(00) 00000-0000" [dropSpecialCharacters]="false"
                    placeholder="(00) 00000-0000" />
                @if (erroDo('telefone'); as erro) { <mat-error>{{ erro }}</mat-error> }
            </mat-form-field>

            <h3 class="text-base font-medium pt-2">Endereço</h3>
            <div class="grid grid-cols-1 sm:grid-cols-4 gap-4">
                <mat-form-field>
                    <mat-label>Estado</mat-label>
                    <mat-select formControlName="estado">
                        @for (uf of estados; track uf) {
                        <mat-option [value]="uf">{{ uf }}</mat-option>
                        }
                    </mat-select>
                    @if (erroDo('estado'); as erro) { <mat-error>Selecione o estado.</mat-error> }
                </mat-form-field>

                <mat-form-field class="sm:col-span-3">
                    <mat-label>Cidade</mat-label>
                    <input matInput formControlName="cidade" />
                    @if (erroDo('cidade'); as erro) { <mat-error>{{ erro }}</mat-error> }
                </mat-form-field>

                <mat-form-field class="sm:col-span-2">
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
                    <input matInput formControlName="numero" />
                    @if (erroDo('numero'); as erro) { <mat-error>{{ erro }}</mat-error> }
                </mat-form-field>
            </div>

            <div class="flex items-center justify-end gap-3 pt-4">
                <button type="submit" mat-flat-button class="px-6" [disabled]="salvando()">
                    <mat-icon class="!text-base mr-1">check</mat-icon>
                    Salvar
                </button>
            </div>
        </form>
    </mat-card>
</section>
`,
})
export class MeusDados implements OnInit {
    private readonly formBuilder = inject(FormBuilder);
    private readonly service = inject(ReservaService);
    private readonly notificacao = inject(NotificacaoService);
    private readonly router = inject(Router);

    protected readonly estados = ESTADOS;
    protected readonly salvando = signal(false);
    protected readonly aviso = signal<string | null>(null);

    /** Quando vem de "Reservar", volta para lá depois de salvar. */
    private readonly voltarPara: string | null =
        this.router.getCurrentNavigation()?.extras.state?.['voltarPara'] ?? null;

    protected readonly form = this.formBuilder.group({
        cpf: ['', [Validators.required, Validators.pattern(/^\d{3}\.\d{3}\.\d{3}-\d{2}$/)]],
        rg: ['', [Validators.required, Validators.maxLength(20)]],
        cnh: ['', [Validators.required, Validators.pattern(/^\d{11}$/)]],
        validadeCnh: ['', [Validators.required, dataBrValida]],
        telefone: ['', [Validators.required, Validators.pattern(/^\(\d{2}\) \d{4,5}-\d{4}$/)]],
        estado: ['', [Validators.required]],
        cidade: ['', [Validators.required, Validators.maxLength(100)]],
        bairro: ['', [Validators.required, Validators.maxLength(100)]],
        rua: ['', [Validators.required, Validators.maxLength(150)]],
        numero: ['', [Validators.required, Validators.maxLength(20)]],
    });

    ngOnInit(): void {
        this.service.obterMeusDados().subscribe({
            next: (perfil) => {
                this.form.patchValue({
                    ...perfil,
                    validadeCnh: perfil.validadeCnh ? isoParaDataBr(perfil.validadeCnh) : '',
                });

                if (!perfil.completo) this.aviso.set('Complete seus dados para poder reservar um carro.');
                else if (perfil.cnhVencida) this.aviso.set(AVISO_CNH_VENCIDA);
            },
            error: (err) =>
                this.notificacao.erro(mensagemDeErro(err, 'Não foi possível carregar seus dados.')),
        });
    }

    protected erroDo(campo: string): string | null {
        const controle = this.form.get(campo);

        if (!controle || !controle.errors || !controle.touched) return null;
        if (controle.errors['required']) return 'Campo obrigatório.';
        if (controle.errors['dataInvalida']) return 'Informe uma data válida (dd/mm/aaaa).';
        if (controle.errors['pattern']) return 'Formato inválido.';
        if (controle.errors['maxlength'])
            return `Informe no máximo ${controle.errors['maxlength'].requiredLength} caracteres.`;

        return 'Valor inválido.';
    }

    protected salvar(): void {
        this.form.markAllAsTouched();

        if (this.form.invalid) return;

        const v = this.form.getRawValue();

        this.salvando.set(true);

        this.service
            .salvarMeusDados({
                cpf: v.cpf!,
                rg: v.rg!,
                cnh: v.cnh!,
                validadeCnh: dataBrParaIso(v.validadeCnh!)!,
                telefone: v.telefone!,
                estado: v.estado!,
                cidade: v.cidade!,
                bairro: v.bairro!,
                rua: v.rua!,
                numero: v.numero!,
            })
            .subscribe({
                next: (perfil) => {
                    this.salvando.set(false);
                    this.notificacao.sucesso('Seus dados foram salvos!');
                    this.aviso.set(perfil.cnhVencida ? AVISO_CNH_VENCIDA : null);

                    if (this.voltarPara) this.router.navigateByUrl(this.voltarPara);
                },
                error: (err) => {
                    this.salvando.set(false);
                    this.notificacao.erro(mensagemDeErro(err, 'Não foi possível salvar seus dados.'));
                },
            });
    }
}

const AVISO_CNH_VENCIDA = 'Sua CNH está vencida: a locadora só libera o carro com a CNH em dia.';
