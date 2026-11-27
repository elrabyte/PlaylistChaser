# syntax=docker/dockerfile:1
# Build from the repository root: docker build -f plugins/Downloader/Dockerfile .

# ---- Build -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY plugins/Downloader/Downloader.csproj Downloader/
RUN dotnet restore Downloader/Downloader.csproj
COPY plugins/Downloader/ Downloader/
RUN dotnet publish Downloader/Downloader.csproj -c Release -o /app --no-restore

# ---- Runtime ---------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
# yt-dlp is pinned to a release on purpose: YouTube changes break it every few weeks, so bump this and rebuild.
ARG YTDLP_VERSION=2026.11.23
RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg curl ca-certificates \
    && curl -fsSL -o /usr/local/bin/yt-dlp "https://github.com/yt-dlp/yt-dlp/releases/download/${YTDLP_VERSION}/yt-dlp_linux" \
    && chmod +x /usr/local/bin/yt-dlp \
    && apt-get purge -y curl && rm -rf /var/lib/apt/lists/* \
    && mkdir /config && chown 568:568 /config
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080 \
    CONFIG_DIR=/config \
    MUSIC_ROOT=/music
EXPOSE 8080
VOLUME /config
ENTRYPOINT ["dotnet", "Downloader.dll"]
