import { Subscription } from 'rxjs';

import {
    ChangeDetectionStrategy,
    Component,
    inject,
    Input,
    OnChanges,
    OnDestroy,
    signal,
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { CatalogoService } from '../catalogo.service';

/** Foto de um carro do catálogo (baixada com o token) ou um ícone quando o carro não tem foto. */
@Component({
    selector: 'app-foto-catalogo',
    imports: [MatIconModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        @if (url(); as endereco) {
        <img [src]="endereco" [alt]="descricao" class="w-full h-full object-cover" />
        } @else {
        <div class="w-full h-full flex items-center justify-center text-gray-400">
            <mat-icon class="!text-5xl !w-12 !h-12">directions_car</mat-icon>
        </div>
        }
    `,
    host: { class: 'block overflow-hidden bg-gray-100' },
})
export class FotoCatalogo implements OnChanges, OnDestroy {
    private readonly catalogoService = inject(CatalogoService);

    @Input({ required: true }) carroId!: string;
    @Input() temFoto = false;
    @Input() descricao = 'Foto do carro';

    protected readonly url = signal<string | null>(null);

    private assinatura?: Subscription;

    ngOnChanges(): void {
        this.limpar();

        if (!this.temFoto || !this.carroId) return;

        this.assinatura = this.catalogoService.obterFoto(this.carroId).subscribe({
            next: (blob) => this.url.set(URL.createObjectURL(blob)),
            error: () => this.url.set(null),
        });
    }

    ngOnDestroy(): void {
        this.limpar();
    }

    private limpar(): void {
        this.assinatura?.unsubscribe();

        const atual = this.url();

        if (atual) URL.revokeObjectURL(atual);

        this.url.set(null);
    }
}
