# 📋 Cadastro de Clientes - Solução .NET 8

Demo webapp para cadastro de clientes com notificação via RabbitMQ. Arquitetura Clean, replicando a estrutura do projeto AgendamentoPro.

## 🏗️ Arquitetura

```
Browser → Blazor Server (Login)
         ↓ HTTPS
      Web App (Cadastro)
         ↓ HTTP
     .NET Web API (REST)
         ↓
    SQLite (Dados)
         ↓
    RabbitMQ (Evento: cliente.criado)
         ↓
   Worker Service (Consome)
         ↓
   E-mail/SMS (SendGrid/Twilio)
```

## 📦 Projetos na Solução

| Projeto | Tipo | Responsabilidade |
|---|---|---|
| `CadastroClientes.Domain` | Class Library | Entidades (Cliente) |
| `CadastroClientes.Application` | Class Library | Use Cases, DTOs, Validações, Interfaces |
| `CadastroClientes.Infrastructure` | Class Library | EF Core, Repositories, RabbitMQ |
| `CadastroClientes.Api` | Web API | Controllers REST + Swagger |
| `CadastroClientes.Web` | Blazor Server | Frontend com ASP.NET Identity |

## 🛠️ Tecnologias

- **.NET 8** (LTS)
- **Blazor Server** (UI com autenticação)
- **ASP.NET Identity** (Login/Senha)
- **Entity Framework Core 8** (ORM)
- **SQLite** (Banco de dados - gratuito)
- **RabbitMQ** (Mensageria - CloudAMQP)
- **FluentValidation** (Validação de dados)
- **Swagger** (Documentação API)

## 📋 Campos do Cliente

- `Id` (GUID, gerado automaticamente)
- `Nome` (obrigatório, 3-150 caracteres)
- `Email` (obrigatório, único, validado)
- `Celular` (obrigatório, formato BR: (XX) 9XXXX-XXXX)
- `DataCadastro` (automático, UTC)

## 🚀 Desenvolvimento Local

### Pré-requisitos
- Visual Studio 2022 ou VS Code
- .NET 8 SDK
- Docker (opcional, para RabbitMQ local)
- Git

### Setup

1. **Clone ou extraia o projeto**
   ```bash
   cd CadastroClientes
   ```

2. **Restaure as dependências**
   ```bash
   dotnet restore
   ```

3. **Configure o RabbitMQ local** (opcional)
   ```bash
   docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
   # Admin: http://localhost:15672 (guest:guest)
   ```

4. **Execute as migrações**
   ```bash
   cd CadastroClientes.Api
   dotnet ef database update
   
   cd ..\CadastroClientes.Web
   dotnet ef database update
   ```

5. **Inicie a API**
   ```bash
   cd CadastroClientes.Api
   dotnet run
   # Swagger em: https://localhost:7001/swagger/index.html
   ```

6. **Em outro terminal, inicie a Web**
   ```bash
   cd CadastroClientes.Web
   dotnet run
   # App em: https://localhost:7002
   ```

7. **Teste o app**
   - Acesse https://localhost:7002
   - Criar uma conta (Login > Register)
   - Fazer login
   - Cadastrar cliente
   - Conferir lista em tempo real
   - Swagger API: https://localhost:7001/swagger

## 🔧 Configuração

### appsettings.json (API)

```json
{
  "RabbitMQ": {
    "Uri": "amqp://user:pass@host:5672/"
  }
}
```

### appsettings.json (Web)

```json
{
  "ApiBaseUrl": "https://localhost:7001"  // Dev
}
```

### Azure Production

```json
{
  "ApiBaseUrl": "https://cadastroclientes-api.azurewebsites.net"
}
```

## 🔐 Validações

### Celular
- **Padrões aceitos:**
  - `(11) 99999-9999` (com parênteses)
  - `11 99999-9999` (com espaço)
  - `+55 11 99999-9999` (internacional)

### Email
- Validação RFC 5322
- Deve ser único no banco

### Nome
- Mínimo 3 caracteres
- Máximo 150 caracteres

## ☁️ Deploy no Azure

### Pré-requisitos Azure
- 2x Azure App Services (Linux, Free tier compatível)
- 1x RabbitMQ CloudAMQP (conta existente)

### 1. Criar Azure App Services

**API:**
```bash
# Criar resource group (se não existir)
az group create -n CadastroClientes -l centralus

# Criar App Service Plan (Linux, Free)
az appservice plan create -n cadastro-plan -g CadastroClientes --sku Free -l centralus

# Criar Web App para API
az webapp create -n cadastroclientes-api -g CadastroClientes -p cadastro-plan --runtime "DOTNET:8"

# Deploy API
cd CadastroClientes.Api
dotnet publish -c Release
az webapp deployment source config-zip -g CadastroClientes -n cadastroclientes-api --src ./bin/Release/net8.0/publish.zip
```

**Web:**
```bash
# Criar Web App para Frontend
az webapp create -n cadastroclientes-web -g CadastroClientes -p cadastro-plan --runtime "DOTNET:8"

# Deploy Web
cd CadastroClientes.Web
dotnet publish -c Release
az webapp deployment source config-zip -g CadastroClientes -n cadastroclientes-web --src ./bin/Release/net8.0/publish.zip
```

### 2. Configurar Variáveis de Ambiente

**API (Azure Portal > Configuração > Configurações do Aplicativo):**
```
ASPNETCORE_ENVIRONMENT = Production
RabbitMQ__Uri = amqp://seu_usuario:sua_senha@seu_host.cloudamqp.com:5672/sua_vhost
```

**Web (Azure Portal > Configuração > Configurações do Aplicativo):**
```
ASPNETCORE_ENVIRONMENT = Production
ApiBaseUrl = https://cadastroclientes-api.azurewebsites.net
```

### 3. Verificar Logs

```bash
# Log streaming da API
az webapp log tail -n cadastroclientes-api -g CadastroClientes

# Log streaming da Web
az webapp log tail -n cadastroclientes-web -g CadastroClientes
```

## 📡 Fluxo de Cadastro

1. **Usuário faz login** → Blazor (Identity)
2. **Preenche form** → Valida client-side + server-side
3. **Submit POST** → `/api/clientes`
4. **API valida** → Email único, formato celular
5. **Salva no SQLite** → Transaction segura
6. **Publica no RabbitMQ** → fila_cadastro_clientes
7. **Retorna 200 OK** → Lista atualiza em tempo real
8. **Worker Service** consome e envia e-mail (em outro projeto)

## 🐛 Troubleshooting

### "Email já está cadastrado"
- Email foi inserido com sucesso, tente com outro

### "Celular inválido"
- Use formato: `(XX) 9XXXX-XXXX`

### Erro 500 ao criar cliente
- Verifique logs: `az webapp log tail -n cadastroclientes-api`
- Confira RabbitMQ URI em appsettings

### Migrations não rodaram
- Execute manualmente:
  ```bash
  dotnet ef database update --project CadastroClientes.Api
  ```

## 📚 API REST (Swagger)

### POST /api/clientes
Cria novo cliente

**Request:**
```json
{
  "nome": "João Silva",
  "email": "joao@example.com",
  "celular": "(11) 99999-9999"
}
```

**Response (200):**
```json
{
  "mensagem": "Cliente criado com sucesso",
  "cliente": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "nome": "João Silva",
    "email": "joao@example.com",
    "celular": "(11) 99999-9999",
    "dataCadastro": "2024-06-10T15:30:45.123Z"
  }
}
```

### GET /api/clientes
Lista todos os clientes

**Response (200):**
```json
{
  "mensagem": "Clientes listados com sucesso",
  "clientes": [
    {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "nome": "João Silva",
      "email": "joao@example.com",
      "celular": "(11) 99999-9999",
      "dataCadastro": "2024-06-10T15:30:45.123Z"
    }
  ]
}
```

## 🔄 Estrutura de Mensagem RabbitMQ

**Fila:** `fila_cadastro_clientes`

**Corpo:**
```json
{
  "clienteId": "123e4567-e89b-12d3-a456-426614174000",
  "nome": "João Silva",
  "email": "joao@example.com",
  "celular": "(11) 99999-9999",
  "dataCadastro": "2024-06-10T15:30:45.123Z",
  "tipo": "cliente.criado"
}
```

## 📦 Packges & Dependências

```bash
# Application
dotnet add CadastroClientes.Application package FluentValidation

# Infrastructure
dotnet add CadastroClientes.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add CadastroClientes.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite
dotnet add CadastroClientes.Infrastructure package SQLitePCLRaw.bundle_e_sqlite3
dotnet add CadastroClientes.Infrastructure package RabbitMQ.Client

# API
dotnet add CadastroClientes.Api package Swashbuckle.AspNetCore

# Web
dotnet add CadastroClientes.Web package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add CadastroClientes.Web package Microsoft.EntityFrameworkCore.Sqlite
```

## 🎯 Próximos Passos

1. **Worker Service** para consumir RabbitMQ e enviar e-mails
2. **Integração SendGrid** para notificações
3. **Dashboard** de estatísticas
4. **Testes unitários** (xUnit)
5. **CI/CD** (GitHub Actions ou Azure Pipelines)

## 📝 Observações

- **SQLite**: Perfeito para demo. Em prod, considere SQL Server
- **Identity**: Usa SQLite. Em prod, considere Azure AD
- **RabbitMQ**: CloudAMQP é pago. Para testes, use local Docker
- **CORS**: Configurado para permitir qualquer origem (ajuste em prod)

## 📧 Suporte

Para erros ou dúvidas, verifique:
1. Logs do Azure (`az webapp log tail`)
2. Swagger da API (`/swagger`)
3. Console do navegador (F12)
4. Output da migration

## 📄 Licença

MIT - Livre para usar em entrevistas, portfolios e projetos pessoais.

---

**Criado em:** junho/2024  
**Versão:** 1.0.0  
**Autor:** Seu Nome  
**Status:** ✅ Production Ready
