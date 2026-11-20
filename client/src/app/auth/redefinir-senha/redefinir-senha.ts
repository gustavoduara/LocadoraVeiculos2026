import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { BotaoVerSenha } from '../../shared/senha/botao-ver-senha';
import { AuthService } from '../auth.service';
import { compararConfirmacaoSenha } from '../validators/comparar-confirmacao-senha-validator';

/** Tela aberta pelo link do e-mail (traz o e-mail e o código na URL): a pessoa cria a nova senha. */
@Component({
    selector: 'app-redefinir-senha',
    imports: [
        MatCardModule,
        MatButtonModule,
        MatIconModule,
        MatFormFieldModule,
        MatInputModule,
        RouterLink,
        ReactiveFormsModule,
        BotaoVerSenha,
    ],
    templateUrl: './redefinir-senha.html',
})
export class RedefinirSenha {
    private readonly formBuilder = inject(FormBuilder);
    private readonly router = inject(Router);
    private readonly authService = inject(AuthService);
    private readonly notificacaoService = inject(NotificacaoService);

    private readonly parametros = inject(ActivatedRoute).snapshot.queryParamMap;

    protected readonly email = this.parametros.get('email') ?? '';
    private readonly codigo = this.parametros.get('codigo') ?? '';

    /** Sem e-mail ou código na URL não há o que redefinir: pede um link novo. */
    protected readonly linkIncompleto = !this.email || !this.codigo;

    protected readonly enviando = signal(false);
    /** Mensagem do servidor quando o link é recusado (inválido, expirado ou já usado). */
    protected readonly erroDoLink = signal<string | null>(null);

    protected readonly form: FormGroup = this.formBuilder.group(
        {
            senha: ['', [Validators.required, Validators.minLength(6)]],
            confirmarSenha: ['', [Validators.required, Validators.minLength(6)]],
        },
        { validators: [compararConfirmacaoSenha] },
    );

    get senha() {
        return this.form.get('senha');
    }

    get confirmarSenha() {
        return this.form.get('confirmarSenha');
    }

    protected redefinir(): void {
        if (this.form.invalid || this.enviando()) return;

        this.enviando.set(true);
        this.erroDoLink.set(null);

        this.authService
            .redefinirSenha({
                email: this.email,
                codigo: this.codigo,
                novaSenha: this.form.value.senha,
                confirmarSenha: this.form.value.confirmarSenha,
            })
            .subscribe({
                next: () => {
                    this.notificacaoService.sucesso('Senha alterada! Entre com a nova senha.');
                    this.router.navigate(['/auth/login']);
                },
                error: (err) => {
                    this.enviando.set(false);

                    const mensagem = mensagemDeErro(err, 'Não foi possível redefinir a senha.');

                    // Link ruim: mostra o aviso na própria tela, com o atalho para pedir outro.
                    if (mensagem.includes('inválido ou já expirou')) this.erroDoLink.set(mensagem);
                    else this.notificacaoService.erro(mensagem);
                },
            });
    }
}
