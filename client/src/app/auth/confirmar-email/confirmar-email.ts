import { Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { AuthService } from '../auth.service';
import { AvisoConfirmarEmail } from '../confirmacao/aviso-confirmar-email';

type Estado = 'confirmando' | 'confirmado' | 'erro';

/** Abre o link do e-mail (/auth/confirmar-email?email=&codigo=) e confirma a conta. */
@Component({
    selector: 'app-confirmar-email',
    imports: [MatButtonModule, MatCardModule, MatIconModule, RouterLink, AvisoConfirmarEmail],
    template: `
<section class="flex justify-center">
    <div class="w-full max-w-xl lg:max-w-md xl:max-w-sm py-10">
        <h2 class="text-2xl font-medium">Confirmação de e-mail</h2>
        <hr class="my-4" />

        <mat-card class="p-4">
            <mat-card-content>
                @switch (estado()) {
                @case ('confirmando') {
                <p class="py-6 text-center text-gray-600">Confirmando o seu e-mail…</p>
                }
                @case ('confirmado') {
                <div class="text-center">
                    <mat-icon class="!h-12 !w-12 !text-[48px] text-primary" aria-hidden="true">mark_email_read</mat-icon>
                    <p class="mt-3 text-lg font-medium">E-mail confirmado!</p>
                    <p class="mt-1 text-sm text-gray-600">Sua conta está ativa. Agora é só entrar.</p>
                    <a mat-flat-button routerLink="/auth/login" class="mt-4">Entrar</a>
                </div>
                }
                @case ('erro') {
                <div class="text-center">
                    <mat-icon class="!h-12 !w-12 !text-[48px] text-red-700" aria-hidden="true">error</mat-icon>
                    <p class="mt-3 text-lg font-medium">Não foi possível confirmar</p>
                    <p class="mt-1 text-sm text-gray-600">{{ mensagem() }}</p>
                </div>

                @if (email()) {
                <div class="mt-4">
                    <app-aviso-confirmar-email [email]="email()" titulo="Receber um novo link" [mostrarVoltar]="false" />
                </div>
                }
                <div class="mt-4 text-center">
                    <a mat-button routerLink="/auth/login">Ir para o login</a>
                </div>
                }
                }
            </mat-card-content>
        </mat-card>
    </div>
</section>
`,
})
export class ConfirmarEmail implements OnInit {
    private readonly route = inject(ActivatedRoute);
    private readonly authService = inject(AuthService);

    protected readonly estado = signal<Estado>('confirmando');
    protected readonly mensagem = signal('');
    protected readonly email = signal('');

    ngOnInit(): void {
        const consulta = this.route.snapshot.queryParamMap;
        const email = consulta.get('email') ?? '';
        const codigo = consulta.get('codigo') ?? '';

        this.email.set(email);

        if (!email || !codigo) {
            this.mensagem.set('O link está incompleto. Abra o link do e-mail de novo ou peça um novo.');
            this.estado.set('erro');

            return;
        }

        this.authService.confirmarEmail(email, codigo).subscribe({
            next: () => this.estado.set('confirmado'),
            error: (err) => {
                this.mensagem.set(mensagemDeErro(err, 'O link é inválido ou já expirou.'));
                this.estado.set('erro');
            },
        });
    }
}
