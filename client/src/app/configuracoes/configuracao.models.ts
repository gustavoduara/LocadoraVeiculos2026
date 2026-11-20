export interface ConfiguracaoModel {
    precoGasolina: number;
    precoGas: number;
    precoDiesel: number;
    precoAlcool: number;
    seguroClienteDiaria: number;
    seguroTerceirosDiaria: number;
}

export interface DadosDaLocadoraModel {
    nomeFantasia: string | null;
    telefone: string | null;
}
