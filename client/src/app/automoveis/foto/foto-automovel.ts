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

import { AutomovelService } from '../automovel.service';

/** Mostra a foto de um automóvel (baixada com o token) ou um ícone quando não há foto. */
@Component({
    selector: 'app-foto-automovel',
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
export class FotoAutomovel implements OnChanges, OnDestroy {
    private readonly automovelService = inject(AutomovelService);

    @Input({ required: true }) automovelId!: string;
    @Input() temFoto = false;
    @Input() descricao = 'Foto do automóvel';

    /** Quando informado, mostra essa foto extra em vez da foto principal. */
    @Input() fotoId: string | null = null;

    /** Mude este valor para forçar o recarregamento (ex.: depois de trocar a foto). */
    @Input() versao = 0;

    protected readonly url = signal<string | null>(null);

    private assinatura?: Subscription;

    ngOnChanges(): void {
        this.limpar();

        if (!this.temFoto || !this.automovelId) return;

        const origem = this.fotoId
            ? this.automovelService.obterFotoExtra(this.automovelId, this.fotoId)
            : this.automovelService.obterFoto(this.automovelId);

        this.assinatura = origem.subscribe({
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
