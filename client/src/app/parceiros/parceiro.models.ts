export interface ParceiroModel {
    id: string;
    nome: string;
}

export interface SelecionarParceirosResponseModel {
    registros: ParceiroModel[];
}

export interface CadastrarParceiroModel {
    nome: string;
}

export interface CadastrarParceiroResponseModel {
    id: string;
}

export type EditarParceiroModel = CadastrarParceiroModel;

export type EditarParceiroResponseModel = CadastrarParceiroModel;
