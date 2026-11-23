export type TipoCombustivelModel = 'Gasolina' | 'Gas' | 'Diesel' | 'Alcool';

export const COMBUSTIVEIS: { valor: TipoCombustivelModel; rotulo: string }[] = [
    { valor: 'Gasolina', rotulo: 'Gasolina' },
    { valor: 'Gas', rotulo: 'GNV (gás natural)' },
    { valor: 'Diesel', rotulo: 'Diesel' },
    { valor: 'Alcool', rotulo: 'Álcool' },
];

export function rotuloCombustivel(valor: string): string {
    return COMBUSTIVEIS.find((c) => c.valor === valor)?.rotulo ?? valor;
}

/** A API guarda a placa sem hífen (ABC1D23); na tela ela aparece como ABC-1D23. */
export function formatarPlaca(placa: string | null | undefined): string {
    if (!placa) return '';

    return placa.length === 7 ? `${placa.slice(0, 3)}-${placa.slice(3)}` : placa;
}

export interface AutomovelModel {
    id: string;
    placa: string;
    marca: string;
    modelo: string;
    cor: string;
    ano: number;
    tipoCombustivel: TipoCombustivelModel;
    capacidadeCombustivelLitros: number;
    grupoVeiculosId: string;
    nomeGrupo: string;
    temFoto: boolean;
    nomeArquivoFoto: string | null;
    /** Valor por dia do seguro deste carro. Zero = o carro não oferece seguro. */
    seguroDiaria: number;
}

export interface SelecionarAutomoveisResponseModel {
    registros: AutomovelModel[];
}

export interface CadastrarAutomovelModel {
    placa: string;
    marca: string;
    modelo: string;
    cor: string;
    ano: number;
    tipoCombustivel: TipoCombustivelModel;
    capacidadeCombustivelLitros: number;
    grupoVeiculosId: string;
    seguroDiaria: number;
}

export interface CadastrarAutomovelResponseModel {
    id: string;
}

export type EditarAutomovelModel = CadastrarAutomovelModel;

export type EditarAutomovelResponseModel = CadastrarAutomovelModel;

export interface EnviarFotoResponseModel {
    nomeArquivo: string;
}

/** O que o usuário fez com a foto no formulário. */
export interface FotoSelecionadaModel {
    acao: 'nenhuma' | 'enviar' | 'remover';
    arquivo?: File;
}

/** Foto extra já salva de um carro (o carrossel do catálogo mostra a capa e depois estas). */
export interface FotoExtraModel {
    id: string;
    nomeArquivo: string;
}

/** Quantas fotos extras um carro pode ter, além da foto principal. */
export const MAXIMO_DE_FOTOS_EXTRAS = 5;

/** O que o usuário fez com as fotos extras no formulário: arquivos novos para enviar e ids já salvos para remover. */
export interface FotosExtrasSelecionadasModel {
    novas: File[];
    removidas: string[];
}

export interface SalvarAutomovelEvento {
    dados: CadastrarAutomovelModel;
    foto: FotoSelecionadaModel;
    fotosExtras: FotosExtrasSelecionadasModel;
}

/** Opção do seletor de grupo de veículos. */
export interface GrupoOpcaoModel {
    id: string;
    nome: string;
}
