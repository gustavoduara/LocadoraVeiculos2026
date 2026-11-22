/** Por qual lado a pessoa conversa: o cliente (fala com a locadora) ou a locadora (administrador e funcionários). */
export type LadoChat = 'cliente' | 'locadora';

export interface MensagemModel {
    id: string;
    conversaId: string;
    autor: 'Cliente' | 'Locadora';
    autorNome: string;
    texto: string;
    enviadaEmUtc: string;
    automovelId: string | null;
    descricaoVeiculo: string | null;
}

export interface ConversaResumoModel {
    id: string;
    titulo: string;
    ultimaMensagem: string;
    ultimaMensagemEmUtc: string;
    naoLidas: number;
}

export interface ConversaDetalheModel {
    id: string;
    titulo: string;
    mensagens: MensagemModel[];
}

/** O que o servidor empurra pelo canal em tempo real quando chega uma mensagem. */
export interface MensagemRecebidaEvento {
    conversaId: string;
    mensagem: MensagemModel;
    clienteNome: string;
    nomeLocadora: string;
    ultimaMensagem: string;
}

export const TAMANHO_MAXIMO_MENSAGEM = 1000;
