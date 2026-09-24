# syntax=docker/dockerfile:1

# ---- Build stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (better layer caching): only the Web + Test project files are needed to build
# and run the Web project; the legacy PlaylistChaser.DB (SSDT/.sqlproj) project is intentionally
# NOT part of this image - it requires Visual Studio/SSDT tooling and isn't used by the
# Postgres/Docker path (see AGENTS.md).
COPY PlaylistChaser.Web/PlaylistChaser.Web.csproj PlaylistChaser.Web/
RUN dotnet restore PlaylistChaser.Web/PlaylistChaser.Web.csproj

COPY PlaylistChaser.Web/ PlaylistChaser.Web/
RUN dotnet publish PlaylistChaser.Web/PlaylistChaser.Web.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PlaylistChaser.Web.dll"]
