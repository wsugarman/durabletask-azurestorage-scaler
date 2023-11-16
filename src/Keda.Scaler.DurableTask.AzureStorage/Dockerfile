ARG COPY=false

# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/sdk:7.0.404-cbl-mariner2.0-amd64@sha256:9a4d0c05607d47287e3e3ac27431da7ba256d99bcf11244ba9134ff197fa7cd5 AS build
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

FROM mcr.microsoft.com/dotnet/sdk:7.0.404-cbl-mariner2.0-amd64@sha256:9a4d0c05607d47287e3e3ac27431da7ba256d99bcf11244ba9134ff197fa7cd5 AS users
RUN groupadd nonroot -g 2000 && \
    useradd -r -M -s /sbin/nologin -g nonroot -c nonroot nonroot -u 200

FROM scratch AS nonroot
COPY --from=users /etc/group /etc/group
COPY --from=users /etc/passwd /etc/passwd

FROM mcr.microsoft.com/dotnet/aspnet:7.0.14-cbl-mariner2.0-distroless-amd64@sha256:89b9e434c54a931c5c1e38817e94bb5e9e6da4d949c156eef43dd38eeb0d67ae AS runtime
COPY --from=nonroot / /
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
USER nonroot
EXPOSE 8080

FROM runtime AS web
COPY --from=publish-app /app /app
WORKDIR /app
ENTRYPOINT ["dotnet", "Keda.Scaler.DurableTask.AzureStorage.dll"]
