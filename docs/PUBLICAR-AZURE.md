# Publicar na Azure

O sistema vai como **uma única aplicação**: a API .NET serve também o front Angular (pasta `wwwroot`).
Assim há um só endereço, sem CORS e com o cookie de renovação da sessão funcionando.

```
Navegador ──HTTPS──► Azure App Service (API + front) ──► Azure SQL Database
```

## O que você vai criar

| Recurso | Para quê | Custo aproximado |
|---|---|---|
| **Azure SQL Database** | O banco de dados | Existe uma oferta gratuita (serverless, com limite mensal). Fora dela, a partir de alguns dólares por mês. |
| **App Service (Linux, .NET 8)** | Roda a API e o front | O plano **F1** é gratuito (limite de CPU por dia, "dorme" quando ocioso). O **B1** custa cerca de US$ 13 por mês e fica sempre ligado. |

Confira os preços atuais no portal antes de criar. Para uma demonstração, F1 + banco gratuito costuma bastar.

## Passo 1: criar o banco

1. No [portal da Azure](https://portal.azure.com), crie um **Azure SQL Database** (se aparecer, aplique a oferta gratuita).
2. Crie um servidor novo com **autenticação SQL**: guarde o *usuário administrador* e a *senha*.
3. Em **Rede** do servidor, marque **Permitir que serviços e recursos do Azure acessem este servidor**.
4. Copie a *connection string* (ADO.NET) do banco e coloque a senha real nela. Ela fica assim:
   `Server=tcp:SEU-SERVIDOR.database.windows.net,1433;Database=SEU-BANCO;User ID=USUARIO;Password=SENHA;Encrypt=True;TrustServerCertificate=False;`

## Passo 2: criar o App Service

1. Crie um **App Service** com: publicar = **Código**, pilha = **.NET 8 (LTS)**, sistema = **Linux**.
2. Escolha o plano (F1 ou B1). Anote o endereço, algo como `https://meu-app.azurewebsites.net`.
3. Em **Configuração > Configurações do aplicativo**, adicione:

| Nome | Valor |
|---|---|
| `SQL_CONNECTION_STRING` | a connection string do passo 1 |
| `JWT_GENERATION_KEY` | um texto longo e aleatório (32 caracteres ou mais) |
| `JWT_AUDIENCE_DOMAIN` | o endereço do app (ex.: `https://meu-app.azurewebsites.net`) |
| `CORS_ALLOWED_ORIGINS` | o mesmo endereço do app |
| `LUCKYPENNY_LICENSE_KEY` | a sua licença do MediatR |
| `APLICAR_MIGRACOES` | `true` (cria as tabelas na primeira subida) |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

4. Em **Configurações > Configuração geral**, ative **Somente HTTPS**.

> Essas chaves ficam só na Azure. Não coloque a licença nem a chave JWT no GitHub.

## Passo 3: conectar o GitHub

1. No App Service, clique em **Baixar perfil de publicação** (arquivo `.PublishSettings`). Em **Configuração > Configurações gerais**, deixe a **autenticação básica para publicação** habilitada (o perfil depende disso).
2. No repositório do GitHub, vá em **Settings > Secrets and variables > Actions** e crie dois *secrets*:
   - `AZURE_WEBAPP_NAME`: o nome do App Service (só o nome, ex.: `meu-app`)
   - `AZURE_WEBAPP_PUBLISH_PROFILE`: o **conteúdo inteiro** do arquivo `.PublishSettings`
3. Apague o arquivo `.PublishSettings` do seu computador depois de colar o conteúdo (ele dá acesso ao app).

## Passo 4: publicar

1. No GitHub, abra **Actions > Publicar na Azure > Run workflow**.
2. Quando terminar, abra o endereço do app. Na primeira vez ele pode demorar um pouco, porque cria o banco.
3. Crie a conta da empresa em **Registro** e use o sistema normalmente.
4. Depois que as tabelas existirem, você pode **remover `APLICAR_MIGRACOES`** (ou deixar como está: aplicar migrations sem novidade não faz nada).

## Para outra pessoa testar

Passe o endereço do app. Cada pessoa cria a **própria empresa** em *Registro*, e os dados dela ficam separados dos de todo mundo.

## Problemas comuns

- **Erro 500 ao entrar:** quase sempre é a `SQL_CONNECTION_STRING` (senha, firewall do SQL) ou a falta de alguma configuração da tabela acima. Veja **Fluxo de log** no App Service.
- **Banco "dormindo" (serverless):** a primeira chamada depois de um tempo parado pode demorar ou falhar uma vez. Tente de novo.
- **Tela em branco ao abrir uma rota direto (ex.: `/alugueis`):** confirme que o deploy foi feito pelo workflow, que coloca o front dentro de `wwwroot`.
- **Sessão cai logo após o login:** confira se o acesso é por HTTPS (o cookie de renovação exige).
