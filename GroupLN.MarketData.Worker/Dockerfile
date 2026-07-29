# ── Build ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY GroupLN.MarketData.Worker/GroupLN.MarketData.Worker.csproj GroupLN.MarketData.Worker/
COPY GroupLN.MarketData.Core/GroupLN.MarketData.Core.csproj GroupLN.MarketData.Core/
COPY GroupLN.MarketData.Infrastructure/GroupLN.MarketData.Infrastructure.csproj GroupLN.MarketData.Infrastructure/
COPY GroupLN.MarketData.Persistence/GroupLN.MarketData.Persistence.csproj GroupLN.MarketData.Persistence/
RUN dotnet restore GroupLN.MarketData.Worker/GroupLN.MarketData.Worker.csproj

COPY GroupLN.MarketData.Worker/ GroupLN.MarketData.Worker/
COPY GroupLN.MarketData.Core/ GroupLN.MarketData.Core/
COPY GroupLN.MarketData.Infrastructure/ GroupLN.MarketData.Infrastructure/
COPY GroupLN.MarketData.Persistence/ GroupLN.MarketData.Persistence/

RUN dotnet publish GroupLN.MarketData.Worker/GroupLN.MarketData.Worker.csproj -c Release -o /app --no-restore

# ── Runtime — Playwright-image met Chromium + alle Linux-dependencies ────
# Tag moet overeenkomen met de Microsoft.Playwright NuGet-versie (1.49.0).
FROM mcr.microsoft.com/playwright/dotnet:v1.49.0-noble AS final
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GroupLN.MarketData.Worker.dll"]
