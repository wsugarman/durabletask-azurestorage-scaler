# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
# Build using the latest .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:7.0.304-bullseye-slim-amd64@sha256:88ebfe8962f5979d6891de0b2403aa4a6e1bf3e0c5776767d2731bb2efc02a9a AS build
ARG BUILD_CONFIGURATION=Release

# Azure Functions v4 targets .NET 6
RUN set -x && \
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
RUN groupadd nonroot -g 2000 && \
    useradd -r -M -s /sbin/nologin -g nonroot -c nonroot nonroot -u 1000 && \
    chown -R nonroot:nonroot /azure-functions-host
ENV ASPNETCORE_URLS=http://+:8080 \
    AzureFunctionsJobHost__FileWatchingEnabled=false \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    AzureFunctionsJobHost__Logging__FileLoggingMode=Never \
    AzureWebJobsScriptRoot=/home/site/wwwroot \
    DOTNET_EnableDiagnostics=0 \
    FUNCTIONS_WORKER_RUNTIME=dotnet \
    LANG=en_US.UTF-8 \
    LC_ALL=en_US.UTF-8
USER nonroot
EXPOSE 8080

FROM runtime AS func
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
