#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/sdk:8.0.100-cbl-mariner2.0-amd64@sha256:4e3c2fe37a44ac88e96ae77841aea3caf9264da19c3a863513de570c22df9f3a AS build
ARG BUILD_CONFIGURATION=Release
COPY [".editorconfig", ".globalconfig", "Directory.Build.props", "Directory.Packages.props", "global.json", "NuGet.config", "/example/"]
COPY ["./tests/Keda.Scaler.Functions.Worker.DurableTask.Examples/", "/example/src/"]
WORKDIR /example/src
RUN dotnet restore "Keda.Scaler.Functions.Worker.DurableTask.Examples.csproj"
RUN dotnet build "Keda.Scaler.Functions.Worker.DurableTask.Examples.csproj" -c $BUILD_CONFIGURATION -warnaserror -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Keda.Scaler.Functions.Worker.DurableTask.Examples.csproj" -c $BUILD_CONFIGURATION -warnaserror -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/sdk:8.0.100-cbl-mariner2.0-amd64@sha256:4e3c2fe37a44ac88e96ae77841aea3caf9264da19c3a863513de570c22df9f3a AS users
RUN groupadd nonroot -g 2000 && \
    useradd -r -M -s /sbin/nologin -g nonroot -c nonroot nonroot -u 1000

FROM scratch AS nonroot
COPY --from=users /etc/group /etc/group
COPY --from=users /etc/passwd /etc/passwd

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated7.0-mariner@sha256:c47edc0829c85c92892abf8d2137f4b77a2586b8d7d5a1d3a91c4fd706886388 AS runtime
COPY --from=nonroot / /
RUN chown -R nonroot:nonroot /azure-functions-host
ENV ASPNETCORE_URLS=http://+:8080 \
    AzureFunctionsJobHost__FileWatchingEnabled=false \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    AzureFunctionsJobHost__Logging__FileLoggingMode=Never \
    AzureWebJobsFeatureFlags=EnableWorkerIndexing \
    AzureWebJobsScriptRoot=/home/site/wwwroot \
    DOTNET_EnableDiagnostics=0 \
    LANG=en_US.UTF-8 \
    LC_ALL=en_US.UTF-8 \
    WEBSITE_HOSTNAME=localhost:8080
USER nonroot
EXPOSE 8080

FROM runtime AS func
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
