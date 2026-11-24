import { computed, Injectable, signal } from '@angular/core';

/** Depois de quanto tempo sem resposta a tela avisa que o servidor está acordando. */
const LIMITE_PARA_AVISAR_MS = 4000;

/**
 * Sabe quando o servidor está demorando para responder. No plano gratuito da Azure o app e o banco "dormem" quando ficam
 * parados e a primeira visita espera até 1 minuto; sem aviso a tela parece travada.
 */
@Injectable({ providedIn: 'root' })
export class EsperaServidor {
    private readonly lentas = signal(0);

    /** Verdadeiro enquanto algum pedido está demorando (ou sendo tentado de novo). */
    public readonly acordando = computed(() => this.lentas() > 0);

    /** Marca o começo de um pedido e devolve a função que marca o fim. `jaLento` vale para tentativas repetidas. */
    public iniciar(jaLento = false): () => void {
        let lento = false;

        const marcarLento = () => {
            lento = true;
            this.lentas.update((n) => n + 1);
        };

        const relogio = jaLento ? undefined : setTimeout(marcarLento, LIMITE_PARA_AVISAR_MS);

        if (jaLento) marcarLento();

        return () => {
            if (relogio) clearTimeout(relogio);
            if (lento) this.lentas.update((n) => n - 1);
        };
    }
}
