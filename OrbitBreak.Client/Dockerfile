# ── Stage 1: build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies first (layer-cached unless .csproj changes)
COPY OrbitBreak.Client/OrbitBreak.Client.csproj OrbitBreak.Client/
RUN dotnet restore OrbitBreak.Client/OrbitBreak.Client.csproj

# Copy source and publish
COPY OrbitBreak.Client/ OrbitBreak.Client/
RUN dotnet publish OrbitBreak.Client/OrbitBreak.Client.csproj \
      -c Release \
      -o /publish \
      --no-restore

# ── Stage 2: serve ───────────────────────────────────────────────────────────
FROM nginx:1.27-alpine AS serve
COPY nginx.conf /etc/nginx/nginx.conf
COPY --from=build /publish/wwwroot /usr/share/nginx/html
EXPOSE 80
