import { NgxMaskDirective } from 'ngx-mask';
import { Observer } from 'rxjs';

import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import {
    EditarMeuPerfilModel,
    EditarMeuPerfilResponseModel,
    MeuPerfilModel,
} from '../funcionario.models';
import { FuncionarioService } from '../funcionario.service';

@Component({
    selector: 'app-meu-perfil',
    imports: [
        MatCardModule,
        MatButtonModule,
        MatIconModule,
        MatFormFieldModule,
        MatInputModule,
        RouterLink,
        ReactiveFormsModule,
        NgxMaskDirective,
        CurrencyPipe,
        DatePipe,
    ],
    templateUrl: './meu-perfil.html',
})
export class MeuPerfil {
    protected readonly formBuilder = inject(FormBuilder);
    protected readonly router = inject(Router);
    protected readonly route = inject(ActivatedRoute);
    protected readonly funcionarioService = inject(FuncionarioService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected readonly perfil = this.route.snapshot.data['perfil'] as MeuPerfilModel;

    protected perfilForm: FormGroup = this.formBuilder.group({
        nomeCompleto: [
            this.perfil?.nomeCompleto ?? '',
            [Validators.required, Validators.minLength(3)],
        ],
        cpf: [
            this.perfil?.cpf ?? '',
            [Validators.required, Validators.pattern(/^\d{3}\.\d{3}\.\d{3}-\d{2}$/)],
        ],
    });

    get nomeCompleto() {
        return this.perfilForm.get('nomeCompleto');
    }

    get cpf() {
        return this.perfilForm.get('cpf');
    }

    public salvar() {
        if (this.perfilForm.invalid) return;

        const model: EditarMeuPerfilModel = this.perfilForm.value;

        const observer: Observer<EditarMeuPerfilResponseModel> = {
            next: () => this.notificacaoService.sucesso('Seu perfil foi atualizado com sucesso!'),
            error: (err) =>
                this.notificacaoService.erro(
                    mensagemDeErro(err, 'Não foi possível atualizar o seu perfil.'),
                ),
            complete: () => this.router.navigate(['/inicio']),
        };

        this.funcionarioService.editarMeuPerfil(model).subscribe(observer);
    }
}
