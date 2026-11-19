import { format, isValid, parse } from 'date-fns';

import { ClienteModel } from '../clientes/cliente.models';

export interface CondutorModel {
    id: string;
    clienteId: string;
    nomeCliente: string;
    nome: string;
    email: string;
    telefone: string;
    cpf: string;
    cnh: string;
    /** Data no formato ISO (yyyy-MM-dd). */
    validadeCnh: string;
    cnhVencida: boolean;
}

export interface SelecionarCondutoresResponseModel {
    registros: CondutorModel[];
}

export interface CadastrarCondutorModel {
    clienteId: string;
    nome: string;
    email: string;
    telefone: string;
    cpf: string;
    cnh: string;
    validadeCnh: string;
}

export interface CadastrarCondutorResponseModel {
    id: string;
}

/** O cliente do condutor não muda na edição. */
export type EditarCondutorModel = Omit<CadastrarCondutorModel, 'clienteId'>;

/** Cliente exibido no seletor do formulário (os dados extras servem ao "cliente é condutor"). */
export type ClienteOpcaoModel = Pick<ClienteModel, 'id' | 'tipo' | 'nome'> &
    Partial<Pick<ClienteModel, 'email' | 'telefone' | 'cpf' | 'cnh'>>;

/** Converte yyyy-MM-dd (API) em dd/MM/yyyy (tela), sem passar por fuso horário. */
export function isoParaDataBr(iso: string): string {
    const [ano, mes, dia] = iso.split('-');

    return ano && mes && dia ? `${dia}/${mes}/${ano}` : iso;
}

/** Converte dd/MM/yyyy (tela) em yyyy-MM-dd (API). Devolve null se a data não existir. */
export function dataBrParaIso(texto: string): string | null {
    if (!/^\d{2}\/\d{2}\/\d{4}$/.test(texto)) return null;

    const data = parse(texto, 'dd/MM/yyyy', new Date());

    return isValid(data) ? format(data, 'yyyy-MM-dd') : null;
}
