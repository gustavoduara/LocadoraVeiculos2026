import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import { catchError, map, of, take } from 'rxjs';

import { UsuarioAutenticadoModel } from '../../auth/auth.models';
import { AuthService } from '../../auth/auth.service';
import { mensagemDeErro } from '../../shared/erros/mensagem-erro';
import { NotificacaoService } from '../../shared/notificacao/notificacao.service';
import { BotaoVerSenha } from '../../shared/senha/botao-ver-senha';
import { ContaService } from '../conta.service';

/**
 * Exclusão da própria conta (LGPD).
 * Cliente: acontece na hora (senha + a palavra EXCLUIR). Locadora: o administrador agenda para 7 dias depois
 * (senha + nome da empresa) e pode cancelar até lá.
 */
@Component({
    selector: 'app-excluir-conta',
    imports: [
        ReactiveFormsModule,
        MatButtonModule,
        MatCardModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        RouterLink,
        BotaoVerSenha,
    ],
    template: `
<section class="mx-auto max-w-2xl px-4 py-6">
    <h2 class="text-2xl font-semibold">Excluir minha conta</h2>
    <hr class="my-4 border-muted/30" />

    @if (usuario(); as u) {
    <mat-card class="p-4 rounded-2xl shadow-sm">
        <mat-card-content>
            @if (u.cargo === 'Cliente') {
            <h3 class="text-base font-medium">O que acontece</h3>
            <ul class="mt-2 list-disc space-y-1 pl-5 text-sm">
                <li>A sua conta é excluída <strong>na hora</strong> e você sai do sistema.</li>
                <li>Os seus dados de "Meus dados" (CPF, RG, CNH, telefone e endereço) são apagados.</li>
                <li>Pedidos de reserva que ainda esperam resposta são cancelados.</li>
                <li>
                    O histórico que as locadoras guardam das suas reservas fica sem os seus dados pessoais
                    (nome e documentos viram "Conta excluída").
                </li>
                <li>Você pode criar uma conta nova depois, até com o mesmo e-mail.</li>
                <li><strong>Não dá para desfazer.</strong></li>
            </ul>
            <p class="mt-3 text-sm text-gray-600">
                Se você tiver um aluguel em aberto, conclua-o antes: a exclusão fica bloqueada até lá.
            </p>
            } @else {
            @if (agendadaPara(); as data) {
            <div class="rounded-lg px-4 py-3 selo-ambar">
                <p class="font-medium">A exclusão desta locadora está agendada para {{ dataBr(data) }}.</p>
                <p class="mt-1 text-sm">
                    Até lá você pode continuar usando o sistema normalmente. Se mudou de ideia, cancele abaixo.
                </p>
            </div>

            <div class="mt-4 flex justify-end">
                <button mat-flat-button type="button" [disabled]="enviando()" (click)="cancelar()">
                    <mat-icon>undo</mat-icon>
                    Cancelar exclusão
                </button>
            </div>
            } @else {
            <h3 class="text-base font-medium">O que acontece</h3>
            <ul class="mt-2 list-disc space-y-1 pl-5 text-sm">
                <li>A exclusão fica <strong>agendada para 7 dias depois</strong>. Até lá você pode cancelar.</li>
                <li>
                    No dia, os dados da locadora são removidos: frota, aluguéis, preços, cupons e reservas.
                </li>
                <li>As contas de todos os <strong>funcionários</strong> são encerradas.</li>
                <li>
                    Os dados pessoais de clientes, condutores e funcionários são anonimizados
                    (nome e documentos viram "Conta excluída").
                </li>
                <li>Os carros saem do catálogo dos clientes.</li>
                <li><strong>Depois do prazo não dá para desfazer.</strong></li>
            </ul>
            <p class="mt-3 text-sm text-gray-600">
                O pedido é bloqueado se houver aluguéis em aberto ou reservas esperando resposta.
            </p>
            }
            }

            @if (u.cargo === 'Cliente' || !agendadaPara()) {
            <form class="mt-6 space-y-2" [formGroup]="form" (ngSubmit)="enviar()">
                <mat-form-field class="w-full">
                    <mat-label>Sua senha</mat-label>
                    <input #campoSenha matInput formControlName="senha" type="password" autocomplete="current-password" />
                    <app-botao-ver-senha matSuffix [campo]="campoSenha" />
                    @if (form.get('senha')?.touched && form.get('senha')?.errors?.['required']) {
                    <mat-error>Informe a senha.</mat-error>
                    }
                </mat-form-field>

                <mat-form-field class="w-full">
                    <mat-label>{{ u.cargo === 'Cliente' ? 'Digite EXCLUIR para confirmar' : 'Digite o nome da locadora: ' + u.nomeCompleto }}</mat-label>
                    <input matInput formControlName="confirmacao" autocomplete="off" />
                </mat-form-field>

                <div class="flex items-center justify-end gap-3 pt-2">
                    <a mat-stroked-button [routerLink]="voltarPara()">Voltar</a>

                    <button type="submit" mat-flat-button class="botao-perigo px-6" [disabled]="!podeEnviar() || enviando()">
                        <mat-icon>delete_forever</mat-icon>
                        {{ u.cargo === 'Cliente' ? 'Excluir minha conta' : 'Agendar exclusão' }}
                    </button>
                </div>
            </form>
            }
        </mat-card-content>
    </mat-card>
    }
</section>
`,
})
export class ExcluirConta implements OnInit {
    private readonly formBuilder = inject(FormBuilder);
    private readonly authService = inject(AuthService);
    private readonly contaService = inject(ContaService);
    private readonly notificacao = inject(NotificacaoService);
    private readonly router = inject(Router);

    protected readonly usuario = signal<UsuarioAutenticadoModel | null>(null);
    protected readonly agendadaPara = signal<string | null>(null);
    protected readonly enviando = signal(false);

    protected readonly form = this.formBuilder.group({
        senha: ['', [Validators.required]],
        confirmacao: ['', [Validators.required]],
    });

    private readonly confirmacao = signal('');
    private readonly senha = signal('');

    constructor() {
        this.form.valueChanges.subscribe((v) => {
            this.confirmacao.set(v.confirmacao ?? '');
            this.senha.set(v.senha ?? '');
        });
    }

    /** O botão só libera depois de digitar a palavra (cliente) ou o nome da locadora (administrador). */
    protected readonly podeEnviar = computed(() => {
        const u = this.usuario();
        const digitado = this.confirmacao().trim().toLowerCase();

        if (!u || !this.senha()) return false;

        return u.cargo === 'Cliente' ? digitado === 'excluir' : digitado === u.nomeCompleto.trim().toLowerCase();
    });

    protected readonly voltarPara = computed(() => (this.usuario()?.cargo === 'Cliente' ? '/catalogo' : '/inicio'));

    ngOnInit(): void {
        this.authService
            .obterAccessToken()
            .pipe(take(1))
            .subscribe((token) => this.usuario.set(token?.usuarioAutenticado ?? null));

        this.contaService
            .situacao()
            .pipe(
                map((s) => s.exclusaoAgendadaParaUtc),
                catchError(() => of(null)),
            )
            .subscribe((data) => this.agendadaPara.set(data));
    }

    protected dataBr(iso: string): string {
        return new Date(iso).toLocaleDateString('pt-BR');
    }

    protected enviar(): void {
        const u = this.usuario();

        if (!u || !this.podeEnviar() || this.enviando()) return;

        const { senha, confirmacao } = this.form.getRawValue();

        this.enviando.set(true);

        if (u.cargo === 'Cliente') {
            this.contaService.excluir(senha!, confirmacao!).subscribe({
                next: () => this.depoisDeExcluir(),
                error: (err) => this.falhar(err, 'Não foi possível excluir a conta.'),
            });

            return;
        }

        this.contaService.agendarExclusao(senha!, confirmacao!).subscribe({
            next: (s) => {
                this.enviando.set(false);
                this.agendadaPara.set(s.exclusaoAgendadaParaUtc);
                this.form.reset();
                this.notificacao.sucesso('Exclusão agendada. Enviamos os detalhes por e-mail.');
            },
            error: (err) => this.falhar(err, 'Não foi possível agendar a exclusão.'),
        });
    }

    protected cancelar(): void {
        this.enviando.set(true);

        this.contaService.cancelarExclusao().subscribe({
            next: () => {
                this.enviando.set(false);
                this.agendadaPara.set(null);
                this.notificacao.sucesso('Exclusão cancelada. Nada será removido.');
            },
            error: (err) => this.falhar(err, 'Não foi possível cancelar a exclusão.'),
        });
    }

    /** A conta já não existe: encerra a sessão deste navegador e volta para o login. */
    private depoisDeExcluir(): void {
        this.notificacao.sucesso('Sua conta foi excluída. Obrigado por usar o SerraDrive.');

        this.authService.sair().subscribe({
            error: () => {
                this.authService.revogarAccessToken();
                this.router.navigate(['/auth', 'login']);
            },
            complete: () => this.router.navigate(['/auth', 'login']),
        });
    }

    private falhar(err: unknown, padrao: string): void {
        this.enviando.set(false);
        this.notificacao.erro(mensagemDeErro(err, padrao));
    }
}
