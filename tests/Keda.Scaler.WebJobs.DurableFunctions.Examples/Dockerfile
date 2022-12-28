# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
# Build using the latest .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:7.0.101-bullseye-slim-amd64@sha256:da34b595be986af8c183209c405d8c5d1ebecb11ae1aceef6a6297896ab71ba3 AS build
ARG BUILD_CONFIGURATION=Release

# Azure Functions v4 targets .NET 6
RUN set -x && \
    apk update && \
    apk add --no-cache bash && \
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin -Channel 6.0 -InstallDir /usr/share/dotnet

COPY [".editorconfig", ".globalconfig", "Directory.Build.props", "Directory.Packages.props", "global.json", "NuGet.config", "/example/"]
COPY ["./tests/Keda.Scaler.WebJobs.DurableFunctions.Examples/", "/example/src/"]
WORKDIR /example/src
RUN dotnet restore "Keda.Scaler.WebJobs.DurableFunctions.Examples.csproj"
RUN dotnet build "Keda.Scaler.WebJobs.DurableFunctions.Examples.csproj" -c $BUILD_CONFIGURATION -warnaserror -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Keda.Scaler.WebJobs.DurableFunctions.Examples.csproj" -c $BUILD_CONFIGURATION -warnaserror -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/azure-functions/dotnet:4 AS runtime
RUN groupadd nonroot && \
    useradd -r -M -s /sbin/nologin -g nonroot -c nonroot nonroot && \
    chown -R nonroot:nonroot /azure-functions-host
ENV ASPNETCORE_URLS=http://+:8080 \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    AzureWebJobsScriptRoot=/home/site/wwwroot
USER nonroot
EXPOSE 8080

FROM runtime AS func
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
