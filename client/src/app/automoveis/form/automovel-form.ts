import { NgxMaskDirective } from 'ngx-mask';

import { Component, EventEmitter, inject, Input, OnDestroy, Output, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';

import { comprimirImagem } from '../../shared/imagem/comprimir-imagem';
import {
    AutomovelModel,
    CadastrarAutomovelModel,
    COMBUSTIVEIS,
    formatarPlaca,
    FotoSelecionadaModel,
    GrupoOpcaoModel,
    SalvarAutomovelEvento,
} from '../automovel.models';
import { FotoAutomovel } from '../foto/foto-automovel';

const anoMaximo = new Date().getFullYear() + 1;

/** Formulário usado no cadastro e na edição de automóveis (dados + foto). */
@Component({
    selector: 'app-automovel-form',
    imports: [
        ReactiveFormsModule,
        MatButtonModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatSelectModule,
        NgxMaskDirective,
        RouterLink,
        FotoAutomovel,
    ],
    templateUrl: './automovel-form.html',
})
export class AutomovelForm implements OnDestroy {
    private readonly formBuilder = inject(FormBuilder);

    @Input() grupos: GrupoOpcaoModel[] = [];

    /** Quando informado, o formulário entra em modo de edição. */
    @Input() set automovel(valor: AutomovelModel | undefined) {
        if (!valor) return;

        this.original = valor;
        this.edicao = true;

        this.form.patchValue({ ...valor, placa: formatarPlaca(valor.placa) });
    }

    @Output() salvar = new EventEmitter<SalvarAutomovelEvento>();

    protected original?: AutomovelModel;
    protected edicao = false;
    protected readonly combustiveis = COMBUSTIVEIS;

    protected readonly fotoNova = signal<File | null>(null);
    protected readonly previaUrl = signal<string | null>(null);
    protected readonly fotoRemovida = signal(false);
    protected readonly erroFoto = signal<string | null>(null);
    protected readonly processandoFoto = signal(false);

    protected form: FormGroup = this.formBuilder.group({
        grupoVeiculosId: ['', [Validators.required]],
        placa: ['', [Validators.required, Validators.pattern(/^[A-Za-z]{3}-?\d[A-Za-z0-9]\d{2}$/)]],
        marca: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
        modelo: ['', [Validators.required, Validators.maxLength(50)]],
        cor: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(30)]],
        ano: [
            new Date().getFullYear(),
            [Validators.required, Validators.min(1950), Validators.max(anoMaximo)],
        ],
        tipoCombustivel: ['Gasolina', [Validators.required]],
        capacidadeCombustivelLitros: [
            null,
            [Validators.required, Validators.min(1), Validators.max(1000), Validators.pattern(/^\d+$/)],
        ],
    });

    protected erroDo(campo: string): string | null {
        const controle = this.form.get(campo);

        if (!controle || !controle.errors || !controle.touched) return null;
        if (controle.errors['required']) return 'Campo obrigatório.';
        if (controle.errors['pattern'])
            return campo === 'placa' ? 'Use o padrão ABC-1234 ou ABC1D23.' : 'Informe um número inteiro.';
        if (controle.errors['minlength'])
            return `Informe no mínimo ${controle.errors['minlength'].requiredLength} caracteres.`;
        if (controle.errors['maxlength'])
            return `Informe no máximo ${controle.errors['maxlength'].requiredLength} caracteres.`;
        if (controle.errors['min']) return `O valor mínimo é ${controle.errors['min'].min}.`;
        if (controle.errors['max']) return `O valor máximo é ${controle.errors['max'].max}.`;

        return 'Valor inválido.';
    }

    /** Há alguma foto para mostrar/remover (nova ou já salva e não removida)? */
    protected get temFotoAtual(): boolean {
        return this.fotoNova() !== null || (!!this.original?.temFoto && !this.fotoRemovida());
    }

    protected get nomeDaFoto(): string | null {
        if (this.fotoNova()) return this.fotoNova()!.name;
        if (this.original?.temFoto && !this.fotoRemovida()) return this.original.nomeArquivoFoto;

        return null;
    }

    protected async selecionarArquivo(evento: Event): Promise<void> {
        const entrada = evento.target as HTMLInputElement;
        const arquivo = entrada.files?.[0];

        entrada.value = '';

        if (!arquivo) return;

        this.processandoFoto.set(true);
        this.erroFoto.set(null);

        try {
            const pronta = await comprimirImagem(arquivo);

            this.liberarPrevia();
            this.fotoNova.set(pronta);
            this.previaUrl.set(URL.createObjectURL(pronta));
            this.fotoRemovida.set(false);
        } catch (erro) {
            this.erroFoto.set((erro as Error).message);
        } finally {
            this.processandoFoto.set(false);
        }
    }

    protected removerFoto(): void {
        this.liberarPrevia();
        this.fotoNova.set(null);
        this.fotoRemovida.set(true);
        this.erroFoto.set(null);
    }

    public enviar(): void {
        this.form.markAllAsTouched();

        if (this.form.invalid) return;

        let foto: FotoSelecionadaModel = { acao: 'nenhuma' };

        if (this.fotoNova()) foto = { acao: 'enviar', arquivo: this.fotoNova()! };
        else if (this.fotoRemovida() && this.original?.temFoto) foto = { acao: 'remover' };

        this.salvar.emit({ dados: this.form.getRawValue() as CadastrarAutomovelModel, foto });
    }

    ngOnDestroy(): void {
        this.liberarPrevia();
    }

    private liberarPrevia(): void {
        const atual = this.previaUrl();

        if (atual) URL.revokeObjectURL(atual);

        this.previaUrl.set(null);
    }
}
