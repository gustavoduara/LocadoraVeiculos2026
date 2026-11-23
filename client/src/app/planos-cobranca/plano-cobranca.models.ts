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
    automovelId: string;
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
    automovelId: string;
    /** "Marca Modelo" do carro dono do plano. */
    nomeAutomovel: string;
    placa: string;
    diario: PlanoDiarioModel;
    controlado: PlanoControladoModel;
    livre: PlanoLivreModel;
}

export interface SelecionarPlanosCobrancaResponseModel {
    registros: PlanoCobrancaModel[];
}

/** Opção do seletor de automóvel no formulário ("Fiat Palio · ABC-1D23"). */
export interface AutomovelOpcaoModel {
    id: string;
    nome: string;
}
