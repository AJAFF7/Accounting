# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["nuget.config", "."]
COPY ["SoftMax.Accounting.csproj", "."]

RUN dotnet restore "./SoftMax.Accounting.csproj" \
    --configfile "nuget.config" \
    --no-http-cache

COPY . .
WORKDIR "/src/."

RUN dotnet build "./SoftMax.Accounting.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "./SoftMax.Accounting.csproj" -c $BUILD_CONFIGURATION -r linux-x64 --self-contained false -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=2 \
    CMD curl -f http://localhost/health || exit 1
ENTRYPOINT ["dotnet", "SoftMax.Accounting.dll"]