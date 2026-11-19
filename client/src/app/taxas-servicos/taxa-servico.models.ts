export type TipoCobrancaTaxaModel = 'Fixo' | 'Diario';

export const TIPOS_COBRANCA: { valor: TipoCobrancaTaxaModel; rotulo: string; descricao: string }[] = [
    { valor: 'Fixo', rotulo: 'Preço fixo', descricao: 'Cobrado uma única vez no aluguel' },
    { valor: 'Diario', rotulo: 'Cobrança diária', descricao: 'Multiplicado pelos dias do aluguel' },
];

export function rotuloTipoCobranca(valor: string): string {
    return TIPOS_COBRANCA.find((t) => t.valor === valor)?.rotulo ?? valor;
}

export interface TaxaServicoModel {
    id: string;
    nome: string;
    preco: number;
    tipoCobranca: TipoCobrancaTaxaModel;
}

export interface SelecionarTaxasServicosResponseModel {
    registros: TaxaServicoModel[];
}

export interface CadastrarTaxaServicoModel {
    nome: string;
    preco: number;
    tipoCobranca: TipoCobrancaTaxaModel;
}

export interface CadastrarTaxaServicoResponseModel {
    id: string;
}

export type EditarTaxaServicoModel = CadastrarTaxaServicoModel;

export type EditarTaxaServicoResponseModel = CadastrarTaxaServicoModel;
