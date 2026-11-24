import { TipoCombustivelModel } from '../automoveis/automovel.models';

export type OrdenacaoCatalogoModel = 'MenorPreco' | 'MaiorPreco' | 'MaisNovo' | 'Marca';

export const ORDENACOES: { valor: OrdenacaoCatalogoModel; rotulo: string }[] = [
    { valor: 'MenorPreco', rotulo: 'Menor preço' },
    { valor: 'MaiorPreco', rotulo: 'Maior preço' },
    { valor: 'MaisNovo', rotulo: 'Mais novo' },
    { valor: 'Marca', rotulo: 'Marca e modelo' },
];

/** Tudo é opcional: sem filtros, a busca devolve a frota inteira. Datas em yyyy-MM-dd. */
export interface FiltrosCatalogoModel {
    retirada?: string;
    devolucao?: string;
    texto?: string;
    categoria?: string;
    combustivel?: TipoCombustivelModel;
    anoMinimo?: number;
    diariaMaxima?: number;
    locadoraId?: string;
    apenasDisponiveis?: boolean;
    ordenacao?: OrdenacaoCatalogoModel;
    pagina?: number;
    tamanhoPagina?: number;
}

export interface CarroCatalogoModel {
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
    /** Total de fotos (capa + extras): o carrossel mostra as posições 0 a quantidadeFotos − 1. */
    quantidadeFotos: number;
    diariaAPartirDe: number | null;
    /** Diárias do período × menor diária (sem km, taxas, seguro e caução). Só quando há datas. */
    totalDoPeriodo: number | null;
    /** Só quando há datas. */
    disponivel: boolean | null;
    /** O próximo período em que o carro está alugado (para quem ainda não escolheu datas). */
    proximaOcupacao?: { retirada: string; devolucao: string } | null;
    /** O pedido deste cliente para este carro que ainda vale (aguardando resposta ou aprovado). */
    meuPedido?: { reservaId: string; status: 'Pendente' | 'Aprovada'; retirada: string; devolucao: string } | null;
}

export interface ResultadoCatalogoModel {
    registros: CarroCatalogoModel[];
    total: number;
    pagina: number;
    tamanhoPagina: number;
    dias: number | null;
}

export interface LocadoraFiltroModel {
    id: string;
    nome: string;
}

export interface OpcoesFiltroCatalogoModel {
    categorias: string[];
    marcas: string[];
    locadoras: LocadoraFiltroModel[];
}
