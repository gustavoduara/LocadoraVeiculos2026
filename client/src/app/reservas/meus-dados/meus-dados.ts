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

    @if (salvoAgora()) {
    <mat-card class="mb-4 rounded-2xl shadow-sm" role="status">
        <mat-card-content class="flex flex-col items-center gap-2 p-6 text-center">
            <mat-icon class="!h-14 !w-14 !text-[56px] text-green-700">check_circle</mat-icon>
            <h3 class="text-lg font-semibold">Dados salvos com sucesso!</h3>
            <p class="text-sm text-gray-600">
                @if (voltarPara) { Pronto, agora você já pode continuar a sua reserva. }
                @else { Seus dados já estão valendo para as próximas reservas. Para mudar algo, use o lápis abaixo. }
            </p>
            @if (voltarPara) {
            <button type="button" mat-flat-button class="mt-2 px-6" (click)="continuar()">
                <mat-icon class="!text-base mr-1">arrow_forward</mat-icon>
                Continuar a reserva
            </button>
            }
        </mat-card-content>
    </mat-card>
    }

    @if (aviso()) {
    <p class="mb-4 rounded-lg px-4 py-3 text-sm selo-ambar">{{ aviso() }}</p>
    }

    <mat-card class="p-4 rounded-2xl shadow-sm">
        <form [formGroup]="form" (ngSubmit)="salvar()" class="flex flex-col gap-2">
            <div class="flex items-center justify-between">
                <h3 class="text-base font-medium">Documentos</h3>
                @if (!editando()) {
                <button type="button" mat-icon-button aria-label="Editar meus dados" title="Editar meus dados" (click)="comecarEdicao()">
                    <mat-icon>edit</mat-icon>
                </button>
                }
            </div>
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

            @if (editando()) {
            <div class="flex items-center justify-end gap-3 pt-4">
                @if (jaTinhaDados()) {
                <button type="button" mat-stroked-button class="px-4" (click)="cancelarEdicao()" [disabled]="salvando()">Cancelar</button>
                }
                <button type="submit" mat-flat-button class="px-6" [disabled]="salvando()">
                    <mat-icon class="!text-base mr-1">check</mat-icon>
                    Salvar
                </button>
            </div>
            }
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

    /** Com os dados já completos a tela abre só para leitura; o lápis liberta os campos. */
    protected readonly editando = signal(true);

    /** Quem já tinha dados salvos pode cancelar a edição; no primeiro preenchimento não há o que cancelar. */
    protected readonly jaTinhaDados = signal(false);

    /** Mostra o cartão verde de "salvo com sucesso" logo depois de gravar. */
    protected readonly salvoAgora = signal(false);

    private valoresSalvos: Record<string, unknown> | null = null;

    /** Quando vem de "Reservar", volta para lá depois de salvar. */
    protected readonly voltarPara: string | null =
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

                if (perfil.completo) this.mostrarSoLeitura();

                if (!perfil.completo) this.aviso.set('Complete seus dados para poder reservar um carro.');
                else if (perfil.cnhVencida) this.aviso.set(AVISO_CNH_VENCIDA);
            },
            error: (err) =>
                this.notificacao.erro(mensagemDeErro(err, 'Não foi possível carregar seus dados.')),
        });
    }

    private mostrarSoLeitura(): void {
        this.valoresSalvos = this.form.getRawValue();
        this.jaTinhaDados.set(true);
        this.editando.set(false);
        this.form.disable({ emitEvent: false });
    }

    protected comecarEdicao(): void {
        this.salvoAgora.set(false);
        this.editando.set(true);
        this.form.enable({ emitEvent: false });
    }

    protected cancelarEdicao(): void {
        if (this.valoresSalvos) this.form.patchValue(this.valoresSalvos);

        this.form.markAsUntouched();
        this.editando.set(false);
        this.form.disable({ emitEvent: false });
    }

    protected continuar(): void {
        if (this.voltarPara) this.router.navigateByUrl(this.voltarPara);
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
                    this.aviso.set(perfil.cnhVencida ? AVISO_CNH_VENCIDA : null);

                    this.mostrarSoLeitura();
                    this.salvoAgora.set(true);

                    // O cartão de sucesso precisa aparecer no topo, onde a pessoa está olhando.
                    setTimeout(() => document.querySelector('[role=status]')?.scrollIntoView({ behavior: 'smooth', block: 'start' }));
                },
                error: (err) => {
                    this.salvando.set(false);
                    this.notificacao.erro(mensagemDeErro(err, 'Não foi possível salvar seus dados.'));
                },
            });
    }
}

const AVISO_CNH_VENCIDA = 'Sua CNH está vencida: a locadora só libera o carro com a CNH em dia.';
