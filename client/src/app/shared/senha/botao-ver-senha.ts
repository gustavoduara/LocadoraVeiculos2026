import { Component, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

/**
 * Olho para mostrar ou esconder a senha digitada. Use dentro do mat-form-field, passando o input:
 * `<input #senha matInput type="password" ... /> <app-botao-ver-senha matSuffix [campo]="senha" />`
 */
@Component({
    selector: 'app-botao-ver-senha',
    imports: [MatButtonModule, MatIconModule],
    template: `
        <button
            mat-icon-button
            type="button"
            [attr.aria-label]="visivel() ? 'Ocultar senha' : 'Mostrar senha'"
            [attr.aria-pressed]="visivel()"
            (click)="alternar()"
        >
            <mat-icon>{{ visivel() ? 'visibility' : 'visibility_off' }}</mat-icon>
        </button>
    `,
})
export class BotaoVerSenha {
    /** O input de senha controlado por este botão. */
    public readonly campo = input.required<HTMLInputElement>();

    protected readonly visivel = signal(false);

    protected alternar(): void {
        const mostrar = !this.visivel();

        this.visivel.set(mostrar);
        this.campo().type = mostrar ? 'text' : 'password';
    }
}
