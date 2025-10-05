# ====== 1) Restore/Build stage ======
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Скопируем только csproj, чтобы кэшировался restore
COPY ./AuthServicePlus.Api/AuthServicePlus.Api.csproj ./AuthServicePlus.Api/
COPY ./AuthServicePlus.Application/AuthServicePlus.Application.csproj ./AuthServicePlus.Application/
COPY ./AuthServicePlus.Domain/AuthServicePlus.Domain.csproj ./AuthServicePlus.Domain/
COPY ./AuthServicePlus.Infrastructure/AuthServicePlus.Infrastructure.csproj ./AuthServicePlus.Infrastructure/
COPY ./AuthServicePlus.Persistence/AuthServicePlus.Persistence.csproj ./AuthServicePlus.Persistence/

# Восстановление пакетов
RUN dotnet restore ./AuthServicePlus.Api/AuthServicePlus.Api.csproj

# Теперь копируем все исходники
COPY . .

# Release-сборка с ReadyToRun
RUN dotnet publish ./AuthServicePlus.Api/AuthServicePlus.Api.csproj \
    -c Release -o /app/publish \
    -p:PublishReadyToRun=true \
    -p:PublishSingleFile=false

# ====== 2) Runtime image ======
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Порты Kestrel (подправь при необходимости)
ENV ASPNETCORE_URLS="http://+:8080;https://+:8081"
EXPOSE 8080
EXPOSE 8081

# Безопасные дефолты ASP.NET Core в контейнере
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Опционально логи в консоль (Serilog читает)
ENV Serilog__WriteTo__0__Name=Console

# Для Postgres строку можно прокинуть через переменные:
ENV ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=AuthServicePlusDb;Username=postgres;Password=Rp_9i7g7;Include Error Detail=true;"

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "AuthServicePlus.Api.dll"]
