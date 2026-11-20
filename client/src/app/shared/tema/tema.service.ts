import { effect, Injectable, signal } from '@angular/core';

const CHAVE = 'locadora.modo-noturno';
const CLASSE = 'modo-noturno';

/** Guarda a preferência de modo noturno (por navegador) e aplica a classe no <html>. */
@Injectable({ providedIn: 'root' })
export class TemaService {
    public readonly noturno = signal(this.lerPreferencia());

    constructor() {
        effect(() => {
            const ativo = this.noturno();

            document.documentElement.classList.toggle(CLASSE, ativo);

            try {
                localStorage.setItem(CHAVE, ativo ? '1' : '0');
            } catch {
                // Sem armazenamento (janela anônima, por exemplo): a preferência vale só nesta sessão.
            }
        });
    }

    public alternar(): void {
        this.noturno.update((v) => !v);
    }

    public definir(ativo: boolean): void {
        this.noturno.set(ativo);
    }

    private lerPreferencia(): boolean {
        try {
            return localStorage.getItem(CHAVE) === '1';
        } catch {
            return false;
        }
    }
}
