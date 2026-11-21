import { Component, DestroyRef, inject, input, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { AuthService } from '../auth.service';

/**
 * "Confirme seu e-mail": explica o que fazer e deixa pedir um novo e-mail.
 * Depois do cadastro o e-mail acabou de ser enviado (espera de 60 s para reenviar); no login, quando a
 * conta ainda não foi confirmada, o reenvio fica liberado na hora.
 */
@Component({
    selector: 'app-aviso-confirmar-email',
    imports: [MatButtonModule, MatIconModule, RouterLink],
    template: `
        <div class="text-center">
            <mat-icon class="!h-12 !w-12 !text-[48px] text-primary" aria-hidden="true">mark_email_unread</mat-icon>
            <p class="mt-3 text-lg font-medium">{{ titulo() }}</p>
            <p class="mt-1 text-sm text-gray-600">
                Enviamos um link para <strong>{{ email() }}</strong>. Abra o e-mail e clique no link para ativar a sua
                conta. Ele vale por 24 horas.
            </p>
            <p class="mt-1 text-sm text-gray-600">Não chegou? Veja a caixa de spam ou peça um novo e-mail.</p>
        </div>

        <div class="mt-4 flex flex-wrap justify-center gap-3">
            <button mat-stroked-button type="button" [disabled]="espera() > 0 || enviando()" (click)="reenviar()">
                <mat-icon>refresh</mat-icon>
                {{ enviando() ? 'Enviando...' : espera() > 0 ? 'Reenviar em ' + espera() + 's' : 'Reenviar e-mail' }}
            </button>

            @if (mostrarVoltar()) {
            <a mat-flat-button routerLink="/auth/login">Ir para o login</a>
            }
        </div>
    `,
})
export class AvisoConfirmarEmail implements OnInit {
    private readonly authService = inject(AuthService);
    private readonly notificacao = inject(NotificacaoService);

    readonly email = input.required<string>();
    readonly titulo = input('Confirme seu e-mail');

    /** Segundos de espera antes de poder reenviar (60 logo depois do cadastro, 0 no login). */
    readonly esperaInicial = input(0);
    readonly mostrarVoltar = input(true);

    protected readonly espera = signal(0);
    protected readonly enviando = signal(false);

    private temporizador?: ReturnType<typeof setInterval>;

    constructor() {
        inject(DestroyRef).onDestroy(() => clearInterval(this.temporizador));
    }

    ngOnInit(): void {
        if (this.esperaInicial() > 0) this.iniciarEspera(this.esperaInicial());
    }

    private iniciarEspera(segundos: number): void {
        clearInterval(this.temporizador);
        this.espera.set(segundos);

        this.temporizador = setInterval(() => {
            this.espera.update((s) => Math.max(0, s - 1));

            if (this.espera() === 0) clearInterval(this.temporizador);
        }, 1000);
    }

    protected reenviar(): void {
        if (this.enviando() || this.espera() > 0) return;

        this.enviando.set(true);

        this.authService.reenviarConfirmacao(this.email()).subscribe({
            next: () => {
                this.enviando.set(false);
                this.notificacao.sucesso('Enviamos o link de novo. Veja também o spam.');
                this.iniciarEspera(60);
            },
            error: (err) => {
                this.enviando.set(false);
                this.notificacao.erro(mensagemDeErro(err, 'Não foi possível enviar o e-mail agora.'));
            },
        });
    }
}
