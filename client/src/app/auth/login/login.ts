import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';

import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { LoginModel } from '../auth.models';
import { AuthService } from '../auth.service';
import { BotaoVerSenha } from '../../shared/senha/botao-ver-senha';
import { rotaInicial } from '../rota-inicial';

@Component({
    selector: 'app-login',
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
    templateUrl: './login.html',
})
export class Login {
    protected readonly formBuilder = inject(FormBuilder);
    protected readonly router = inject(Router);
    protected readonly authService = inject(AuthService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected loginForm: FormGroup = this.formBuilder.group({
        email: ['', [Validators.required, Validators.email]],
        senha: ['', [Validators.required, Validators.minLength(6)]],
    });

    get email() {
        return this.loginForm.get('email');
    }

    get senha() {
        return this.loginForm.get('senha');
    }

    public login() {
        if (this.loginForm.invalid) return;

        const loginModel: LoginModel = this.loginForm.value;

        this.authService.login(loginModel).subscribe({
            next: (token) => {
                this.notificacaoService.sucesso('Login realizado com sucesso!');
                this.router.navigate([rotaInicial(token.usuarioAutenticado.cargo)]);
            },
            error: (err) => {
                const mensagem = mensagemDeErro(err, 'Falha ao autenticar. Verifique suas credenciais.');
                this.notificacaoService.erro(mensagem);
            }
        });
    }
}