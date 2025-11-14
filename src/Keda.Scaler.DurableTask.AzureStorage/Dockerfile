ARG COPY=false

# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/sdk:9.0.307-azurelinux3.0@sha256:a7801f58b9f4c45d4132fe9502850165ff573b7f0328421d98dd925796d5aa02 AS build
ARG BUILD_CONFIGURATION=Release
ARG CONTINUOUS_INTEGRATION_BUILD=false
ARG ASSEMBLY_VERSION=1.0.0
ARG FILE_VERSION=1.0.0.0

COPY [".editorconfig", ".globalconfig", "Directory.Build.props", "Directory.Packages.props", "global.json", "NuGet.config", "/scaler/"]
COPY ["./src/Directory.Build.props", "./src/Keda.Scaler.DurableTask.AzureStorage/", "/scaler/src/"]
WORKDIR /scaler/src
RUN dotnet restore "Keda.Scaler.DurableTask.AzureStorage.csproj"
RUN dotnet build "Keda.Scaler.DurableTask.AzureStorage.csproj" \
  -c $BUILD_CONFIGURATION \
  "-p:ContinuousIntegrationBuild=$CONTINUOUS_INTEGRATION_BUILD;AssemblyVersion=$ASSEMBLY_VERSION;FileVersion=$FILE_VERSION;InformationalVersion=$FILE_VERSION" \
  -warnaserror \
  -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG CONTINUOUS_INTEGRATION_BUILD=false
ARG ASSEMBLY_VERSION=1.0.0
ARG FILE_VERSION=1.0.0.0

RUN dotnet publish "Keda.Scaler.DurableTask.AzureStorage.csproj" \
  -c $BUILD_CONFIGURATION \
  "-p:ContinuousIntegrationBuild=$CONTINUOUS_INTEGRATION_BUILD;AssemblyVersion=$ASSEMBLY_VERSION;FileVersion=$FILE_VERSION;InformationalVersion=$FILE_VERSION" \
  -warnaserror \
  -o /app/publish

FROM scratch AS publish-copy-false
COPY --from=publish /app/publish /app

# Optionally, binaries can be taken directly from the Docker build context
FROM scratch AS publish-copy-true
ARG PUBLISH_DIRECTORY
COPY ${PUBLISH_DIRECTORY} /app

FROM publish-copy-${COPY} AS publish-app

FROM mcr.microsoft.com/dotnet/aspnet:9.0.11-azurelinux3.0-distroless@sha256:09cab75fcc0abdf1cbee79359843295c2ce82d4d148d957fbf4fec847806411c AS runtime
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_USE_POLLING_FILE_WATCHER=true
USER $APP_UID
EXPOSE 8080

FROM runtime AS web
COPY --from=publish-app /app /app
WORKDIR /app
ENTRYPOINT ["dotnet", "Keda.Scaler.DurableTask.AzureStorage.dll"]
