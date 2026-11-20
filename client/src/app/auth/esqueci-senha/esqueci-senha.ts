import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { AuthService } from '../auth.service';

/** Pede o e-mail com o link para criar uma nova senha. A resposta é a mesma exista ou não a conta. */
@Component({
    selector: 'app-esqueci-senha',
    imports: [
        MatCardModule,
        MatButtonModule,
        MatIconModule,
        MatFormFieldModule,
        MatInputModule,
        RouterLink,
        ReactiveFormsModule,
    ],
    templateUrl: './esqueci-senha.html',
})
export class EsqueciSenha {
    private readonly formBuilder = inject(FormBuilder);
    private readonly authService = inject(AuthService);
    private readonly notificacaoService = inject(NotificacaoService);

    protected readonly enviando = signal(false);
    protected readonly enviado = signal(false);

    protected readonly form: FormGroup = this.formBuilder.group({
        email: ['', [Validators.required, Validators.email]],
    });

    get email() {
        return this.form.get('email');
    }

    protected enviar(): void {
        if (this.form.invalid || this.enviando()) return;

        this.enviando.set(true);

        this.authService.esqueciSenha(this.form.value.email).subscribe({
            next: () => {
                this.enviado.set(true);
                this.enviando.set(false);
            },
            error: (err) => {
                this.enviando.set(false);
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível enviar o e-mail agora.'));
            },
        });
    }
}
