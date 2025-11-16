# Est�gio 1: Build (SDK)
# Usamos a imagem do SDK do .NET 8 para compilar o projeto
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia o .csproj e restaura as depend�ncias primeiro
# Isso aproveita o cache do Docker se as depend�ncias n�o mudarem
COPY *.csproj .
RUN dotnet restore

# Copia o resto do c�digo-fonte e faz o publish
COPY . .
RUN dotnet publish Cortex.csproj -c Release -o /app/publish

# Est�gio 2: Final (Runtime)
# Usamos a imagem de runtime do ASP.NET, que � muito menor
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# O Cloud Run injeta a vari�vel de ambiente PORT. 
# A imagem base do ASP.NET j� est� configurada para ouvi-la (porta 8080 por padr�o).
# Se sua app n�o rodar na 8080, descomente e ajuste:
# ENV ASPNETCORE_URLS=http://+:8080

# ENTRYPOINT - Altere "SeuProjeto.dll" para o nome da DLL gerada
# Geralmente � o nome do seu arquivo .csproj
ENTRYPOINT ["dotnet", "Cortex.dll"]