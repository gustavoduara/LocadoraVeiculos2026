# LocadoraVeiculos2026

Sistema de locadora de veículos. Monorepo: API .NET (`Server/`) + SPA Angular (`client/`). Código e commits em português.

## Arquitetura

### Server (.NET 8, solution `Server/LocadoraVeiculos2026.sln`)
Camadas (pastas ≠ nome dos projetos/namespaces: pastas `LocadoraVeiculos.*`, namespaces `LocadoraDeVeiculos.*`):

| Pasta | Papel |
|---|---|
| `LocadoraVeiculos2026/` | WebApi (Program.cs, Controllers, `Models/` = requests, `Config/` = CORS, Identity/JWT, Swagger, migrations auto) |
| `LocadoraVeiculos.Aplicacao/` | CQRS com MediatR: `Modulo*/Commands`, `Handlers`, `Validators` (FluentValidation), resultados via FluentResults |
| `LocadoraVeiculos.Dominio/` | Entidades (`EntidadeBase`), Identity (`Usuario`, `Cargo`, tokens), `ITenantProvider` |
| `LocadoraVeiculos.Infraestrutura.Orm/` | EF Core 8 + SQL Server: `AppDbContext`, repositórios, mapeadores, `Migrations/` |
| `LocadoraDeVeiculos.Infraestrutura.jwt/` | `AccessTokenProvider`, `RefreshTokenProvider` |

- Módulos existentes: Autenticação, Funcionário, GrupoVeiculos. Novo módulo = Dominio entidade → Orm mapeador+repositório (+registrar em `DependencyInjection` do Orm) → Aplicacao commands/handlers/validators → Controller + Models → migration.
- Rotas: `api/auth` (autenticar, registrar, rotacionar, sair), `api/funcionarios`, `api/grupos-veiculos`. Autorização por cargo (`Empresa`, `Funcionario`).
- Multi-tenant: filtro global `EmpresaId == tenant && !Excluido` em `AppDbContext` (soft delete); tenant vem da claim `EmpresaId` (`IdentityTenantProvider`).
- JWT: token valida `jti` contra `Usuario.AccessTokenVersionId` (revogação); refresh token em cookie (`RefreshTokenCookieService`).
- Em `Development` a API roda `Database.Migrate()` ao subir e expõe Swagger em `/swagger`.

### client (Angular 20, standalone, Material + Tailwind 4, ngx-mask)
`client/src/app/`: `auth/` (login, registro, interceptor, service), `funcionarios/` (CRUD: listar/cadastrar/editar/excluir), `shared/` (shell, notificação), `inicio/`. Ainda **não há** tela de grupos de veículos.
Ambientes: `src/environments/environment(.development).ts` (`apiUrl`).

## Configuração local (user-secrets do projeto WebApi, `UserSecretsId` b49f88a1-…)
A API **não sobe** sem estas chaves (lidas via `IConfiguration`): `SQL_CONNECTION_STRING`, `JWT_GENERATION_KEY`, `JWT_AUDIENCE_DOMAIN`, `CORS_ALLOWED_ORIGINS`, `LUCKYPENNY_LICENSE_KEY` (licença do MediatR 14). Hoje todas estão definidas na máquina do dev.
- Banco: LocalDB `(localdb)\mssqllocaldb`, database `LocadoraVeiculos2026DB`, Integrated Security.
- Listar: `dotnet user-secrets list --project Server/LocadoraVeiculos2026` — **não colar a saída em respostas/commits** (contém chave JWT e licença).
- Portas: API `http://localhost:9050` (e `https://localhost:9099` quando o perfil https é usado); front `http://localhost:4200`.

## Comandos (rodar da raiz do repo; shell = PowerShell/Git Bash no Windows)

```bash
# API
dotnet build Server/LocadoraVeiculos2026.sln
dotnet run --project Server/LocadoraVeiculos2026 --launch-profile "https [Dev]"
# Migrations (dotnet-ef global 10.x)
dotnet ef migrations add NomeDaMigration -p Server/LocadoraVeiculos.Infraestrutura.Orm -s Server/LocadoraVeiculos2026
dotnet ef database update -p Server/LocadoraVeiculos.Infraestrutura.Orm -s Server/LocadoraVeiculos2026
# Banco
SqlLocalDB start MSSQLLocalDB
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -d LocadoraVeiculos2026DB -Q "SELECT name FROM sys.tables"
# Front (em client/)
npm install
npm start            # ng serve → :4200
npm run build
npm test             # karma/jasmine (precisa de Chrome)
npx ng ...           # `ng` não está no PATH global
```
Não existe projeto de testes no Server.

## Armadilhas conhecidas (2026-11-14)
1. Já corrigidos: `launchSettings.json` (perfil `https [Dev]` → `https://localhost:9099;http://localhost:9050`), `appsettings.json` (config válida), `environment.development.ts` (`https://localhost:9099/api`) e dependência circular Orm→Aplicacao no `.sln`.
2. A API usa `UseHttpsRedirection`: chame por **https://localhost:9099** (cert de dev não confiável → `curl -k`). Chamar http:9050 redireciona e quebra o preflight de CORS.
3. Resolvido: o banco local estava divergente e as migrations antigas eram inválidas (`Add_Identity` só criava `Funcionarios`; `Add_Funcionarios` vazia) → 500 `Invalid object name 'RefreshTokens'`. Foram substituídas por uma única `Initial_Create` e o banco foi recriado. Se o banco divergir de novo: `dotnet ef database drop --force` + `database update`. Registro e login verificados com 200 OK em `https://localhost:9099/api/auth/*`.
3. `CORS_ALLOWED_ORIGINS` precisa bater exatamente com a origem do front (`http://localhost:4200`); `AllowCredentials` é usado por causa do cookie de refresh.
4. Versões de EF Tools misturadas entre csproj (8.0.30 e 10.0.11) e `dotnet-ef` global 10.0.14 — funciona, mas atenção a avisos.
5. `package-lock.json` na raiz do repo é lixo (vazio) e está untracked; o lock real é `client/package-lock.json`.
6. Erros de digitação nos nomes: pasta `Config/Indentity`, `IndetityTenantProvider.cs`, `RepostiorioGrupoVeiculosEmOrm.cs`.
