# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /source
COPY global.json Directory.Build.props .editorconfig ./
COPY src/ ./src/
RUN dotnet restore src/MatchingSports.Api/MatchingSports.Api.csproj --locked-mode \
    && dotnet restore src/MatchingSports.Migrator/MatchingSports.Migrator.csproj --locked-mode
RUN dotnet publish src/MatchingSports.Api/MatchingSports.Api.csproj -c Release --no-restore -o /publish/api /p:UseAppHost=false \
    && dotnet publish src/MatchingSports.Migrator/MatchingSports.Migrator.csproj -c Release --no-restore -o /publish/migrator /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/keys && chown "$APP_UID" /app/keys
USER $APP_UID

FROM runtime AS migrator
COPY --from=build /publish/migrator/ ./
ENTRYPOINT ["dotnet", "MatchingSports.Migrator.dll"]

FROM runtime AS api
COPY --from=build /publish/api/ ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MatchingSports.Api.dll"]
