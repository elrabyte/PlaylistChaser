# syntax=docker/dockerfile:1

# 1. the React app
FROM node:22-alpine AS client
WORKDIR /client
COPY client/package.json client/package-lock.json ./
RUN npm ci
COPY client/ ./
RUN npm run build

# 2. the .NET server, with the React app as its static files
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS server
WORKDIR /src
COPY server/PlaylistChaser/PlaylistChaser.csproj PlaylistChaser/
RUN dotnet restore PlaylistChaser/PlaylistChaser.csproj
COPY server/PlaylistChaser/ PlaylistChaser/
RUN dotnet publish PlaylistChaser/PlaylistChaser.csproj -c Release -o /app --no-restore
COPY --from=client /client/dist /app/wwwroot

# 3. runtime: non-root user (UID 1654), listens on 8080, keeps encryption keys in /data
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=server /app .
RUN mkdir /data && chown $APP_UID /data
USER $APP_UID
EXPOSE 8080
VOLUME /data
ENTRYPOINT ["dotnet", "PlaylistChaser.dll"]
