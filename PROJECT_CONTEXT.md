# Contexto completo do projeto CoC

Última atualização: 2026-11-20

Este documento é uma passagem de contexto para outro assistente. Resume a arquitetura, os fluxos de negócio, as páginas atuais, decisões já tomadas e o estado local conhecido. Não substitui a leitura do código: antes de alterar algo, confirmar o estado atual do Git e consultar os ficheiros relevantes.

## 1. Resumo do produto

**CoC** significa **Cockpit of Communications**. O repositório contém uma API para utilizadores e campanhas, e um frontend estático em desenvolvimento. A identidade visual usa a marca AskBlue.

O foco recente tem sido o protótipo da listagem de contas/utilizadores em `CoC_front/AccountListingPage/`. O utilizador pediu expressamente para **não ligar ainda essa página à API**. Os dados e ações da página são demonstração local.

## 2. Preferências e decisões do utilizador

- Comunicar com o utilizador em português.
- Os rótulos existentes do frontend estão maioritariamente em inglês; manter esse padrão salvo pedido contrário.
- Reutilizar cores, grafismo, tipografia e logo da página de login, mas não o seu layout dividido em duas colunas.
- A listagem deve ocupar bem a largura disponível; não deve ficar como um cartão estreito no centro.
- As regras de aumento de dimensões para desktop começam em `min-width: 1024px`; não alterar o comportamento móvel sem pedido.
- Não ligar o protótipo à API, criar formulários de criação/edição ou inventar uma página de campanhas sem pedido.
- Preservar alterações locais existentes. Não descartar, reverter ou fazer commit do estado de trabalho sem autorização explícita.

## 3. Estrutura do repositório

```text
CoC_API/
├── CoC_API.slnx
├── README.md
├── CoC_API/
│   ├── CoC_API.csproj
│   ├── Program.cs
│   ├── Context/
│   │   └── CoC_DbContext.cs
│   ├── DTOs/Request/
│   ├── Endpoints/
│   ├── Migrations/
│   ├── Models/
│   ├── Properties/
│   └── Security/
├── CoC_API.Tests/             # artefactos locais bin/obj; sem projeto/testes fonte encontrados
└── CoC_front/
    ├── AccountListingPage/
    │   ├── accounts.html
    │   ├── accounts.css
    │   └── accounts.js
    ├── EditUserPage/
    │   ├── edit-user.html
    │   ├── edit-user.css
    │   └── edit-user.js
    ├── LogInPage/
    │   ├── index.html
    │   ├── styles.css
    │   └── app.js
    ├── FRONTEND_STYLE_GUIDE.md
    ├── askblue-logo-oficial-rgb.png
    └── FundoTeams2025_askblueGrafismo.jpg
```

O `CoC_API.slnx` inclui o projeto `CoC_API/CoC_API.csproj`. No estado observado não existe um `.csproj` nem código-fonte de testes sob `CoC_API.Tests`; essa pasta contém apenas artefactos locais. Não assumir que há uma suite de testes executável nesse diretório.

O `.gitignore` é baseado no template Visual Studio/.NET e ignora `bin/`, `obj/`, resultados de testes e outros artefactos.

## 4. Backend: tecnologia e arranque

- ASP.NET Core Minimal API, target `net10.0`, nullable e implicit usings ativados.
- Persistência via Entity Framework Core 10.0.14 com providers SQL Server e SQLite.
- Swagger/Swashbuckle 10.3.1, disponível em Development.
- O `.csproj` também referencia `Microsoft.EntityFrameworkCore.Tools` 10.0.14 e `System.Security.Cryptography.Pkcs` 10.0.14.
- `Program.cs` regista o `CoC_DbContext` usando `ConnectionStrings:Default`.
- O README documenta SQL Server local em `127.0.0.1:14330`, base `Cockpit_of_Comms`, atualização de migrations com `dotnet ef database update` e arranque com `dotnet run`.
- No perfil Development a API usa `http://localhost:5062`; o perfil HTTPS também declara `https://localhost:7029`. O Swagger UI fica em `/swagger`.
- `appsettings.json` contém configuração local de ligação à base de dados. **Não copiar passwords, connection strings ou outros valores locais para documentação, commits ou respostas.**
- `Program.cs` não configura `UseStaticFiles` nem CORS; o frontend atual é separado da API.

### Comandos habituais documentados

Executar na pasta `CoC_API/`:

```powershell
dotnet ef database update
dotnet run
```

O README também descreve requisitos de Docker Desktop, SQL Server local e `dotnet-ef` da versão 10. Consultá-lo para instruções completas e resolução de problemas; não alterar a connection string partilhada para acomodar configuração local.

Problemas conhecidos documentados: num timeout SQL, confirmar Docker/porta e usar `127.0.0.1`; usar `dotnet-ef` 10; se `CoC_API.exe` estiver bloqueado, verificar se já há uma instância da API; para erro de Swagger `MissingMethodException`, não adicionar `Microsoft.OpenApi` diretamente e seguir a limpeza de `bin/obj` descrita no README.

## 5. Persistência e migrations

### `CoC_DbContext`

Em `CoC_API/Context/CoC_DbContext.cs`, o contexto expõe:

- `Users`
- `Campaigns`
- `UserSessions`

As propriedades do `User` são mapeadas explicitamente para campos internos (`idValue`, `nameValue`, etc.). O modo de acesso a campos evita executar validação/hash de password quando o EF materializa entidades.

`UserSession` tem `TokenHash` como chave, comprimento máximo 64, relação com `User` e cascade delete.

### Migrations existentes

1. `PrimeiraMigration`: cria `Users` e `Campaigns`.
2. `AlignCampaignModelWithWorkflow`: torna motivo e justificação anuláveis e alinha `PublicationDate` com o workflow.
3. `PersistUserSessions`: cria `UserSessions`, chave pelo hash do token, FK para `Users` e índice por `UserId`.

O snapshot atual é `Migrations/CoC_DbContextModelSnapshot.cs`. Não editar migrations antigas para mudanças novas; seguir o fluxo normal de gerar uma nova migration, se o utilizador pedir alterações de schema.

## 6. Utilizadores

### Modelo e validações

O modelo é `CoC_API/Models/User.cs`. Propriedades públicas relevantes:

- `id`
- `name`
- `email`
- `password` (decorada com `JsonIgnore`)
- `birthDate`
- `taxId`
- `caracteristics`

Regras implementadas no modelo:

- O nome não pode ser vazio e tem de conter pelo menos duas partes separadas por espaços.
- O email não pode ser vazio e tem de conter `@`; o setter remove espaços exteriores. A unicidade é verificada pelos endpoints após normalização para maiúsculas.
- O `id` rejeita valores negativos; create passa `0` porque a coluna é `IDENTITY` e a base gera o ID real.
- A password tem de ter mais de 8 caracteres, maiúscula, minúscula, dígito e símbolo. O código atual usa SHA-256 sem salt; isto é comportamento existente, não uma recomendação para novos sistemas.
- A data de nascimento não pode estar no futuro.
- `taxId` tem de ter exatamente 9 dígitos.
- As características não podem ser nulas nem conter valores de enum inválidos.
- O construtor de utilizador novo adiciona `isActive` se ainda não estiver presente.
- `DeactivateUser()` remove `isActive` e falha se o utilizador já estiver inativo.

As enums são serializadas como strings por `JsonStringEnumConverter`. `UserCaracteristics` contém `Admin`, `isAprovando`, `isAprovador` e `isActive`.

### DTOs de utilizador

- `CreateUserRequest`: `Name`, `Email`, `Password`, `BirthDate`, `TaxId`, `Caracteristics`.
- `EditUserRequest`: `Name`, `Email`, `Password`, `caracteristics`.
- A edição exige password no DTO atual.
- O modelo não tem `CreatedAt`; na UI a coluna disponível é `birthDate`.

### Endpoints de utilizadores

Definidos em `CoC_API/Endpoints/UserEndpoints.cs`:

| Método e rota | Comportamento | Autorização |
|---|---|---|
| `GET /users` | Lista todos os utilizadores, sem paginação | Autenticação global; sem role Admin declarada no endpoint |
| `GET /users/{id}` | Obtém um utilizador ou 404 | Autenticação global |
| `POST /users` | Cria utilizador, verifica email duplicado e devolve 201 | Role Admin |
| `PUT /users/{id}` | Atualiza nome/email/password/características; 404 se não existir, 409 para email duplicado | Role Admin |

Erros de validação de create/update devolvem 400; email duplicado devolve 409. Não existe atualmente endpoint dedicado de delete ou deactivate. O método `DeactivateUser()` do modelo não está exposto por uma rota própria.

`GET /users` devolve as entidades `User` diretamente; a password fica excluída por `JsonIgnore`, mas os outros campos públicos, incluindo `taxId` e características, são serializados.

## 7. Autenticação e autorização

Implementação distribuída por:

- `Endpoints/AuthEndpoints.cs`
- `Security/UserSessionStore.cs`
- `Security/UserClaimsPrincipalFactory.cs`
- `Models/UserSession.cs`
- configuração em `Program.cs`

### Login/logout

- `POST /login` é anónimo e recebe email/password.
- Email é normalizado para comparação sem distinção de maiúsculas.
- Utilizador inexistente, password incorreta ou utilizador sem `isActive` devolve 401.
- Login cria uma sessão persistida e autentica por cookie.
- `POST /logout` revoga a sessão e termina o cookie.
- `LoginRequest` contém apenas `Email` e `Password`.

### Cookie e sessões

- Cookie: `CoC_API.Session`, HttpOnly, SameSite Strict.
- Em Development usa `SameAsRequest`; noutros ambientes exige HTTPS.
- Duração de 8 horas, sem sliding expiration.
- O token aleatório tem 32 bytes; a base de dados guarda o hash SHA-256 do token, não o token em claro.
- Sessões expiradas/revogadas são removidas durante criação de sessão.
- Em cada validação do principal, o sistema confirma sessão e utilizador ativo; se inválido, rejeita principal e termina sessão.
- Claims incluem ID, nome e session ID. Roles são adicionadas para Admin e isAprovador. `isAprovando` é verificado a partir das características do utilizador.

### Política global e Admin Development

- A fallback authorization policy exige utilizador autenticado em todas as rotas que não permitam anonymous explicitamente.
- Redirects de cookie são convertidos em status 401/403 em vez de redirecionamentos HTML.
- Em Development podem ser definidos `COC_API_DEV_ADMIN_EMAIL` e `COC_API_DEV_ADMIN_PASSWORD`. Se apenas um for definido, a aplicação falha no arranque; se ambos existirem, cria um admin inicial se necessário ou falha se a conta já existir sem role Admin.
- Não incluir valores dessas variáveis no contexto ou no controlo de versões.

## 8. Campanhas

### Modelo e enums

`CoC_API/Models/Campaign.cs` representa uma campanha com:

- `Id` GUID
- `Name`, `Description`, `ModelMsg`
- `Type`, `Status`
- `CreatedAt`, `PublicationDate`
- `ImageUrl`
- `UserId`
- `motivo` e `justificationText` opcionais

As propriedades validam campos obrigatórios e enums válidas. A data de publicação não pode ser default nem anterior ao minuto atual (comparação UTC ao minuto). A campanha recebe `CreatedAt` em UTC.

Enums em `Models/Enums.cs`:

- `CampaignType`: `Email`, `Messaging`, `Instagram`, `Facebook`, `LinkedIn`.
- `CampaignStatus`: `New`, `Scheduled`, `Sending`, `Closed`, `InReview`, `ForChange`, `Approved`, `Canceled`.
- `Motivo`: `None`, `InadequateContent`, `InadequateLanguage`, `InadequateImage`, `InadequateTargetAudience`, `Other`.
- As enums são serializadas como strings.

### DTOs de campanha

- `CreateCampaignRequest`: nome, descrição, mensagem/modelo, tipo, data de publicação e URL opcional de imagem.
- `EditCampaignRequest`: os mesmos campos editáveis.
- `RequestCampaignChangesRequest`: justificação.
- `MotivoRequest`: justificação e motivo.

### Rotas e workflow

As rotas são mapeadas em `CoC_API/Endpoints/CampaingEndPoints.cs` (o nome da classe tem esse spelling):

| Método e rota | Regra principal |
|---|---|
| `GET /campaigns` | Devolve campanhas próprias; aprovadores também veem campanhas `InReview` de owners com `isAprovando` |
| `POST /campaigns` | Utilizador autenticado cria campanha; owner `isAprovando` inicia em `InReview`, outros usam estado calculado pela data |
| `GET /campaigns/{id}` | Owner vê a própria; aprovador pode ver `InReview` de owner aprovando; caso contrário 404 |
| `PUT /campaigns/{id}/edit` | Apenas owner; não edita `Canceled`/`Closed`; regras especiais para `ForChange` |
| `PUT /campaigns/{id}/cancel` | Apenas owner; motivo e justificação obrigatórios; não cancela campanhas `Sending`, `Closed` ou `Canceled` |
| `PUT /campaigns/{id}/review/approve` | Requer aprovador e campanha reviewable |
| `PUT /campaigns/{id}/review/request-changes` | Requer aprovador; guarda justificação e muda para `ForChange` |
| `PUT /campaigns/{id}/review/reject` | Requer aprovador; rejeição conclui como `Canceled` com motivo/justificação |
| `GET /campaigns/{id}/motivo` | Devolve motivo/justificação se o utilizador puder ver a campanha |

`Campaign.ResolvePublicationStatus()` devolve `Sending` quando a publicação é no minuto atual e `Scheduled` quando é futura. Aprovação resolve o estado novamente pela data. Em geral, campanhas inacessíveis são tratadas como 404; ações não permitidas devolvem 403; conflitos de edição devolvem 409.

`CampaignStatus` também contém `Approved`, mas o método atual `Approve()` resolve o estado pela data (`Scheduled` ou `Sending`) em vez de atribuir `Approved`.

`GET /campaigns` e os handlers consultam a identidade autenticada e confirmam que o utilizador continua ativo.

## 9. Frontend atual

### Login

Ficheiros:

- `CoC_front/LogInPage/index.html`
- `CoC_front/LogInPage/styles.css`
- `CoC_front/LogInPage/app.js`

A página recolhe email e password, permite mostrar/ocultar password, verifica email/password vazios e formato de email, e mostra erros/status no browser. Ainda **não** chama `POST /login`; a mensagem após validação indica que está pronta para ligar à API.

O layout de login tem painel AskBlue com grafismo à esquerda e formulário à direita em desktop; em ecrãs pequenos o painel de marca desaparece. Esse layout dividido é específico do login e não deve ser copiado para a listagem.

### Listagem de contas

Ficheiros:

- `CoC_front/AccountListingPage/accounts.html`
- `CoC_front/AccountListingPage/accounts.css`
- `CoC_front/AccountListingPage/accounts.js`

Comportamento atual:

- Usa 67 contas totalmente sintéticas em JavaScript, sem chamadas à API nem persistência.
- Pesquisa por nome/email.
- Ordenação crescente/decrescente por nome e `birthDate`.
- “Show inactive” inclui utilizadores inativos; por omissão a lista mostra apenas ativos.
- Cada linha tem “Edit” e “Deactivate”. “Edit” mostra aviso de que o formulário ainda não existe.
- “Deactivate” marca `active = false` apenas na memória; a linha desaparece por omissão e reaparece com “Show inactive”. O estado volta ao início ao recarregar.
- “Create Account” mostra aviso; não existe formulário de criação.
- “Back to campaigns” está na toolbar, depois dos outros controlos, com `margin-left: auto`; alinha à direita. Como não existe página frontend de campanhas, mostra aviso em vez de navegar.
- Status/feedback é apresentado em região `aria-live`.
- A interface usa inglês (ex.: “Accounts”, “Birth date”, “Show inactive”) embora `accounts.html` tenha `lang="pt-PT"`.
- Não há paginação; a lista longa usa scroll vertical normal da página. Em ecrãs estreitos, a tabela tem scroll horizontal.
- Os sete primeiros registos são exemplos fixos (incluindo Eduardo e Juan). Mais 60 são gerados por combinações de 10 nomes próprios e 6 apelidos; os emails usam `example.com`. O resultado inicial tem 59 ativos e 8 inativos. Datas e IDs gerados são determinísticos.
- Para uma conta já inativa, o botão “Deactivate” aparece desativado e o nome recebe o estado “Inactive”.

Estilo atual:

- Cabeçalho horizontal de marca com imagem `../askblue-logo-oficial-rgb.png` e grafismo `../FundoTeams2025_askblueGrafismo.jpg`.
- Cartão branco largo; azul AskBlue e controlos quadrados, inspirados no login sem replicar o layout de login.
- Tamanhos de desktop aumentados em `@media (min-width: 1024px)`.
- Em desktop, o cartão usa largura relativa ao viewport para não ficar estreito com zoom-out. O browser zoom não é bloqueado.
- Regras móveis mantêm tabela com scroll horizontal quando necessário e toolbar com wrap.

Não há frontend de campanhas identificado em `CoC_front`. O botão de retorno é, por isso, apenas um placeholder com feedback.

## 10. Assets e caminhos frontend

Os assets ficam na raiz de `CoC_front`:

- `askblue-logo-oficial-rgb.png`
- `FundoTeams2025_askblueGrafismo.jpg`

As páginas estão em subpastas. `accounts.html` referencia `accounts.css` e `accounts.js` na mesma pasta, logo/grafismo por `../...`, e link do logo para `../LogInPage/index.html`. Manter os caminhos relativos ao mover ficheiros.

## 11. Execução e integração ainda não feita

- A API corre em localhost conforme `CoC_API/Properties/launchSettings.json`; Swagger é ativado em Development.
- O frontend está separado e não há middleware de static files/CORS configurado no `Program.cs`.
- A conta de utilizadores exige cookie autenticado na API; `GET /users` não é anónimo.
- Ligar a listagem à API futuramente exigirá decidir como servir o frontend na mesma origem ou configurar CORS/cookies adequadamente, integrar o login real e adaptar os campos JSON.
- Para ações reais de conta, será necessário desenhar formulários e decidir autorização/contrato para edição e desativação. O backend atual não tem rota dedicada de desativação.
- Estas integrações não estão aprovadas como parte do protótipo atual.

## 12. Estado Git e histórico recente

Último estado observado nesta passagem:

- Repositório GitHub: `JuanMutizAB/CoC_API`.
- Remote `origin` aponta para esse repositório.
- Branch local `eduardo_local`, upstream `origin/Eduardo`.
- HEAD: `06db564` — `Add CoC_front files`.
- Working tree contém alterações não commitadas:
  - `CoC_front/app.js`, `CoC_front/index.html` e `CoC_front/styles.css` aparecem removidos da raiz.
  - `CoC_front/AccountListingPage/` e `CoC_front/LogInPage/` aparecem não rastreadas.
  - `PROJECT_CONTEXT.md` é novo/não commitado.
- As pastas não rastreadas contêm o frontend reorganizado e as alterações visuais recentes. Não as remover nem restaurar os ficheiros antigos da raiz automaticamente.
- `CoC_front` foi originalmente um repositório Git embutido; foi convertido em ficheiros normais do repositório principal no commit `06db564`.

## 13. Verificações já feitas no frontend

- `node --check` passou para `AccountListingPage/accounts.js`; o `app.js` do login foi lido, mas não foi validado por comando nessa sessão.
- Um DOM simulado verificou pesquisa, filtro, ordenação, listagem de 67 contas e desativação em memória.
- CSS foi verificado por equilíbrio de chavetas e por trailing whitespace em validações anteriores. `git diff --check` foi executado, mas como as pastas da UI estavam não rastreadas, essa verificação não substitui uma validação visual.
- Não foi feito build/teste do backend para as alterações da listagem.
- Não foi possível confirmar o aspeto num browser nesta sessão; usar preview/browser visual antes de afirmar validação visual.

## 14. Fontes de verdade para trabalho futuro

Consultar o código quando houver diferença entre este resumo e o repositório:

- Arranque/configuração: `README.md`, `CoC_API/Program.cs`, `CoC_API/Properties/launchSettings.json`.
- Modelo/schema: `CoC_API/Models/`, `CoC_API/Context/CoC_DbContext.cs`, `CoC_API/Migrations/`.
- Rotas: `CoC_API/Endpoints/`.
- Contratos HTTP: `CoC_API/DTOs/Request/`.
- Sessões/autorização: `CoC_API/Security/` e `Program.cs`.
- Páginas: `CoC_front/LogInPage/` e `CoC_front/AccountListingPage/`.
