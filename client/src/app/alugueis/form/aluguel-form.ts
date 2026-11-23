import { format } from 'date-fns';
import { NgxMaskDirective } from 'ngx-mask';
import { catchError, debounceTime, filter, map, of, switchMap } from 'rxjs';

import { CurrencyPipe } from '@angular/common';
import { Component, computed, DestroyRef, EventEmitter, inject, Input, Output, signal, ViewChild } from '@angular/core';
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
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';

import { AutomovelModel, formatarPlaca } from '../../automoveis/automovel.models';
import { ClienteModel } from '../../clientes/cliente.models';
import { CondutorModel, dataBrParaIso, isoParaDataBr } from '../../condutores/condutor.models';
import { CupomModel, descricaoDoDesconto } from '../../cupons/cupom.models';
import { SelecionarGruposVeiculosModel } from '../../grupo-veiculos/grupo-veiculos.models';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { PrecoComDesconto, ratearDesconto } from '../../shared/preco/preco-com-desconto';
import { TaxaServicoModel } from '../../taxas-servicos/taxa-servico.models';
import {
    AluguelModel,
    DadosAluguelModel,
    SimulacaoAluguelModel,
    TIPOS_PLANO,
    TIPOS_SEGURO,
} from '../aluguel.models';
import { AluguelService } from '../aluguel.service';
import { NovoClienteAlterado, NovoClienteForm } from '../novo-cliente/novo-cliente-form';

function dataBrValida(controle: AbstractControl): ValidationErrors | null {
    const valor = (controle.value ?? '') as string;

    if (!valor) return null;

    return dataBrParaIso(valor) ? null : { dataInvalida: true };
}

/** Formulário usado no registro e na edição de aluguéis, com o valor total calculado pela API. */
@Component({
    selector: 'app-aluguel-form',
    imports: [
        ReactiveFormsModule,
        MatButtonModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatSelectModule,
        NgxMaskDirective,
        RouterLink,
        CurrencyPipe,
        PrecoComDesconto,
        MatRadioModule,
        NovoClienteForm,
    ],
    templateUrl: './aluguel-form.html',
})
export class AluguelForm {
    private readonly formBuilder = inject(FormBuilder);
    private readonly destroyRef = inject(DestroyRef);
    private readonly aluguelService = inject(AluguelService);

    @Input() clientes: ClienteModel[] = [];
    @Input() condutores: CondutorModel[] = [];
    @Input() grupos: SelecionarGruposVeiculosModel[] = [];
    @Input() automoveis: AutomovelModel[] = [];
    @Input() taxas: TaxaServicoModel[] = [];
    @Input() cupons: CupomModel[] = [];

    /** Quando informado, o formulário entra em modo de edição. */
    @Input() set aluguel(valor: AluguelModel | undefined) {
        if (!valor) return;

        this.edicao = true;

        const automovel = this.automoveis.find((a) => a.id === valor.automovelId);

        this.form.patchValue({
            clienteId: valor.clienteId,
            condutorId: valor.condutorId,
            grupoId: automovel?.grupoVeiculosId ?? '',
            automovelId: valor.automovelId,
            tipoPlano: valor.tipoPlano,
            dataSaida: isoParaDataBr(valor.dataSaida),
            dataRetornoPrevista: isoParaDataBr(valor.dataRetornoPrevista),
            kmInicial: valor.kmInicial,
            tipoSeguro: valor.tipoSeguro,
            taxasIds: valor.taxas.map((t) => t.taxaServicoId),
            cupomId: valor.cupomId ?? '',
        });
    }

    /** Carro e datas vindos da vitrine do Início (parâmetros da URL, datas em yyyy-MM-dd). */
    @Input() set preenchimento(
        valor: { automovelId?: string | null; retirada?: string | null; devolucao?: string | null } | undefined,
    ) {
        if (!valor) return;

        const dataValida = (d?: string | null) =>
            d && /^\d{4}-\d{2}-\d{2}$/.test(d) ? isoParaDataBr(d) : null;

        const automovel = this.automoveis.find((a) => a.id === valor.automovelId);
        const retirada = dataValida(valor.retirada);
        const devolucao = dataValida(valor.devolucao);

        this.form.patchValue({
            ...(automovel ? { grupoId: automovel.grupoVeiculosId, automovelId: automovel.id } : {}),
            ...(retirada ? { dataSaida: retirada } : {}),
            ...(devolucao ? { dataRetornoPrevista: devolucao } : {}),
        });
    }

    @Output() salvar = new EventEmitter<DadosAluguelModel>();

    protected edicao = false;

    protected readonly tiposPlano = TIPOS_PLANO;
    protected readonly tiposSeguro = TIPOS_SEGURO;

    /** Valor por dia do seguro do carro escolhido (0 quando o carro não oferece seguro ou ainda não foi escolhido). */
    protected get seguroDoCarro(): number {
        const id = this.form.get('automovelId')?.value;

        return this.automoveis.find((a) => a.id === id)?.seguroDiaria ?? 0;
    }
    protected readonly formatarPlaca = formatarPlaca;

    protected readonly simulacao = signal<SimulacaoAluguelModel | null>(null);

    /** Com cupom, o desconto é repartido entre as linhas para mostrar cada preço atualizado. */
    protected readonly precos = computed(() => {
        const p = this.simulacao()?.previsto;

        if (!p) return null;

        const [diarias, taxas, seguro] = ratearDesconto(
            [p.valorDiarias, p.valorTaxas, p.valorSeguro],
            p.valorDesconto,
        );

        return {
            diarias,
            taxas,
            seguro,
            totalSemCupom: Math.round((p.valorTotalPrevisto + p.valorDesconto) * 100) / 100,
        };
    });

    protected readonly descricaoDesconto = descricaoDoDesconto;
    protected readonly erroSimulacao = signal<string | null>(null);

    @ViewChild(NovoClienteForm) private novoClienteForm?: NovoClienteForm;

    protected form: FormGroup = this.formBuilder.group({
        /** Cliente que já existe na locadora ou um cliente novo (cadastrado junto com o aluguel). */
        modoCliente: ['existente' as 'existente' | 'novo'],
        novoCliente: [null as NovoClienteAlterado['dados'] | null],
        novoClienteValido: [false],
        clienteId: ['', [Validators.required]],
        condutorId: ['', [Validators.required]],
        grupoId: ['', [Validators.required]],
        automovelId: ['', [Validators.required]],
        tipoPlano: ['Diario', [Validators.required]],
        dataSaida: [format(new Date(), 'dd/MM/yyyy'), [Validators.required, dataBrValida]],
        dataRetornoPrevista: ['', [Validators.required, dataBrValida]],
        kmInicial: [0, [Validators.required, Validators.min(0)]],
        tipoSeguro: ['Nenhum', [Validators.required]],
        taxasIds: [[] as string[]],
        cupomId: [''],
    });

    constructor() {
        this.form
            .get('modoCliente')!
            .valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((modo) => this.aplicarModoDoCliente(modo));

        this.aplicarModoDoCliente('existente');

        // Trocar o cliente ou o grupo invalida a escolha dependente (condutor / automóvel).
        this.form
            .get('clienteId')!
            .valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                const condutorId = this.form.get('condutorId')!.value;

                if (!this.condutoresDoCliente.some((c) => c.id === condutorId))
                    this.form.get('condutorId')!.setValue('');
            });

        this.form
            .get('grupoId')!
            .valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                const automovelId = this.form.get('automovelId')!.value;

                if (!this.automoveisDoGrupo.some((a) => a.id === automovelId))
                    this.form.get('automovelId')!.setValue('');
            });

        // Recalcula o valor previsto sempre que os dados mudam e estão completos.
        this.form.valueChanges
            .pipe(
                debounceTime(300),
                map(() => this.montarDados()),
                filter((dados): dados is DadosAluguelModel => {
                    if (!dados) this.simulacao.set(null);

                    return !!dados;
                }),
                switchMap((dados) =>
                    this.aluguelService.calcular(dados).pipe(
                        map((resultado) => ({ resultado, erro: null as string | null })),
                        catchError((err) =>
                            of({
                                resultado: null,
                                erro: mensagemDeErro(err, 'Não foi possível calcular o valor.'),
                            }),
                        ),
                    ),
                ),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(({ resultado, erro }) => {
                this.simulacao.set(resultado);
                this.erroSimulacao.set(erro);
            });
    }

    /** Só o modo escolhido exige os seus campos (cliente e condutor existentes, ou os dados do cliente novo). */
    private aplicarModoDoCliente(modo: 'existente' | 'novo'): void {
        const novo = modo === 'novo';

        for (const campo of ['clienteId', 'condutorId'])
            novo ? this.form.get(campo)!.disable({ emitEvent: false }) : this.form.get(campo)!.enable({ emitEvent: false });

        const valido = this.form.get('novoClienteValido')!;

        valido.setValidators(novo ? [Validators.requiredTrue] : []);
        valido.updateValueAndValidity({ emitEvent: false });

        this.form.updateValueAndValidity();
    }

    protected get clienteNovo(): boolean {
        return this.form.get('modoCliente')!.value === 'novo';
    }

    protected aoMudarNovoCliente(alterado: NovoClienteAlterado): void {
        this.form.patchValue({ novoCliente: alterado.dados, novoClienteValido: alterado.valido });
    }

    /**
     * Só cupons ativos podem ser escolhidos; ao editar, o cupom que o aluguel já usa continua
     * disponível mesmo vencido.
     */
    protected get cuponsDisponiveis(): CupomModel[] {
        const atual = this.form.get('cupomId')!.value;

        return this.cupons.filter((c) => c.ativo || c.id === atual);
    }

    protected get condutoresDoCliente(): CondutorModel[] {
        const clienteId = this.form.get('clienteId')!.value;

        return this.condutores.filter((c) => c.clienteId === clienteId);
    }

    protected get automoveisDoGrupo(): AutomovelModel[] {
        const grupoId = this.form.get('grupoId')!.value;

        return this.automoveis.filter((a) => a.grupoVeiculosId === grupoId);
    }

    /** Dados prontos para a API, ou null enquanto o formulário estiver incompleto. */
    private montarDados(): DadosAluguelModel | null {
        const v = this.form.getRawValue();

        const saida = dataBrParaIso(v.dataSaida ?? '');
        const retorno = dataBrParaIso(v.dataRetornoPrevista ?? '');

        const novo = v.modoCliente === 'novo';

        if (novo ? !v.novoCliente : !v.condutorId) return null;
        if (!v.automovelId || !saida || !retorno) return null;
        if (v.kmInicial === null || v.kmInicial === '' || v.kmInicial < 0) return null;

        return {
            ...(novo ? { novoCliente: v.novoCliente } : { condutorId: v.condutorId }),
            automovelId: v.automovelId,
            tipoPlano: v.tipoPlano,
            dataSaida: saida,
            dataRetornoPrevista: retorno,
            kmInicial: Number(v.kmInicial),
            tipoSeguro: v.tipoSeguro,
            taxasIds: v.taxasIds ?? [],
            cupomId: v.cupomId || null,
        };
    }

    protected erroDo(campo: string): string | null {
        const controle = this.form.get(campo);

        if (!controle || !controle.errors || !controle.touched) return null;
        if (controle.errors['required']) return 'Campo obrigatório.';
        if (controle.errors['dataInvalida']) return 'Informe uma data válida (dd/mm/aaaa).';
        if (controle.errors['min']) return 'Informe um valor igual ou maior que zero.';

        return 'Valor inválido.';
    }

    public enviar(): void {
        this.form.markAllAsTouched();
        this.novoClienteForm?.marcarComoTocado();

        if (this.form.invalid) return;

        const dados = this.montarDados();

        if (dados) this.salvar.emit(dados);
    }
}
