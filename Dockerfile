# ─────────────────────────────────────────────────────────────────────────────
# Stage 1 — Build
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# ── Copy SharedKernel projects (referenced by ATLAS.i18n) ─────────────────────
COPY ["../SharedKernel/Atlas.SharedKernel.Abstractions/Atlas.SharedKernel.Abstractions.csproj",
      "SharedKernel/Atlas.SharedKernel.Abstractions/"]
COPY ["../SharedKernel/Atlas.SharedKernel.Domain/Atlas.SharedKernel.Domain.csproj",
      "SharedKernel/Atlas.SharedKernel.Domain/"]
COPY ["../SharedKernel/Atlas.SharedKernel.Infrastructure/Atlas.SharedKernel.Infrastructure.csproj",
      "SharedKernel/Atlas.SharedKernel.Infrastructure/"]
COPY ["../SharedKernel_DB/Atlas.SharedKernel.Infrastructure.Database.csproj",
      "SharedKernel_DB/"]

# ── Copy ATLAS.i18n project files (for layer caching) ────────────────────────
COPY ["src/ATLAS.i18n.Domain/ATLAS.i18n.Domain.csproj",           "src/ATLAS.i18n.Domain/"]
COPY ["src/ATLAS.i18n.Application/ATLAS.i18n.Application.csproj", "src/ATLAS.i18n.Application/"]
COPY ["src/ATLAS.i18n.Infrastructure/ATLAS.i18n.Infrastructure.csproj", "src/ATLAS.i18n.Infrastructure/"]
COPY ["src/ATLAS.i18n.API/ATLAS.i18n.API.csproj",                 "src/ATLAS.i18n.API/"]

RUN dotnet restore "src/ATLAS.i18n.API/ATLAS.i18n.API.csproj"

# ── Copy all source ────────────────────────────────────────────────────────────
COPY . .

RUN dotnet publish "src/ATLAS.i18n.API/ATLAS.i18n.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ─────────────────────────────────────────────────────────────────────────────
# Stage 2 — Runtime
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN addgroup --system --gid 1001 atlasgroup \
 && adduser  --system --uid 1001 --ingroup atlasgroup atlasuser

RUN mkdir -p /app/logs && chown atlasuser:atlasgroup /app/logs

COPY --from=build --chown=atlasuser:atlasgroup /app/publish .

USER atlasuser

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=20s --retries=3 \
    CMD curl --fail http://localhost:8080/health/live || exit 1

ENV ASPNETCORE_URLS="http://+:8080"
ENV ASPNETCORE_ENVIRONMENT="Production"
ENV DOTNET_RUNNING_IN_CONTAINER="true"

ENTRYPOINT ["dotnet", "ATLAS.i18n.API.dll"]
