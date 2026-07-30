# syntax=docker/dockerfile:1.7

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS restore
WORKDIR /src

COPY dashboardapi/dashboardapi.csproj dashboardapi/
RUN dotnet restore dashboardapi/dashboardapi.csproj

FROM restore AS publish
COPY dashboardapi/ dashboardapi/
RUN dotnet publish dashboardapi/dashboardapi.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl fontconfig fonts-liberation \
    && fc-cache --force \
    && fc-match Arial | grep --quiet "Liberation Sans" \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/data \
    && chown app:app /app/data

COPY --from=publish --chown=app:app /app/publish ./

EXPOSE 8080
USER app

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=5 \
    CMD curl --fail --silent --show-error http://127.0.0.1:8080/health > /dev/null || exit 1

ENTRYPOINT ["dotnet", "dashboardapi.dll"]
