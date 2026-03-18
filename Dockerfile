# ── Stage 1: Restore & Build ─────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BookLibrary.sln .
COPY BookLibrary.csproj .
COPY BookLibrary.Tests/BookLibrary.Tests.csproj BookLibrary.Tests/

RUN dotnet restore BookLibrary.sln

COPY . .
RUN dotnet build BookLibrary.csproj --no-restore --configuration Release

# ── Stage 2: Test ─────────────────────────────────────────────────────────────
FROM build AS test
RUN dotnet test BookLibrary.Tests/BookLibrary.Tests.csproj --no-build --configuration Release --verbosity normal

# ── Stage 3: Publish ─────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish BookLibrary.csproj --no-build --configuration Release -o /app/publish

# ── Stage 4: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BookLibrary.dll"]
