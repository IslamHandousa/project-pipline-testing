# ─── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG VERSION=1.0.0
WORKDIR /src

COPY ["src/MyApp/MyApp.csproj", "src/MyApp/"]
RUN dotnet restore "src/MyApp/MyApp.csproj"

COPY . .
WORKDIR "/src/src/MyApp"
RUN dotnet build "MyApp.csproj" -c Release -o /app/build

# ─── Stage 2: Publish ─────────────────────────────────────────────────────────
FROM build AS publish
ARG VERSION=1.0.0
RUN dotnet publish "MyApp.csproj" -c Release -o /app/publish /p:UseAppHost=false \
    /p:AssemblyVersion=${VERSION} /p:FileVersion=${VERSION}

# ─── Stage 3: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
ARG VERSION=1.0.0

# OCI image labels for versioning / rollback traceability
LABEL org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.title="MyApp" \
      org.opencontainers.image.description=".NET 9 Login Application" \
      org.opencontainers.image.source="https://github.com/YOUR_USERNAME/YOUR_REPO"

WORKDIR /app

# Non-root user for security
RUN groupadd --system --gid 1001 appgroup \
 && useradd  --system --uid 1001 --gid appgroup appuser \
 && mkdir -p /app/data \
 && chown -R appuser:appgroup /app

COPY --from=publish --chown=appuser:appgroup /app/publish .

USER appuser

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    APP_VERSION=${VERSION} \
    ConnectionStrings__DefaultConnection="Data Source=/app/data/app.db"

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "MyApp.dll"]
