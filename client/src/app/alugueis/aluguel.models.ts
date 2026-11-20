export type TipoPlanoModel = 'Diario' | 'Controlado' | 'Livre';
export type TipoSeguroModel = 'Nenhum' | 'Cliente' | 'Terceiros';
export type StatusAluguelModel = 'EmAberto' | 'Concluido';
export type TipoCobrancaTaxaAluguelModel = 'Fixo' | 'Diario';

export const TIPOS_PLANO: { valor: TipoPlanoModel; rotulo: string }[] = [
    { valor: 'Diario', rotulo: 'Plano Diário' },
    { valor: 'Controlado', rotulo: 'Plano Controlado' },
    { valor: 'Livre', rotulo: 'Plano Livre' },
];

export const TIPOS_SEGURO: { valor: TipoSeguroModel; rotulo: string }[] = [
    { valor: 'Nenhum', rotulo: 'Sem seguro' },
    { valor: 'Cliente', rotulo: 'Seguro para o cliente' },
    { valor: 'Terceiros', rotulo: 'Seguro para terceiros' },
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
}

export interface SelecionarAlugueisResponseModel {
    registros: AluguelModel[];
}

/** Dados enviados ao registrar, editar ou simular um aluguel. Datas em ISO (yyyy-MM-dd). */
export interface DadosAluguelModel {
    condutorId: string;
    automovelId: string;
    tipoPlano: TipoPlanoModel;
    dataSaida: string;
    dataRetornoPrevista: string;
    kmInicial: number;
    tipoSeguro: TipoSeguroModel;
    taxasIds: string[];
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
