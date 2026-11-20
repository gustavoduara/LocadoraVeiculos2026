import { format } from 'date-fns';
import { NgxMaskDirective } from 'ngx-mask';
import { catchError, debounceTime, map, of, switchMap } from 'rxjs';

import { CurrencyPipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { dataBrParaIso, isoParaDataBr } from '../../condutores/condutor.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import {
    AluguelModel,
    DetalhamentoFinalModel,
    DevolucaoRequestModel,
    rotuloPlano,
} from '../aluguel.models';
import { AluguelService } from '../aluguel.service';

/** Registra a devolução do veículo e mostra o valor final calculado pela API. */
@Component({
    selector: 'app-devolucao-aluguel',
    imports: [
        ReactiveFormsModule,
        MatButtonModule,
        MatCardModule,
        MatCheckboxModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        NgxMaskDirective,
        RouterLink,
        CurrencyPipe,
    ],
    templateUrl: './devolucao-aluguel.html',
})
export class DevolucaoAluguel {
    private readonly formBuilder = inject(FormBuilder);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly route = inject(ActivatedRoute);
    protected readonly router = inject(Router);
    protected readonly aluguelService = inject(AluguelService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly aluguel = this.route.snapshot.data['aluguel'] as AluguelModel;

    protected readonly dataBr = isoParaDataBr;
    protected readonly rotuloPlano = rotuloPlano;

    protected readonly simulacao = signal<DetalhamentoFinalModel | null>(null);
    protected readonly erroSimulacao = signal<string | null>(null);

    protected readonly form: FormGroup = this.formBuilder.group({
        dataDevolucao: [format(new Date(), 'dd/MM/yyyy'), [Validators.required]],
        kmFinal: [this.aluguel.kmInicial, [Validators.required, Validators.min(this.aluguel.kmInicial)]],
        tanqueCheio: [true],
    });

    constructor() {
        this.form.valueChanges
            .pipe(
                debounceTime(300),
                map(() => this.montarDados()),
                switchMap((dados) => {
                    if (!dados) return of({ resultado: null, erro: null as string | null });

                    return this.aluguelService.calcularDevolucao(this.aluguel.id, dados).pipe(
                        map((resultado) => ({ resultado, erro: null as string | null })),
                        catchError((err) =>
                            of({
                                resultado: null,
                                erro: mensagemDeErro(err, 'Não foi possível calcular o valor final.'),
                            }),
                        ),
                    );
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(({ resultado, erro }) => {
                this.simulacao.set(resultado);
                this.erroSimulacao.set(erro);
            });

        // Calcula já com os valores iniciais.
        this.form.updateValueAndValidity();
    }

    private montarDados(): DevolucaoRequestModel | null {
        const v = this.form.getRawValue();
        const data = dataBrParaIso(v.dataDevolucao ?? '');

        if (!data || v.kmFinal === null || v.kmFinal === '' || v.kmFinal < this.aluguel.kmInicial)
            return null;

        return { dataDevolucao: data, kmFinal: Number(v.kmFinal), tanqueCheio: !!v.tanqueCheio };
    }

    protected get erroKm(): string | null {
        const controle = this.form.get('kmFinal')!;

        if (!controle.touched || !controle.errors) return null;

        return controle.errors['min']
            ? `A quilometragem não pode ser menor que a inicial (${this.aluguel.kmInicial}).`
            : 'Campo obrigatório.';
    }

    public devolver(): void {
        this.form.markAllAsTouched();

        const dados = this.montarDados();

        if (!dados) {
            this.notificacaoService.erro('Informe uma data válida e a quilometragem atual do veículo.');
            return;
        }

        this.aluguelService.devolver(this.aluguel.id, dados).subscribe({
            next: () => this.notificacaoService.sucesso('A devolução foi registrada com sucesso!'),
            error: (err) =>
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível registrar a devolução.')),
            complete: () => this.router.navigate(['/alugueis']),
        });
    }
}
