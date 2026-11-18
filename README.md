# CoC API (Cockpit of Communications)

API em .NET 10 (ASP.NET Core minimal APIs) para gerir **users** e **campanhas**, com persistência em SQL Server através do Entity Framework Core.

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) com um container de SQL Server (ver abaixo)
- Ferramenta `dotnet-ef` na versão 10

## Base de dados

A API usa SQL Server. A connection string está em `CoC_API/appsettings.json` (`ConnectionStrings:Default`) e aponta para:

```
Server=127.0.0.1,14330;Database=Cockpit_of_Comms;User Id=sa;...
```

Ou seja, espera um container de SQL Server **na tua própria máquina**, com a porta **14330** exposta e a password do `sa` igual à do `appsettings.json`.

> Cada pessoa tem a sua própria base de dados local. Os dados criados por um colega **não** aparecem nos outros.

Se o teu container usar outra porta ou outra password, não alteres o `appsettings.json` para o commit: define a connection string só na tua máquina, por exemplo com uma variável de ambiente:

```powershell
$env:ConnectionStrings__Default = "Server=127.0.0.1,<porta>;Database=Cockpit_of_Comms;User Id=sa;Password=<password>;TrustServerCertificate=true"
```

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

A versão 5.x, que pode vir instalada, não funciona com este projeto.

```bash
dotnet tool update --global dotnet-ef
```

### 3. Criar as tabelas

Dentro da pasta `CoC_API` (a que contém o `CoC_API.csproj`):

```bash
dotnet ef database update
```

Isto cria a base `Cockpit_of_Comms` e aplica as migrações. Só é preciso uma vez, e de novo quando houver migrações novas.

### 4. Correr a API

```bash
dotnet run
```

Ou pelo Visual Studio. Em modo Development a API escuta em `http://localhost:5062`, e o Swagger fica em `http://localhost:5062/swagger`.

Para testar os pedidos de exemplo, usa o ficheiro `CoC_API/CoC_API.http`.

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
| PUT | `/campaigns/{id}/motivo` | Define o motivo e a justificação |
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
