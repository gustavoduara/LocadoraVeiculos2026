import { defer, finalize, retry, throwError, timer } from 'rxjs';

import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { EsperaServidor } from './espera-servidor';

/** Tentativas extras e pausa entre elas: juntas cobrem o tempo de o banco acordar (até ~1 minuto). */
const TENTATIVAS_EXTRAS = 10;
const PAUSA_ENTRE_TENTATIVAS_MS = 6000;

/** Respostas de "servidor ainda não está pronto": sem rede, erro interno (banco pausado) ou gateway/serviço indisponível. */
function servidorIndisponivel(erro: unknown): boolean {
    return erro instanceof HttpErrorResponse && [0, 500, 502, 503, 504].includes(erro.status);
}

/**
 * Mostra o aviso "acordando o servidor" quando um pedido demora e tenta de novo sozinho. Só repete leituras (GET) e o login:
 * repetir um cadastro poderia criar o registro duas vezes.
 */
export const esperaInterceptor: HttpInterceptorFn = (req, next) => {
    const espera = inject(EsperaServidor);
    const podeRepetir = req.method === 'GET' || req.url.includes('/auth/autenticar');

    let tentativa = 0;

    return defer(() => {
        const terminar = espera.iniciar(tentativa > 0);
        tentativa++;

        return next(req).pipe(finalize(terminar));
    }).pipe(
        retry({
            count: podeRepetir ? TENTATIVAS_EXTRAS : 0,
            delay: (erro) => {
                if (!servidorIndisponivel(erro)) return throwError(() => erro);

                // Durante a pausa o aviso continua na tela.
                const terminarPausa = espera.iniciar(true);

                return timer(PAUSA_ENTRE_TENTATIVAS_MS).pipe(finalize(terminarPausa));
            },
        }),
    );
};
