# Multi-stage Dockerfile.
# Targets: base (runtime) -> build -> publish -> dev (hot reload compose override) -> final (producción).

# Runtime base for final self-contained production image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-bookworm-slim AS base

ARG APP_UID=1000

ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_URLS=http://+:8080

WORKDIR /app

EXPOSE 8080
EXPOSE 8081

# Build stage.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["Papasur.Api/Papasur.Api.csproj", "Papasur.Api/"]
COPY ["Papasur.Application/Papasur.Application.csproj", "Papasur.Application/"]
COPY ["Papasur.Domain/Papasur.Domain.csproj", "Papasur.Domain/"]
COPY ["Papasur.Infrastructure/Papasur.Infrastructure.csproj", "Papasur.Infrastructure/"]

RUN dotnet restore "Papasur.Api/Papasur.Api.csproj" -r linux-x64

COPY . .

WORKDIR /src/Papasur.Api

RUN dotnet build "Papasur.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    -r linux-x64


# Publish stage.
FROM build AS publish

ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "Papasur.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false


# Development stage for docker-compose.override.yml.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev

WORKDIR /workspace

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates unzip procps \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /vsdbg \
    && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg

EXPOSE 8080
EXPOSE 8081

CMD ["dotnet", "watch", "--project", "Papasur.Api/Papasur.Api.csproj", "run", "--urls", "http://0.0.0.0:8080"]


# Final production/runtime stage.
FROM base AS final

ARG APP_UID=1000

WORKDIR /app

COPY --from=publish /app/publish .

USER root

RUN chown -R $APP_UID:$APP_UID /app

USER $APP_UID

ENTRYPOINT ["./Papasur.Api"]
