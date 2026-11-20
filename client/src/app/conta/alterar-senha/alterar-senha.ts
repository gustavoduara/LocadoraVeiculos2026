import { Component, inject, signal } from '@angular/core';
import {
    AbstractControl,
    FormBuilder,
    FormGroup,
    ReactiveFormsModule,
    ValidationErrors,
    Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import { map, take } from 'rxjs';

import { AuthService } from '../../auth/auth.service';
import { rotaInicial } from '../../auth/rota-inicial';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { BotaoVerSenha } from '../../shared/senha/botao-ver-senha';

function confirmacaoIgual(grupo: AbstractControl): ValidationErrors | null {
    return grupo.get('novaSenha')?.value === grupo.get('confirmarSenha')?.value ? null : { senhasDiferentes: true };
}

/** Qualquer pessoa logada (cliente, funcionário ou empresa) troca a própria senha. */
@Component({
    selector: 'app-alterar-senha',
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
    templateUrl: './alterar-senha.html',
})
export class AlterarSenha {
    private readonly formBuilder = inject(FormBuilder);
    private readonly router = inject(Router);
    private readonly authService = inject(AuthService);
    private readonly notificacaoService = inject(NotificacaoService);

    protected readonly enviando = signal(false);
    protected readonly voltarPara = signal('/inicio');

    protected readonly form: FormGroup = this.formBuilder.group(
        {
            senhaAtual: ['', [Validators.required]],
            novaSenha: ['', [Validators.required, Validators.minLength(6)]],
            confirmarSenha: ['', [Validators.required]],
        },
        { validators: [confirmacaoIgual] },
    );

    constructor() {
        this.authService
            .obterAccessToken()
            .pipe(
                take(1),
                map((token) => rotaInicial(token?.usuarioAutenticado.cargo)),
            )
            .subscribe((rota) => this.voltarPara.set(rota));
    }

    get senhaAtual() {
        return this.form.get('senhaAtual');
    }

    get novaSenha() {
        return this.form.get('novaSenha');
    }

    get confirmarSenha() {
        return this.form.get('confirmarSenha');
    }

    protected salvar(): void {
        if (this.form.invalid || this.enviando()) return;

        this.enviando.set(true);

        this.authService.alterarSenha(this.form.value).subscribe({
            next: () => {
                this.notificacaoService.sucesso('Senha alterada com sucesso!');
                this.router.navigate([this.voltarPara()]);
            },
            error: (err) => {
                this.enviando.set(false);
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível alterar a senha.'));
            },
        });
    }
}
