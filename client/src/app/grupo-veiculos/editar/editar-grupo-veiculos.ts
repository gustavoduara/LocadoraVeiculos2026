import { filter, map, Observer, shareReplay, switchMap, take, tap } from 'rxjs';

import { AsyncPipe } from '@angular/common';
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
    DetalhesGrupoVeiculosModel,
    EditarGrupoVeiculosModel,
    EditarGrupoVeiculosResponseModel,
} from '../grupo-veiculos.models';
import { GrupoVeiculosService } from '../grupo-veiculos.service';

@Component({
    selector: 'app-editar-grupo-veiculos',
    imports: [
        MatCardModule,
        MatButtonModule,
        MatIconModule,
        MatFormFieldModule,
        MatInputModule,
        RouterLink,
        ReactiveFormsModule,
        AsyncPipe,
    ],
    templateUrl: './editar-grupo-veiculos.html',
})
export class EditarGrupoVeiculos {
    protected readonly formBuilder = inject(FormBuilder);
    protected readonly router = inject(Router);
    protected readonly route = inject(ActivatedRoute);
    protected readonly grupoService = inject(GrupoVeiculosService);
    protected readonly notificacaoService = inject(NotificacaoService);

    protected grupoForm: FormGroup = this.formBuilder.group({
        nome: ['', [Validators.required, Validators.minLength(3)]],
    });

    get nome() {
        return this.grupoForm.get('nome');
    }

    protected readonly grupo$ = this.route.data.pipe(
        filter((data) => data['grupo']),
        map((data) => data['grupo'] as DetalhesGrupoVeiculosModel),
        tap((grupo) => this.grupoForm.patchValue(grupo)),
        shareReplay({ bufferSize: 1, refCount: true }),
    );

    public editar() {
        if (this.grupoForm.invalid) return;

        const model: EditarGrupoVeiculosModel = this.grupoForm.value;

        const observer: Observer<EditarGrupoVeiculosResponseModel> = {
            next: () =>
                this.notificacaoService.sucesso(`O grupo "${model.nome}" foi editado com sucesso!`),
            error: (err) =>
                this.notificacaoService.erro(
                    mensagemDeErro(err, 'Não foi possível editar o grupo de veículos.'),
                ),
            complete: () => this.router.navigate(['/grupos-veiculos']),
        };

        this.grupo$
            .pipe(
                take(1),
                switchMap((grupo) => this.grupoService.editar(grupo.id, model)),
            )
            .subscribe(observer);
    }
}
