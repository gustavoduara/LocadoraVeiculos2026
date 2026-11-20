export interface CupomModel {
    id: string;
    nome: string;
    valor: number;
    /** Data no formato ISO (yyyy-MM-dd). */
    dataValidade: string;
    parceiroId: string;
    nomeParceiro: string;
    /** Verdadeiro enquanto a validade não passou: só cupons ativos entram em um aluguel. */
    ativo: boolean;
}

export interface SelecionarCuponsResponseModel {
    registros: CupomModel[];
}

export interface CadastrarCupomModel {
    nome: string;
    valor: number;
    dataValidade: string;
    parceiroId: string;
}

export interface CadastrarCupomResponseModel {
    id: string;
}

export type EditarCupomModel = CadastrarCupomModel;

export interface CupomMaisUtilizadoModel {
    cupomId: string;
    nome: string;
    nomeParceiro: string;
    valor: number;
    usos: number;
}

export interface ParceiroMaisUtilizadoModel {
    parceiroId: string;
    nomeParceiro: string;
    usos: number;
}

export interface CuponsMaisUtilizadosModel {
    cupons: CupomMaisUtilizadoModel[];
    parceiros: ParceiroMaisUtilizadoModel[];
}
