# syntax=docker/dockerfile:1.7

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src ./src
COPY tests ./tests
COPY README.md ./

RUN dotnet restore ./src/PiholeDnsPropagate.Worker/PiholeDnsPropagate.Worker.csproj
RUN dotnet publish ./src/PiholeDnsPropagate.Worker/PiholeDnsPropagate.Worker.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish ./

# Expose health endpoint port (configurable via ApplicationOptions)
EXPOSE 8080

ENTRYPOINT ["dotnet", "PiholeDnsPropagate.Worker.dll"]
