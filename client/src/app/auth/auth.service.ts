import {
    BehaviorSubject, catchError, defer, distinctUntilChanged, Observable, of, shareReplay,
    switchMap, tap
} from 'rxjs';

import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import {
    AccessTokenModel,
    AlterarSenhaModel,
    LoginModel,
    RedefinirSenhaModel,
    RegistroModel,
    RegistroRespostaModel,
} from './auth.models';

@Injectable()
export class AuthService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = environment.apiUrl + '/auth';

    private readonly accessTokenSubject$ = new BehaviorSubject<AccessTokenModel | null>(null);

    private readonly inicializacao$ = defer(() =>
        this.rotacionar().pipe(catchError(() => of(null))),
    ).pipe(shareReplay({ bufferSize: 1, refCount: true }));

    public obterAccessToken(): Observable<AccessTokenModel | null> {
        return this.inicializacao$.pipe(
            switchMap(() => this.accessTokenSubject$.asObservable()),
            distinctUntilChanged((a, b) => a?.chave === b?.chave),
        );
    }

    /** Cria a conta da locadora. Ela só entra depois de confirmar o e-mail (por isso não devolve token). */
    public registro(registroModel: RegistroModel): Observable<RegistroRespostaModel> {
        return this.http.post<RegistroRespostaModel>(`${this.apiUrl}/registrar`, registroModel);
    }

    /** Conta de cliente: quem procura e aluga carros no catálogo (não é uma locadora). */
    public registroCliente(registroModel: RegistroModel): Observable<RegistroRespostaModel> {
        return this.http.post<RegistroRespostaModel>(`${this.apiUrl}/registrar-cliente`, registroModel);
    }

    /** Abre o link do e-mail e confirma o endereço (repetir não dá erro). */
    public confirmarEmail(email: string, codigo: string): Observable<null> {
        return this.http.post<null>(`${this.apiUrl}/confirmar-email`, { email, codigo });
    }

    /** Pede um novo e-mail de confirmação (a resposta é a mesma exista ou não a conta). */
    public reenviarConfirmacao(email: string): Observable<{ mensagem: string }> {
        return this.http.post<{ mensagem: string }>(`${this.apiUrl}/reenviar-confirmacao`, { email });
    }

    public login(loginModel: LoginModel): Observable<AccessTokenModel> {
        const urlCompleto = `${this.apiUrl}/autenticar`;

        return this.http
            .post<AccessTokenModel>(urlCompleto, loginModel)
            .pipe(tap((token) => this.accessTokenSubject$.next(token)));
    }

    /** Pede o e-mail com o link para criar uma nova senha (a resposta é a mesma exista ou não a conta). */
    public esqueciSenha(email: string): Observable<{ mensagem: string }> {
        return this.http.post<{ mensagem: string }>(`${this.apiUrl}/esqueci-senha`, { email });
    }

    public redefinirSenha(model: RedefinirSenhaModel): Observable<null> {
        return this.http.post<null>(`${this.apiUrl}/redefinir-senha`, model);
    }

    /** A pessoa logada troca a própria senha. */
    public alterarSenha(model: AlterarSenhaModel): Observable<null> {
        return this.http.post<null>(`${this.apiUrl}/alterar-senha`, model);
    }

    public rotacionar(): Observable<AccessTokenModel> {
        const urlCompleto = `${this.apiUrl}/rotacionar`;

        return this.http
            .post<AccessTokenModel>(urlCompleto, {})
            .pipe(tap((token) => this.accessTokenSubject$.next(token)));
    }

    public sair(): Observable<null> {
        const urlCompleto = `${this.apiUrl}/sair`;

        return this.http.post<null>(urlCompleto, {}).pipe(tap(() => this.revogarAccessToken()));
    }

    public revogarAccessToken(): void {
        return this.accessTokenSubject$.next(null);
    }



}