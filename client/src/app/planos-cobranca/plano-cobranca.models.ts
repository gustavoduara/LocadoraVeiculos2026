export interface PlanoDiarioModel {
    precoDiaria: number;
    precoPorKm: number;
}

export interface PlanoControladoModel {
    kmDisponiveis: number;
    precoDiaria: number;
    precoPorKmExtrapolado: number;
}

export interface PlanoLivreModel {
    precoDiaria: number;
}

export interface CadastrarPlanoCobrancaModel {
    grupoVeiculosId: string;
    diario: PlanoDiarioModel;
    controlado: PlanoControladoModel;
    livre: PlanoLivreModel;
}

export interface CadastrarPlanoCobrancaResponseModel {
    id: string;
}

export interface EditarPlanoCobrancaModel {
    diario: PlanoDiarioModel;
    controlado: PlanoControladoModel;
    livre: PlanoLivreModel;
}

export interface EditarPlanoCobrancaResponseModel {
    diario: PlanoDiarioModel;
    controlado: PlanoControladoModel;
    livre: PlanoLivreModel;
}

export interface PlanoCobrancaModel {
    id: string;
    grupoVeiculosId: string;
    nomeGrupo: string;
    diario: PlanoDiarioModel;
    controlado: PlanoControladoModel;
    livre: PlanoLivreModel;
}

export interface SelecionarPlanosCobrancaResponseModel {
    registros: PlanoCobrancaModel[];
}

/** Opção do seletor de grupo de veículos no formulário. */
export interface GrupoOpcaoModel {
    id: string;
    nome: string;
}
