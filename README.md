# CoC API (Cockpit of Communications)

API em .NET 10 (ASP.NET Core minimal APIs) para gerir **users** e **campanhas**, com persistência em SQL Server através do Entity Framework Core.

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) com um container de SQL Server (ver abaixo)
- Ferramenta `dotnet-ef` na versão 10

## Base de dados

A API usa SQL Server e lê a connection string da configuração `ConnectionStrings:Default`. Guarda a connection string localmente em User Secrets, não em `appsettings.json`:

```
dotnet user-secrets set "ConnectionStrings:Default" "Server=127.0.0.1,14330;Database=Cockpit_of_Comms;User Id=sa;Password=<password>;TrustServerCertificate=True" --project CoC_API/CoC_API.csproj
```

Isto configura apenas o teu projeto local. Cada pessoa deve usar as credenciais do seu próprio container SQL Server.

> Cada pessoa tem a sua própria base de dados local. Os dados criados por um colega **não** aparecem nos outros.

Se o teu container usar outra porta, altera a connection string nos User Secrets, não no `appsettings.json`.

## Como correr

### 1. Ligar o container de SQL Server

Confirma que está a correr e que a porta 14330 está exposta:

```bash
docker ps
```

Deve aparecer algo como `127.0.0.1:14330->1433/tcp`. Se o container estiver parado, arranca-o (ajusta o nome ao teu):

```bash
docker start askblue-sqlserver-sqlserver-1
```

O container não arranca sozinho depois de reiniciar o PC ou o Docker.

### 2. Instalar a ferramenta `dotnet-ef` (versão 10)

A versão da ferramenta deve corresponder ao EF Core 10 usado pelo projeto.

```bash
dotnet tool update --global dotnet-ef --version 10.0.14
```

### 3. Recriar a base de desenvolvimento

O histórico foi consolidado numa única migração inicial. **Todos os developers têm de recriar a sua própria base local** depois de obter estas alterações. Isto apaga os dados dessa base; faz backup antes, e nunca corras estes comandos contra uma base partilhada ou de produção.

Dentro da pasta `CoC_API` (a que contém o `CoC_API.csproj`):

```bash
dotnet ef database drop --force
dotnet ef database update
```

Isto recria `Cockpit_of_Comms` com o esquema final, incluindo sessões e publicação Instagram e LinkedIn.

### 4. Correr a API

```bash
dotnet run
```

Ou pelo Visual Studio. Em modo Development a API escuta em `http://localhost:5062`, e o Swagger fica em `http://localhost:5062/swagger`.

### Publicação no Instagram

Antes de iniciar a API, configura os segredos locais. Usa um token válido para uma conta profissional Instagram ligada à Meta e uma versão Graph API atualmente suportada:

```bash
dotnet user-secrets set "COC_API_INSTAGRAM_ACCESS_TOKEN" "<access-token>" --project CoC_API/CoC_API.csproj
dotnet user-secrets set "COC_API_INSTAGRAM_USER_ID" "<instagram-user-id>" --project CoC_API/CoC_API.csproj
dotnet user-secrets set "COC_API_INSTAGRAM_GRAPH_API_VERSION" "vNN.N" --project CoC_API/CoC_API.csproj
```

Cria uma campanha `Instagram` com uma imagem HTTPS publicamente acessível e uma data/hora UTC futura. Se o criador tiver a característica `isAprovando`, um utilizador `isAprovador` tem de aprová-la em `PUT /campaigns/{id}/review/approve`. O worker verifica a base de dados a cada 15 segundos e tenta publicar quando chega a hora. Consulta `GET /campaigns/{id}` para ver o estado: `Closed` significa publicada; `Failed` significa que a Meta rejeitou a publicação ou ocorreu um erro. Não há retry automático. Os testes automatizados usam uma resposta HTTP simulada e não publicam conteúdo real.

Para testar os pedidos de exemplo, usa o ficheiro `CoC_API/CoC_API.http`.

### Publicação no LinkedIn

A publicação usa um token OAuth de utilizador com a permissão `w_member_social`, autorizado pela LinkedIn App. Configura os segredos locais (não os guardes no `appsettings.json`):

```bash
dotnet user-secrets set "LinkedIn:AccessToken" "<access-token>" --project CoC_API/CoC_API.csproj
dotnet user-secrets set "LinkedIn:PersonUrn" "urn:li:person:<member-id>" --project CoC_API/CoC_API.csproj
dotnet user-secrets set "LinkedIn:ApiVersion" "202611" --project CoC_API/CoC_API.csproj
```

O token tem de ter autorização para publicar no perfil pessoal configurado; o membro da LinkedIn App também tem de ter acesso ao produto/API de Community Management. O token é gerado pelo fluxo OAuth da LinkedIn fora desta API e pode expirar, pelo que terá de ser renovado. A integração publica posts de texto e imagens JPEG, PNG ou GIF a partir de URLs HTTPS públicas; o acesso ao endereço da imagem é feito pela API.

Cria uma campanha `LinkedIn` com uma data/hora UTC futura. Se o criador tiver a característica `isAprovando`, um utilizador `isAprovador` tem de aprová-la em `PUT /campaigns/{id}/review/approve`. O worker verifica a base de dados a cada 15 segundos. Consulta `GET /campaigns/{id}` para ver o estado: `Closed` significa publicada e `Failed` indica erro. Não há retry automático. Os testes usam HTTP simulado e não publicam conteúdo real.

### Testes automatizados

```bash
dotnet test CoC_API.Tests/CoC_API.Tests.csproj
```

Os testes cobrem revisão, aprovação, rejeição, cancelamento, isolamento de campanhas e os workers Instagram e LinkedIn com respostas HTTP simuladas; não fazem publicações reais.

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| POST | `/users` | Cria um user (o `id` é gerado pela BD) |
| GET | `/users` | Lista os users |
| GET | `/users/{id}` | Obtém um user |
| PUT | `/users/{id}` | Edita um user |
| POST | `/login` | Autentica um user (email + password) |
| POST | `/campaigns` | Cria uma campanha |
| GET | `/campaigns` | Lista as campanhas |
| GET | `/campaigns/{id}` | Obtém uma campanha (o `id` é um GUID) |
| PUT | `/campaigns/{id}/edit` | Edita uma campanha |
| PUT | `/campaigns/{id}/cancel` | Cancela uma campanha |
| PUT | `/campaigns/{id}/review/approve` | Aprova uma campanha em revisão |
| PUT | `/campaigns/{id}/review/request-changes` | Pede alterações com justificação |
| PUT | `/campaigns/{id}/review/reject` | Rejeita uma campanha com motivo e justificação |
| GET | `/campaigns/{id}/motivo` | Obtém o motivo e a justificação |

Exemplo de body para `POST /users` (sem `id`):

```json
{
  "name": "Guilherme Conde",
  "email": "guilherme.conde@example.com",
  "password": "Password123!",
  "birthDate": "1990-01-01",
  "taxId": "123456789",
  "caracteristics": ["isAprovando"]
}
```

Regras de validação do user:

- nome com pelo menos nome e apelido;
- email com `@`, e único;
- password com mais de 8 caracteres, com maiúscula, minúscula, dígito e símbolo;
- `taxId` com exatamente 9 dígitos;
- data de nascimento não pode ser futura.

## Problemas comuns

**A API não liga à base de dados** (`error: 0 - The wait operation timed out`): confirma que o container está ligado (`docker ps`) e que a porta e a password coincidem com a connection string. Usa `127.0.0.1` em vez de `localhost`.

**`dotnet ef` falha com erros de versão:** atualiza a ferramenta com `dotnet tool update --global dotnet-ef`.

**O build falha com `CoC_API.exe` bloqueado:** há uma instância da API ainda a correr. Pára-a e volta a compilar.

**O Swagger dá erro 500 ao abrir** (`MissingMethodException` em `Microsoft.OpenApi`): não adiciones o pacote `Microsoft.OpenApi` ao `.csproj`. O Swashbuckle já traz a versão certa. Se o erro persistir, apaga as pastas `bin` e `obj` e volta a compilar.

**`POST /users` devolve 400 com "Expected depth to be zero":** o JSON enviado está mal formado, por exemplo falta o `}` final. Confirma o body no Swagger.
