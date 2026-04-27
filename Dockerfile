# ─────────────────────────────────────────────────────────────────────────────
# Stage 1 — SDK build
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first to leverage Docker layer caching
COPY ["Source/ATLAS.i18n.Domain/ATLAS.i18n.Domain.csproj",                  "Source/ATLAS.i18n.Domain/"]
COPY ["Source/ATLAS.i18n.Application/ATLAS.i18n.Application.csproj",        "Source/ATLAS.i18n.Application/"]
COPY ["Source/ATLAS.i18n.Infrastructure/ATLAS.i18n.Infrastructure.csproj",  "Source/ATLAS.i18n.Infrastructure/"]
COPY ["Source/ATLAS.i18n.API/ATLAS.i18n.API.csproj",                        "Source/ATLAS.i18n.API/"]

# Restore dependencies
RUN dotnet restore "Source/ATLAS.i18n.API/ATLAS.i18n.API.csproj"

# Copy the full source
COPY . .

# Build and publish in Release mode
RUN dotnet publish "Source/ATLAS.i18n.API/ATLAS.i18n.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ─────────────────────────────────────────────────────────────────────────────
# Stage 2 — Runtime image (lean ASP.NET Core runtime)
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create a non-root user for security
RUN addgroup --system --gid 1001 atlasgroup \
 && adduser  --system --uid 1001 --ingroup atlasgroup atlasuser

# Create log directory with correct ownership
RUN mkdir -p /app/logs && chown atlasuser:atlasgroup /app/logs

# Copy published output
COPY --from=build --chown=atlasuser:atlasgroup /app/publish .

USER atlasuser

# Service listens on port 8080 (non-privileged)
EXPOSE 8080

# Health-check — polls the /health/live endpoint every 30 seconds
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl --fail http://localhost:8080/health/live || exit 1

ENV ASPNETCORE_URLS="http://+:8080"
ENV ASPNETCORE_ENVIRONMENT="Production"
ENV DOTNET_RUNNING_IN_CONTAINER="true"

ENTRYPOINT ["dotnet", "ATLAS.i18n.API.dll"]
