# Imagem única: a API .NET serve também o front Angular (contexto de build = raiz do repositório).

# 1) Front
FROM node:22-alpine AS front
WORKDIR /client

COPY client/package.json client/package-lock.json ./
RUN npm ci

COPY client/ .
RUN npx ng build --configuration production

# 2) API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api
WORKDIR /src

COPY Server/ .

RUN dotnet restore LocadoraVeiculos2026/LocadoraDeVeiculos.WebApi.csproj \
    && dotnet publish LocadoraVeiculos2026/LocadoraDeVeiculos.WebApi.csproj \
        --configuration Release --no-restore --output /app

# 3) Imagem final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=api /app .
COPY --from=front /client/dist/*/browser ./wwwroot

# Atrás de um proxy (Azure, nginx) o HTTPS é terminado fora do container.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 8080

ENTRYPOINT ["dotnet", "LocadoraDeVeiculos.WebApi.dll"]
