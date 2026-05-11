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

RUN groupadd --system --gid 1000 ordowiki && \
    useradd --system --uid 1000 --gid ordowiki ordowiki && \
    mkdir -p /data/uploads /data/dpkeys && \
    chown -R ordowiki:ordowiki /app /data

COPY --from=build --chown=ordowiki:ordowiki /app .
USER ordowiki

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OrdoWiki.Web.dll"]
