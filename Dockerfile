FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY OrdoWiki.slnx ./
COPY src/OrdoWiki.Web/OrdoWiki.Web.csproj src/OrdoWiki.Web/
COPY src/OrdoWiki.Data/OrdoWiki.Data.csproj src/OrdoWiki.Data/
COPY tests/OrdoWiki.Tests/OrdoWiki.Tests.csproj tests/OrdoWiki.Tests/
RUN dotnet restore src/OrdoWiki.Web/OrdoWiki.Web.csproj

COPY src/ src/
RUN dotnet publish src/OrdoWiki.Web/OrdoWiki.Web.csproj \
    -c Release \
    -o /app \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# The aspnet base image ships with a non-root user `app` at UID/GID 1654 (exposed as $APP_UID).
RUN mkdir -p /data/uploads /data/dpkeys && \
    chown -R 1654:1654 /app /data

COPY --from=build --chown=1654:1654 /app .
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OrdoWiki.Web.dll"]
