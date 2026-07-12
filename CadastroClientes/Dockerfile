FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CadastroClientes.Domain/CadastroClientes.Domain.csproj CadastroClientes.Domain/
COPY CadastroClientes.Application/CadastroClientes.Application.csproj CadastroClientes.Application/
COPY CadastroClientes.Infrastructure/CadastroClientes.Infrastructure.csproj CadastroClientes.Infrastructure/
COPY CadastroClientes.Api/CadastroClientes.Api.csproj CadastroClientes.Api/
COPY CadastroClientes.Worker/CadastroClientes.Worker.csproj CadastroClientes.Worker/
COPY CadastroClientes.Web/CadastroClientes.Web.csproj CadastroClientes.Web/

RUN dotnet restore CadastroClientes.Api/CadastroClientes.Api.csproj
RUN dotnet restore CadastroClientes.Web/CadastroClientes.Web.csproj

COPY . .
RUN dotnet publish CadastroClientes.Api/CadastroClientes.Api.csproj -c Release -o /app/api
RUN dotnet publish CadastroClientes.Web/CadastroClientes.Web.csproj -c Release -o /app/web

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/api ./api/
COPY --from=build /app/web ./web/
ENTRYPOINT ["dotnet"]
