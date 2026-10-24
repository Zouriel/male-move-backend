FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props global.json ./
COPY src ./src
RUN dotnet restore src/MaleMove.Api/MaleMove.Api.csproj
RUN dotnet publish src/MaleMove.Api/MaleMove.Api.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
WORKDIR /app
RUN groupadd --system --gid 10001 appgroup \
    && useradd --system --uid 10001 --gid appgroup --home-dir /app --no-create-home appuser \
    && mkdir -p /var/lib/male-move/private-permits \
    && chown -R appuser:appgroup /app /var/lib/male-move
COPY --from=build --chown=appuser:appgroup /out ./
USER 10001:10001
EXPOSE 8080
ENTRYPOINT ["dotnet", "MaleMove.Api.dll"]

