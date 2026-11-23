import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { RegistroModel } from '../auth.models';
import { AuthService } from '../auth.service';
import { AvisoConfirmarEmail } from '../confirmacao/aviso-confirmar-email';
import { compararConfirmacaoSenha } from '../validators/comparar-confirmacao-senha-validator';
import { AvisoDemonstracao } from '../../shared/demonstracao/aviso-demonstracao';
import { BotaoVerSenha } from '../../shared/senha/botao-ver-senha';

@Component({
    selector: 'app-registro',
    imports: [
        AvisoDemonstracao,
        MatCardModule,
        MatCheckboxModule,
        MatButtonModule,
        MatIconModule,
        MatFormFieldModule,
        MatInputModule,
        RouterLink,
        ReactiveFormsModule,
        BotaoVerSenha,
        AvisoConfirmarEmail,
    ],
    templateUrl: './registro.html',
})
export class Registro {
    protected readonly formBuilder = inject(FormBuilder);
    protected readonly authService = inject(AuthService);
    protected readonly notificacaoService = inject(NotificacaoService);

    /** A mesma tela serve às duas contas: a rota /auth/registro-cliente marca `tipo: 'cliente'`. */
    protected readonly ehCliente = inject(ActivatedRoute).snapshot.data['tipo'] === 'cliente';

    /** Depois de criar a conta: o e-mail para onde o link de confirmação foi enviado. */
    protected readonly emailEnviado = signal<string | null>(null);
    protected readonly enviando = signal(false);

    protected readonly registroForm: FormGroup = this.formBuilder.group(
        {
            nomeCompleto: ['', [Validators.required, Validators.minLength(3)]],
            email: ['', [Validators.required, Validators.email]],
            senha: ['', [Validators.required, Validators.minLength(6)]],
            confirmarSenha: ['', [Validators.required, Validators.minLength(6)]],
            // Termos de Uso e Política de Privacidade: sem o aceite a conta não é criada.
            aceitouTermos: [false, [Validators.requiredTrue]],
        },
        { validators: [compararConfirmacaoSenha] },
    );

    get nomeCompleto() {
        return this.registroForm.get('nomeCompleto');
    }

    get email() {
        return this.registroForm.get('email');
    }

    get senha() {
        return this.registroForm.get('senha');
    }

    get confirmarSenha() {
        return this.registroForm.get('confirmarSenha');
    }

    public registro() {
        if (this.registroForm.invalid) return;

        if (this.enviando()) return;

        const registroModel: RegistroModel = this.registroForm.value;

        this.enviando.set(true);

        (this.ehCliente
            ? this.authService.registroCliente(registroModel)
            : this.authService.registro(registroModel)
        ).subscribe({
            next: (resposta) => {
                this.enviando.set(false);
                this.emailEnviado.set(resposta.email);
            },
            error: (err) => {
                this.enviando.set(false);
                this.notificacaoService.erro(mensagemDeErro(err, 'Não foi possível criar a conta.'));
            },
        });
    }
}