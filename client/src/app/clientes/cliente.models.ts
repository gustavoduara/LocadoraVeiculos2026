export type TipoClienteModel = 'PessoaFisica' | 'PessoaJuridica';

export const TIPOS_CLIENTE: { valor: TipoClienteModel; rotulo: string }[] = [
    { valor: 'PessoaFisica', rotulo: 'Pessoa Física' },
    { valor: 'PessoaJuridica', rotulo: 'Pessoa Jurídica' },
];

export function rotuloTipoCliente(valor: string): string {
    return TIPOS_CLIENTE.find((t) => t.valor === valor)?.rotulo ?? valor;
}

export const ESTADOS: string[] = [
    'AC', 'AL', 'AP', 'AM', 'BA', 'CE', 'DF', 'ES', 'GO', 'MA', 'MT', 'MS', 'MG', 'PA',
    'PB', 'PR', 'PE', 'PI', 'RJ', 'RN', 'RS', 'RO', 'RR', 'SC', 'SP', 'SE', 'TO',
];

export interface ClienteModel {
    id: string;
    tipo: TipoClienteModel;
    nome: string;
    email: string;
    telefone: string;
    cpf: string | null;
    rg: string | null;
    cnh: string | null;
    cnpj: string | null;
    estado: string;
    cidade: string;
    bairro: string;
    rua: string;
    numero: string;
    pessoaJuridicaId: string | null;
    nomePessoaJuridica: string | null;
    /** Veio de uma reserva online: tem conta própria e a locadora só consulta. */
    contaOnline?: boolean;
}

export interface SelecionarClientesResponseModel {
    registros: ClienteModel[];
}

/** Cpf, Rg, Cnh e pessoaJuridicaId valem para pessoa física; cnpj para pessoa jurídica. */
export interface CadastrarClienteModel {
    tipo: TipoClienteModel;
    nome: string;
    email: string;
    telefone: string;
    cpf: string | null;
    rg: string | null;
    cnh: string | null;
    cnpj: string | null;
    estado: string;
    cidade: string;
    bairro: string;
    rua: string;
    numero: string;
    pessoaJuridicaId: string | null;
}

export interface CadastrarClienteResponseModel {
    id: string;
}

/** Na edição o tipo não muda; a API usa o tipo do registro. */
export type EditarClienteModel = Omit<CadastrarClienteModel, 'tipo'>;
