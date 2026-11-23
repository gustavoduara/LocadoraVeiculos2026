import { TipoCombustivelModel } from '../automoveis/automovel.models';
import { TipoPlanoModel, TipoSeguroModel } from '../alugueis/aluguel.models';
import { TipoDescontoModel } from '../cupons/cupom.models';
import { TipoCobrancaTaxaModel } from '../taxas-servicos/taxa-servico.models';

export type StatusReservaModel = 'Pendente' | 'Aprovada' | 'Recusada' | 'Cancelada';

export const STATUS_RESERVA: { valor: StatusReservaModel; rotulo: string }[] = [
    { valor: 'Pendente', rotulo: 'Aguardando resposta' },
    { valor: 'Aprovada', rotulo: 'Aprovada' },
    { valor: 'Recusada', rotulo: 'Recusada' },
    { valor: 'Cancelada', rotulo: 'Cancelada' },
];

export function rotuloStatusReserva(valor: string): string {
    return STATUS_RESERVA.find((s) => s.valor === valor)?.rotulo ?? valor;
}

/** Classe do selo de cada situação (cores definidas em styles.scss). */
export function classeDoStatusReserva(valor: string): string {
    switch (valor) {
        case 'Aprovada':
            return 'selo-verde';
        case 'Recusada':
            return 'selo-vermelho';
        case 'Pendente':
            return 'selo-ambar';
        default:
            return 'selo-cinza';
    }
}

// ---------------- dados do cliente (Meus dados) ----------------

export interface PerfilClienteModel {
    completo: boolean;
    cpf: string;
    rg: string;
    cnh: string;
    /** yyyy-MM-dd */
    validadeCnh: string | null;
    cnhVencida: boolean;
    telefone: string;
    estado: string;
    cidade: string;
    bairro: string;
    rua: string;
    numero: string;
}

export interface SalvarPerfilClienteModel {
    cpf: string;
    rg: string;
    cnh: string;
    validadeCnh: string;
    telefone: string;
    estado: string;
    cidade: string;
    bairro: string;
    rua: string;
    numero: string;
}

// ---------------- detalhe do carro (opções para reservar) ----------------

export interface CarroDetalheModel {
    id: string;
    locadoraId: string;
    locadora: string;
    marca: string;
    modelo: string;
    cor: string;
    ano: number;
    combustivel: TipoCombustivelModel;
    categoria: string;
    temFoto: boolean;
    quantidadeFotos: number;
    diario: { precoDiaria: number; precoPorKm: number } | null;
    controlado: { kmDisponiveisPorDia: number; precoDiaria: number; precoPorKmExtrapolado: number } | null;
    livre: { precoDiaria: number } | null;
    seguroClienteDiaria: number | null;
    seguroTerceirosDiaria: number | null;
    valorCaucao: number;
    /** Taxas e serviços que a locadora oferece (o cliente marca as que quer). */
    taxas: TaxaOpcaoModel[];
}

export interface TaxaOpcaoModel {
    id: string;
    nome: string;
    preco: number;
    tipoCobranca: TipoCobrancaTaxaModel;
}

/** Uma taxa da reserva, com o valor já multiplicado pelos dias quando a cobrança é diária. */
export interface TaxaDaReservaModel {
    taxaServicoId: string;
    nome: string;
    precoUnitario: number;
    tipoCobranca: TipoCobrancaTaxaModel;
    valor: number;
}

// ---------------- pedidos ----------------

/** Datas em yyyy-MM-dd. */
export interface DadosReservaModel {
    automovelId: string;
    retirada: string;
    devolucao: string;
    tipoPlano: TipoPlanoModel;
    tipoSeguro: TipoSeguroModel;
    /** Taxas e serviços do catálogo da locadora do carro. */
    taxasIds: string[];
    /** Nome do cupom (opcional) dessa locadora. */
    cupom?: string | null;
}

export interface EstimativaReservaModel {
    dias: number;
    valorDiarias: number;
    valorSeguro: number;
    valorCaucao: number;
    valorTotalPrevisto: number;
    valorTaxas: number;
    valorDesconto: number;
    taxas: TaxaDaReservaModel[];
    cupomNome: string | null;
    cupomTipo: TipoDescontoModel | null;
    cupomValor: number;
}

export interface ReservaModel {
    id: string;
    status: StatusReservaModel;
    nomeLocadora: string;
    telefoneLocadora: string | null;
    automovelId: string;
    descricaoVeiculo: string;
    nomeGrupo: string;
    retirada: string;
    devolucao: string;
    dias: number;
    tipoPlano: TipoPlanoModel;
    tipoSeguro: TipoSeguroModel;
    valorDiarias: number;
    valorSeguro: number;
    valorCaucao: number;
    valorTotalPrevisto: number;
    motivoRecusa: string | null;
    aluguelId: string | null;
    criadaEmUtc: string;
    respondidaEmUtc: string | null;
    clienteNome: string;
    clienteEmail: string;
    clienteTelefone: string;
    clienteCpf: string;
    clienteCnh: string;
    clienteValidadeCnh: string;
    clienteCidade: string;
    clienteEstado: string;
    valorTaxas: number;
    valorDesconto: number;
    taxas: TaxaDaReservaModel[];
    cupomNome: string | null;
    cupomTipo: TipoDescontoModel | null;
    cupomValor: number;
}

export interface SelecionarReservasResponseModel {
    registros: ReservaModel[];
}
