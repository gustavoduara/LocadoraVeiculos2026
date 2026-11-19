export interface CadastrarGrupoVeiculosModel {
    nome: string;
}

export interface CadastrarGrupoVeiculosResponseModel {
    id: string;
}

export interface EditarGrupoVeiculosModel {
    nome: string;
}

export interface EditarGrupoVeiculosResponseModel {
    nome: string;
}

export interface SelecionarGruposVeiculosResponseModel {
    registros: SelecionarGruposVeiculosModel[];
}

export interface SelecionarGruposVeiculosModel {
    plano?: PlanoResumoModel | null;
    id: string;
    nome: string;
}

export interface DetalhesGrupoVeiculosModel {
    id: string;
    nome: string;
}

export interface PlanoResumoModel {
    id: string;
    diario: { precoDiaria: number; precoPorKm: number };
    controlado: { kmDisponiveis: number; precoDiaria: number; precoPorKmExtrapolado: number };
    livre: { precoDiaria: number };
}
