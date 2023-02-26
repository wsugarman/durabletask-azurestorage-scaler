# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/sdk:7.0.103-cbl-mariner2.0-amd64@sha256:386a66f71d7eba72aef14c44b559d227d78c62de51bd758d6e2cf95cca6a74f3 AS build
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
  -p:ContinuousIntegrationBuild=$CONTINUOUS_INTEGRATION_BUILD \
  -warnaserror \
  -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:7.0.103-cbl-mariner2.0-amd64@sha256:386a66f71d7eba72aef14c44b559d227d78c62de51bd758d6e2cf95cca6a74f3 AS users
RUN groupadd nonroot -g 2000 && \
    useradd -r -M -s /sbin/nologin -g nonroot -c nonroot nonroot -u 200

FROM scratch AS nonroot
COPY --from=users /etc/group /etc/group
COPY --from=users /etc/passwd /etc/passwd

FROM mcr.microsoft.com/dotnet/aspnet:7.0.3-cbl-mariner2.0-distroless-amd64@sha256:e81bf6b1f98ed9dbb326dccd40b49b7b6755ee52b60d6f24dda22f39c53225fe AS runtime
COPY --from=nonroot / /
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
USER nonroot
EXPOSE 8080

FROM runtime AS web
WORKDIR /app
COPY --from=publish ["/app/publish", "./"]
ENTRYPOINT ["dotnet", "Keda.Scaler.DurableTask.AzureStorage.dll"]
