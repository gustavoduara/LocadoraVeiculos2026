import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { BotaoVerSenha } from '../../shared/senha/botao-ver-senha';
import { LoginModel, TipoDeAcessoModel } from '../auth.models';
import { AuthService } from '../auth.service';
import { rotaInicial } from '../rota-inicial';

const CHAVE_TIPO = 'locadora.tipo-de-login';

/** As duas portas de entrada: o cliente procura carros, a locadora (empresa e funcionários) gerencia. */
const OPCOES: { tipo: TipoDeAcessoModel; titulo: string; descricao: string; icone: string }[] = [
    {
        tipo: 'Cliente',
        titulo: 'Sou cliente',
        descricao: 'Quero procurar e alugar um carro',
        icone: 'directions_car',
    },
    {
        tipo: 'Locadora',
        titulo: 'Sou locadora',
        descricao: 'Empresa ou funcionário de uma locadora',
        icone: 'storefront',
    },
];

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

    protected readonly opcoes = OPCOES;

    /** Lembra a última escolha neste navegador; quem nunca escolheu começa como cliente. */
    protected readonly tipo = signal<TipoDeAcessoModel>(this.lerTipoSalvo());

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

    protected escolherTipo(tipo: TipoDeAcessoModel): void {
        this.tipo.set(tipo);

        try {
            localStorage.setItem(CHAVE_TIPO, tipo);
        } catch {
            // Sem armazenamento (janela anônima): a escolha vale só nesta tela.
        }
    }

    public login() {
        if (this.loginForm.invalid) return;

        const loginModel: LoginModel = { ...this.loginForm.value, tipo: this.tipo() };

        this.authService.login(loginModel).subscribe({
            next: (token) => {
                this.notificacaoService.sucesso('Login realizado com sucesso!');
                this.router.navigate([rotaInicial(token.usuarioAutenticado.cargo)]);
            },
            error: (err) => {
                const mensagem = mensagemDeErro(err, 'Falha ao autenticar. Verifique suas credenciais.');
                this.notificacaoService.erro(mensagem);
            },
        });
    }

    private lerTipoSalvo(): TipoDeAcessoModel {
        try {
            return localStorage.getItem(CHAVE_TIPO) === 'Locadora' ? 'Locadora' : 'Cliente';
        } catch {
            return 'Cliente';
        }
    }
}
