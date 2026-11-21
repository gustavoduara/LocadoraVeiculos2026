export interface RegistroModel {
    nomeCompleto: string;
    email: string;
    senha: string;
    confirmarSenha: string;
}

/** Resposta do cadastro: a conta ainda não entra, a pessoa precisa confirmar o e-mail. */
export interface RegistroRespostaModel {
    email: string;
    mensagem: string;
}

/** O lado por onde a pessoa diz que quer entrar: cliente (procura carros) ou locadora (empresa e funcionários). */
export type TipoDeAcessoModel = 'Cliente' | 'Locadora';

export interface LoginModel {
    email: string;
    senha: string;
    tipo?: TipoDeAcessoModel;
}

export interface RedefinirSenhaModel {
    email: string;
    codigo: string;
    novaSenha: string;
    confirmarSenha: string;
}

export interface AlterarSenhaModel {
    senhaAtual: string;
    novaSenha: string;
    confirmarSenha: string;
}

export interface AccessTokenModel {
    chave: string;
    expiracao: string;
    usuarioAutenticado: UsuarioAutenticadoModel;
}

export interface UsuarioAutenticadoModel {
    id: string;
    nomeCompleto: string;
    email: string;
    cargo: string;
}