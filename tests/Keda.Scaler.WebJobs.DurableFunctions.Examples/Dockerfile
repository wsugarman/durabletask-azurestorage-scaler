#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/azure-functions/dotnet:4 AS runtime
ENV ASPNETCORE_URLS=http://+:8080 \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    AzureWebJobsScriptRoot=/home/site/wwwroot \
    LANG=en_US.UTF-8 \
    LC_ALL=en_US.UTF-8
RUN groupadd nonroot && \
    useradd -r -M -s /sbin/nologin -g nonroot -c nonroot nonroot && \
    chown -R nonroot:nonroot /azure-functions-host
USER nonroot
EXPOSE 8080

# Build using the latest .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:7.0.101-alpine3.17 AS build
ARG BUILD_CONFIGURATION=Release

# Azure Functions v4 targets .NET 6
RUN set -x && \
    apk update && \
    apk add --no-cache bash && \
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin -Channel 6.0 -InstallDir /usr/share/dotnet

WORKDIR /example
COPY [".editorconfig", ".globalconfig", "Directory.Build.props", "Directory.Packages.props", "global.json", "NuGet.config", "."]
WORKDIR /example/src
COPY ["./tests/Keda.Scaler.WebJobs.DurableFunctions.Examples/*", "."]
RUN dotnet restore "Keda.Scaler.WebJobs.DurableFunctions.Examples.csproj"
RUN dotnet build "Keda.Scaler.WebJobs.DurableFunctions.Examples.csproj" -c $BUILD_CONFIGURATION -warnaserror -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Keda.Scaler.WebJobs.DurableFunctions.Examples.csproj" -c $BUILD_CONFIGURATION -warnaserror -o /app/publish /p:UseAppHost=false

FROM runtime AS func
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
