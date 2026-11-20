/** Para onde cada perfil vai depois de entrar: o cliente procura carros, a locadora gerencia. */
export function rotaInicial(cargo?: string | null): string {
    return cargo === 'Cliente' ? '/catalogo' : '/inicio';
}
