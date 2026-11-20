import { addDays, format } from 'date-fns';
import { map } from 'rxjs';

import { AsyncPipe, CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';

import { AuthService } from '../auth/auth.service';

/** Carro de exemplo da vitrine (dados fictícios, só para visualizar o layout). */
interface CarroExemplo {
    nome: string;
    categoria: string;
    diaria: number;
    passageiros: number;
    cambio: 'Manual' | 'Automático';
    malas: number;
    cor: string;
}

const CATEGORIAS = ['Econômico', 'Compacto', 'Sedan', 'SUV', 'Utilitário'];

const CARROS_EXEMPLO: CarroExemplo[] = [
    { nome: 'Fiat Mobi', categoria: 'Econômico', diaria: 89, passageiros: 4, cambio: 'Manual', malas: 1, cor: 'from-emerald-400 to-emerald-600' },
    { nome: 'Chevrolet Onix', categoria: 'Econômico', diaria: 109, passageiros: 5, cambio: 'Manual', malas: 2, cor: 'from-teal-400 to-teal-600' },
    { nome: 'Hyundai HB20', categoria: 'Compacto', diaria: 129, passageiros: 5, cambio: 'Automático', malas: 2, cor: 'from-sky-400 to-sky-600' },
    { nome: 'Toyota Corolla', categoria: 'Sedan', diaria: 219, passageiros: 5, cambio: 'Automático', malas: 3, cor: 'from-indigo-400 to-indigo-600' },
    { nome: 'Jeep Renegade', categoria: 'SUV', diaria: 189, passageiros: 5, cambio: 'Automático', malas: 3, cor: 'from-orange-400 to-orange-600' },
    { nome: 'Fiat Toro', categoria: 'Utilitário', diaria: 249, passageiros: 5, cambio: 'Automático', malas: 4, cor: 'from-rose-400 to-rose-600' },
];

const FORMATO_ISO = 'yyyy-MM-dd';

@Component({
    selector: 'app-inicio',
    imports: [
        MatCardModule,
        MatButtonModule,
        MatIconModule,
        MatDividerModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatTooltipModule,
        AsyncPipe,
        CurrencyPipe,
        RouterLink,
    ],
    templateUrl: './inicio.html',
    styleUrl: './inicio.scss',
})
export class Inicio {
    protected readonly usuarioAutenticado$ = inject(AuthService)
        .obterAccessToken()
        .pipe(map((x) => x?.usuarioAutenticado));

    protected readonly categorias = CATEGORIAS;
    protected readonly hoje = format(new Date(), FORMATO_ISO);

    // Campos da pesquisa (datas em yyyy-MM-dd, o formato do input de data).
    protected readonly retirada = signal(this.hoje);
    protected readonly devolucao = signal(format(addDays(new Date(), 3), FORMATO_ISO));
    protected readonly categoria = signal('');

    /** Resultado da última pesquisa: os cards mostram o total do período pesquisado. */
    protected readonly pesquisa = signal<{ dias: number; categoria: string } | null>(null);

    protected readonly erro = computed(() => {
        if (!this.retirada() || !this.devolucao()) return 'Informe as duas datas.';
        if (this.retirada() < this.hoje) return 'A retirada não pode ser no passado.';
        if (this.devolucao() < this.retirada()) return 'A devolução não pode ser antes da retirada.';

        return null;
    });

    protected readonly carros = computed(() => {
        const filtro = this.pesquisa()?.categoria;

        return filtro ? CARROS_EXEMPLO.filter((c) => c.categoria === filtro) : CARROS_EXEMPLO;
    });

    protected atualizarRetirada(valor: string): void {
        this.retirada.set(valor);

        // Mantém a devolução depois da retirada.
        if (valor && this.devolucao() < valor) this.devolucao.set(valor);
    }

    protected pesquisar(): void {
        if (this.erro()) return;

        // Mesma regra do aluguel: diferença entre as datas, no mínimo 1 diária.
        const dias = Math.max(
            1,
            Math.round((Date.parse(this.devolucao()) - Date.parse(this.retirada())) / 86_400_000),
        );

        this.pesquisa.set({ dias, categoria: this.categoria() });
    }

    protected limpar(): void {
        this.pesquisa.set(null);
        this.categoria.set('');
    }
}
