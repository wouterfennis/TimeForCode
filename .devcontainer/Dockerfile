FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0

# Remove stale/unsigned Yarn repo if present to prevent apt update failures in features.
RUN rm -f /etc/apt/sources.list.d/yarn.list /etc/apt/sources.list.d/yarn.list.save /etc/apt/sources.list.d/yarn*.list || true
