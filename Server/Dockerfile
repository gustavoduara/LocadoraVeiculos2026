# Imagem da API (contexto de build = pasta Server).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore LocadoraVeiculos2026/LocadoraDeVeiculos.WebApi.csproj \
    && dotnet publish LocadoraVeiculos2026/LocadoraDeVeiculos.WebApi.csproj \
        --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app .

# A API escuta em HTTP dentro do container; o HTTPS fica a cargo de quem publica na frente dela.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LocadoraDeVeiculos.WebApi.dll"]
