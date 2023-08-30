ARG SIGN=false

# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/sdk:7.0.400-cbl-mariner2.0-amd64@sha256:739ddaf84935f7a4756f14ca18ae6cc49a3bb57b56c953c7a9292e918d04e8bd AS build
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

FROM scratch AS publish-signed-false
COPY --from=publish /app/publish /app

# Azure Code Signing requires Windows
FROM mcr.microsoft.com/powershell:windowsservercore-ltsc2022@sha256:af96b19499cb704ae815044e06dc3195b33f4ead7bc5369aea44a3fc28d2d502 AS publish-signed-true
COPY --from=publish /app/publish /app
USER ContainerUser
RUN mkdir -p /signing && \
    echo "../app/Keda.Scaler.DurableTask.AzureStorage.dll" >> /signing/catalog.txt && \
    echo "../app/Keda.Scaler.DurableTask.AzureStorage.exe" >> /signing/catalog.txt
RUN ["pwsh", "-c", "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi; Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'; Remove-Item .\AzureCLI.msi"]
RUN ["pwsh", "-c", "Install-Module -Name AzureCodeSigning -RequiredVersion 0.2.21 -Force -Repository PSGallery"]
RUN --mount=type=secret,id=credentials,target=/Users/ContainerUser/.azure/msal_token_cache.json \
  --mount=type=secret,id=signing_parameters,target=/signing/parameters.json \
  ["pwsh", "-c", "$params = Get-Content -Path '/signing/parameters.json' | ConvertFrom-Json -AsHashTable; Invoke-AzureCodeSigning -FilesCatalog '/sign/catalog.txt' @params }"]

FROM publish-signed-${SIGN} AS app

FROM mcr.microsoft.com/dotnet/sdk:7.0.400-cbl-mariner2.0-amd64@sha256:739ddaf84935f7a4756f14ca18ae6cc49a3bb57b56c953c7a9292e918d04e8bd AS users
RUN groupadd nonroot -g 2000 && \
    useradd -r -M -s /sbin/nologin -g nonroot -c nonroot nonroot -u 200

FROM scratch AS nonroot
COPY --from=users /etc/group /etc/group
COPY --from=users /etc/passwd /etc/passwd

FROM mcr.microsoft.com/dotnet/aspnet:7.0.10-cbl-mariner2.0-distroless-amd64@sha256:b1229b00dace247e7fd1cd5fc8614c742addc64131df075f6cd0ffe3f7e02989 AS runtime
COPY --from=nonroot / /
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
USER nonroot
EXPOSE 8080

FROM runtime AS web
COPY --from=app /app /app
WORKDIR /app
ENTRYPOINT ["dotnet", "Keda.Scaler.DurableTask.AzureStorage.dll"]
