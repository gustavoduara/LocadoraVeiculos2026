import { TipoClienteModel } from '../clientes/cliente.models';

export type TipoPlanoModel = 'Diario' | 'Controlado' | 'Livre';
export type TipoSeguroModel = 'Nenhum' | 'Contratado';
export type StatusAluguelModel = 'EmAberto' | 'Concluido';
export type TipoCobrancaTaxaAluguelModel = 'Fixo' | 'Diario';

export const TIPOS_PLANO: { valor: TipoPlanoModel; rotulo: string }[] = [
    { valor: 'Diario', rotulo: 'Plano Diário' },
    { valor: 'Controlado', rotulo: 'Plano Controlado' },
    { valor: 'Livre', rotulo: 'Plano Livre' },
];

export const TIPOS_SEGURO: { valor: TipoSeguroModel; rotulo: string }[] = [
    { valor: 'Nenhum', rotulo: 'Sem seguro' },
    { valor: 'Contratado', rotulo: 'Com seguro' },
];

export function rotuloPlano(valor: string): string {
    return TIPOS_PLANO.find((t) => t.valor === valor)?.rotulo ?? valor;
}

export function rotuloSeguro(valor: string): string {
    return TIPOS_SEGURO.find((t) => t.valor === valor)?.rotulo ?? valor;
}

export function rotuloStatus(valor: string): string {
    return valor === 'EmAberto' ? 'Em aberto' : 'Concluído';
}

export interface AluguelTaxaModel {
    taxaServicoId: string;
    nome: string;
    precoUnitario: number;
    tipoCobranca: TipoCobrancaTaxaAluguelModel;
    valor: number;
}

export interface DetalhamentoPrevistoModel {
    dias: number;
    valorCaucao: number;
    valorDiarias: number;
    valorTaxas: number;
    valorSeguro: number;
    valorDesconto: number;
    valorTotalPrevisto: number;
}

export interface DetalhamentoFinalModel {
    dias: number;
    kmRodados: number;
    atrasado: boolean;
    valorCaucao: number;
    valorDiarias: number;
    valorKm: number;
    valorTaxas: number;
    valorSeguro: number;
    valorCombustivel: number;
    valorMulta: number;
    valorDesconto: number;
    valorFinal: number;
}

export interface DevolucaoModel {
    /** Datas no formato ISO (yyyy-MM-dd). */
    dataDevolucao: string;
    kmFinal: number;
    tanqueCheio: boolean;
    final: DetalhamentoFinalModel;
}

export interface AluguelModel {
    id: string;
    condutorId: string;
    nomeCondutor: string;
    clienteId: string;
    nomeCliente: string;
    automovelId: string;
    descricaoVeiculo: string;
    nomeGrupo: string;
    tipoPlano: TipoPlanoModel;
    dataSaida: string;
    dataRetornoPrevista: string;
    kmInicial: number;
    tipoSeguro: TipoSeguroModel;
    taxas: AluguelTaxaModel[];
    status: StatusAluguelModel;
    previsto: DetalhamentoPrevistoModel;
    devolucao: DevolucaoModel | null;
    cupomId: string | null;
    nomeCupom: string | null;
}

export interface SelecionarAlugueisResponseModel {
    registros: AluguelModel[];
}

/** Condutor de um cliente novo (pessoa jurídica). Datas em ISO. */
export interface CondutorNovoAluguelModel {
    nome: string;
    email: string;
    telefone: string;
    cpf: string;
    cnh: string;
    validadeCnh: string;
}

/**
 * Cliente cadastrado junto com o aluguel. Pessoa física: cpf, rg, cnh e validadeCnh (ela mesma dirige).
 * Pessoa jurídica: cnpj e o condutor.
 */
export interface NovoClienteAluguelModel {
    tipo: TipoClienteModel;
    nome: string;
    email: string;
    telefone: string;
    cpf: string | null;
    rg: string | null;
    cnh: string | null;
    cnpj: string | null;
    estado: string;
    cidade: string;
    bairro: string;
    rua: string;
    numero: string;
    validadeCnh: string;
    condutor: CondutorNovoAluguelModel | null;
}

/** Dados enviados ao registrar, editar ou simular um aluguel. Datas em ISO (yyyy-MM-dd). */
export interface DadosAluguelModel {
    /** Um condutor existente; ausente quando o cliente é novo. */
    condutorId?: string;
    /** Cliente que ainda não existe: só ao registrar (ou simular) o aluguel. */
    novoCliente?: NovoClienteAluguelModel;
    automovelId: string;
    tipoPlano: TipoPlanoModel;
    dataSaida: string;
    dataRetornoPrevista: string;
    kmInicial: number;
    tipoSeguro: TipoSeguroModel;
    taxasIds: string[];
    cupomId: string | null;
}

export interface CadastrarAluguelResponseModel {
    id: string;
}

export interface SimulacaoAluguelModel {
    taxas: AluguelTaxaModel[];
    previsto: DetalhamentoPrevistoModel;
}

export interface DevolucaoRequestModel {
    dataDevolucao: string;
    kmFinal: number;
    tanqueCheio: boolean;
}
