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

- Módulos existentes: Autenticação, Funcionário, GrupoVeiculos, Condutor (pertence a um cliente que não muda na edição; CPF único por cliente — a mesma pessoa pode ser condutora de clientes diferentes; `ValidadeCnh` é `DateOnly` (JSON yyyy-MM-dd); CNH vencida pode ser cadastrada e vem com `CnhVencida=true`, o aluguel é que exigirá CNH em dia; excluir cliente com condutores → 422), Cliente (uma tabela só: `TipoCliente` PessoaFisica/PessoaJuridica; PF tem CPF/RG/CNH, PJ tem CNPJ; CPF/CNPJ únicos por empresa e validados com dígitos verificadores em `Aplicacao/Compartilhado/DocumentosUtil`; PF pode ter `PessoaJuridicaId`; o tipo não muda na edição; excluir PJ com PF vinculada → 422; os validadores por tipo são `internal` e escolhidos no handler), TaxaServico (catálogo de taxas: nome único por empresa sem diferenciar maiúsculas, preço, `TipoCobrancaTaxa` Fixo|Diario; escrita só `Empresa`), Automovel (placa única por empresa, normalizada sem hífen; foto em `varbinary(max)` no próprio registro, servida só por endpoint autenticado, validada pela assinatura real do arquivo — JPEG/PNG/WebP, máx. 2 MB, SVG recusado; listagem usa projeção `AutomovelResumo` sem os bytes; excluir grupo com automóveis → 422), PlanoCobranca (um plano por grupo com 3 modalidades Diário/Controlado/Livre; índice único filtrado por `Excluido = 0`; excluir grupo com plano → 422), Parceiro (só `Nome`; excluir com cupom → 422), Cupom (`Nome` único por empresa, `Valor` fixo, `DataValidade` DateOnly inclusive, `ParceiroId`; escrita só `Empresa`; ranking `GET api/cupons/mais-utilizados` com cupons e parceiros; cadastrar exige validade ≥ hoje, mas editar aceita data passada = encerrar), Configuração (preço dos 4 combustíveis + diária do seguro `Cliente`/`Terceiros`; entidade `Configuracao` única por empresa, upsert; `TipoCombustivel`/`TipoSeguro` em `Dominio/Compartilhado`), Aluguel (ver abaixo). **Cupom no aluguel:** `DadosAluguelDto.CupomId?`; o aluguel copia `CupomId/CupomNome/CupomValor`; desconto = min(valor, consumo) — consumo exclui caução, multa e combustível; só cupom não expirado (quem já usa o cupom mantém ao editar), um uso por cliente (`ClienteJaUsouCupomAsync`, ignora o próprio aluguel e os excluídos); cupom em aluguel aberto não exclui. **Aluguel** (`ModuloAluguel`): guarda *snapshot* de nomes, plano, seguro e taxas (`AluguelTaxa`), então mudar preços depois não altera o aluguel. Regras assumidas da especificação: caução fixa R$ 1.000 INCLUÍDA nos totais; dias = max(1, retorno − saída); Diário = diárias + km rodados × preço/km; Controlado = km acima da franquia (kmDisponíveis × dias) × preço extrapolado; Livre = só diárias; seguro por dia (preço em Configurações); taxa Fixo cobra 1× e Diario × dias; devolução com tanque não cheio cobra capacidade × preço do combustível (config); atraso = multa de 10% sobre diárias+km+taxas+seguro; valor final = caução + tudo. Carro ocupado = outro aluguel em aberto sobreposto (vencido e não devolvido ocupa até hoje); CNH precisa valer até o retorno previsto; só edita em aberto, só exclui concluído. Cálculos em `Aluguel.CalcularPrevisto/CalcularFinal` e montagem em `AluguelServico`. Editar troca `Taxas`: novos `AluguelTaxa` têm Id preenchido, então o handler marca `EntityState.Added` (senão o EF faz UPDATE e dá concorrência). Erros `OperacaoBloqueada` → 422 (ex.: editar automóvel/taxa em aluguel aberto). Excluir automóvel/taxa/condutor/cliente/plano/funcionário ligado a aluguel em aberto → 422. Novo módulo = Dominio entidade → Orm mapeador+repositório (+registrar em `DependencyInjection` do Orm) → Aplicacao commands/handlers/validators → Controller + Models → migration.
- Rotas: `api/auth` (autenticar, registrar, rotacionar, sair), `api/funcionarios` (só `Empresa`; inclui `PATCH {id}/status` para ativar/desativar), `api/funcionarios/meu-perfil` (só `Funcionario`: GET/PUT de nome e CPF próprios; o id vem do token), `api/grupos-veiculos` (`Empresa` e `Funcionario`), `api/configuracoes` (GET `Empresa`+`Funcionario`, PUT só `Empresa`), `api/planos-cobranca` (GET `Empresa`+`Funcionario`; POST/PUT/DELETE só `Empresa`), `api/condutores` (`Empresa`+`Funcionario`; GET aceita `?clienteId=`), `api/clientes` (`Empresa`+`Funcionario`; GET aceita `?tipo=PessoaFisica|PessoaJuridica`), `api/taxas-servicos` (GET `Empresa`+`Funcionario`; POST/PUT/DELETE só `Empresa`), `api/parceiros` e `api/cupons` (GET `Empresa`+`Funcionario`; escrita e ranking só `Empresa`), `api/alugueis` (`Empresa`+`Funcionario`; GET aceita `?status=EmAberto|Concluido`; `POST calcular` simula o preço; `POST {id}/devolucao` e `POST {id}/devolucao/calcular`), `api/automoveis` (`Empresa`+`Funcionario`; GET aceita `?grupoVeiculosId=`; foto em `PUT/GET/DELETE {id}/foto`, upload multipart campo `arquivo`). Desativar um funcionário revoga access/refresh tokens e bloqueia o login.
- Multi-tenant: filtro global `EmpresaId == tenant && !Excluido` em `AppDbContext` (soft delete); tenant vem da claim `EmpresaId` (`IdentityTenantProvider`).
- JWT: token valida `jti` contra `Usuario.AccessTokenVersionId` (revogação); refresh token em cookie (`RefreshTokenCookieService`).
- Em `Development` a API roda `Database.Migrate()` ao subir e expõe Swagger em `/swagger`.

### client (Angular 20, standalone, Material + Tailwind 4, ngx-mask)
`client/src/app/`: `auth/` (login, registro, interceptor, service, `cargo.guard.ts`), `funcionarios/` (CRUD + ativar/desativar; `perfil/` = Meu Perfil do funcionário), `grupo-veiculos/` (CRUD), `configuracoes/` (preços dos combustíveis, só Empresa), `planos-cobranca/` (CRUD com formulário em abas `form/`, só Empresa), `condutores/` (CRUD; o `form/` tem "cliente é condutor", que copia os dados do cliente PF e trava esses campos; datas dd/MM/yyyy na tela, ISO na API), `clientes/` (CRUD; o `form/` alterna campos de PF e PJ habilitando/desabilitando controles), `taxas-servicos/` (CRUD com formulário `form/`, só Empresa), `parceiros/` e `cupons/` (CRUD, só Empresa; a lista de cupons mostra o ranking), `alugueis/` (seleção de cupom no form; listar com filtro de situação, `form/` com valor previsto ao vivo via `POST calcular`, página `devolucao/` com valor final ao vivo), `automoveis/` (CRUD + foto: `foto/` baixa a imagem como Blob com o token; `shared/imagem/comprimir-imagem.ts` reduz a foto antes do upload), `shared/` (shell, notificação, `erros/mensagem-erro.ts`), `inicio/`. Rotas protegidas por cargo via `cargoGuard(...)` em `app.config.ts`. Novo módulo no front = copiar `grupo-veiculos/` (models, service, routes, listar/cadastrar/editar/excluir) + rota + item no shell.
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
# Testes (Server) — não precisam de SQL Server nem de LocalDB
dotnet test Server/LocadoraVeiculos.Testes
dotnet test Server/LocadoraVeiculos.Testes --collect:"XPlat Code Coverage"   # ~89%, migrations excluídas (coverlet.runsettings)
# Docker (SQL Server + app): copiar .env.example para .env e preencher; abre em http://localhost:8080
docker compose up --build
```
Projeto de testes `Server/LocadoraVeiculos.Testes` (xUnit): `Dominio/` e `Aplicacao/` = unitários (regras de preço do `Aluguel`, validadores `internal` via `InternalsVisibleTo`, `DocumentosUtil`); `Integracao/` = `ApiFactory` (`WebApplicationFactory<Program>`, ambiente `Testing`, EF InMemory no lugar do SQL Server, chaves de teste fixas) + `Cenario` (empresa com cadastros prontos e geradores de CPF/CNPJ) + `ClienteApi`. Limitação do InMemory: unicidade sem diferenciar maiúsculas depende do collation do SQL Server, então não é testada. CI em `.github/workflows/ci.yml` (build + testes com cobertura + build do Angular). O front ainda não tem testes (`ng test`).
Publicação: a API serve o front Angular (pasta `wwwroot`, ver `Program.cs`: `UseStaticFiles` + `UseRouting` explícito DEPOIS + `MapFallbackToFile` que ignora `/api`), então é uma só origem e o `apiUrl` de produção é `/api`. Em produção as migrations só rodam com `APLICAR_MIGRACOES=true`. `Dockerfile` na raiz gera a imagem única; `.github/workflows/azure.yml` (manual) faz deploy no App Service; guia em `docs/PUBLICAR-AZURE.md`. Verificado localmente (publish + wwwroot + Production + LocalDB isolado): SPA, .js/.css, 404 em /api desconhecida, registro e 401 sem token. O Docker e o deploy na Azure ainda não foram executados.

## Armadilhas conhecidas (2026-11-14)
1. Já corrigidos: `launchSettings.json` (perfil `https [Dev]` → `https://localhost:9099;http://localhost:9050`), `appsettings.json` (config válida), `environment.development.ts` (`https://localhost:9099/api`) e dependência circular Orm→Aplicacao no `.sln`.
2. A API usa `UseHttpsRedirection`: chame por **https://localhost:9099** (cert de dev não confiável → `curl -k`). Chamar http:9050 redireciona e quebra o preflight de CORS.
3. Resolvido: o banco local estava divergente e as migrations antigas eram inválidas (`Add_Identity` só criava `Funcionarios`; `Add_Funcionarios` vazia) → 500 `Invalid object name 'RefreshTokens'`. Foram substituídas por uma única `Initial_Create` e o banco foi recriado. Se o banco divergir de novo: `dotnet ef database drop --force` + `database update`. Registro e login verificados com 200 OK em `https://localhost:9099/api/auth/*`.
3. `CORS_ALLOWED_ORIGINS` precisa bater exatamente com a origem do front (`http://localhost:4200`); `AllowCredentials` é usado por causa do cookie de refresh.
4. Versões de EF Tools misturadas entre csproj (8.0.30 e 10.0.11) e `dotnet-ef` global 10.0.14 — funciona, mas atenção a avisos.
5. `package-lock.json` na raiz do repo é lixo (vazio) e está untracked; o lock real é `client/package-lock.json`.
6. Erros de digitação nos nomes: pasta `Config/Indentity`, `IndetityTenantProvider.cs`, `RepostiorioGrupoVeiculosEmOrm.cs`.
7. **LocalDB e testes do Claude:** não iniciar/usar a instância `mssqllocaldb` a partir do Claude — quando ela é iniciada pela sessão do Claude, o Visual Studio do dev falha com "LocalDB instance startup: SQL Server process failed to start" (erro 575). Para testar a API: `SqlLocalDB create ClaudeDev`, rodar com `SQL_CONNECTION_STRING` apontando para `(localdb)\ClaudeDev` (env var sobrescreve o user-secret), porta separada (`--no-launch-profile --urls "https://localhost:9199;http://localhost:9150"`) e, ao final, `SqlLocalDB stop ClaudeDev -k` + `SqlLocalDB delete ClaudeDev`. Compilar (`dotnet build`) **antes** de `dotnet run --no-build`, senão migrations novas não são aplicadas. Apagar também os arquivos `LocadoraClaudeTeste.mdf` e `LocadoraClaudeTeste_log.ldf` que ficam soltos na pasta do usuário do Windows (o `SqlLocalDB delete` não os remove; se ficarem, a próxima criação do banco falha com erro 5170 "Cannot create file ... already exists").
