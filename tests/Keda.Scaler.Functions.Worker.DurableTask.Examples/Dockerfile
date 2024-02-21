#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/sdk:8.0.201-cbl-mariner2.0-amd64@sha256:33d91a9dd7c3c5872d84a79878002bc6dcb17adb335ac32a41fff38e50e5f60d AS build
ARG BUILD_CONFIGURATION=Release
COPY [".editorconfig", ".globalconfig", "Directory.Build.props", "Directory.Packages.props", "global.json", "NuGet.config", "/example/"]
COPY ["./tests/Keda.Scaler.Functions.Worker.DurableTask.Examples/", "/example/src/"]
WORKDIR /example/src
RUN dotnet restore "Keda.Scaler.Functions.Worker.DurableTask.Examples.csproj"
RUN dotnet build "Keda.Scaler.Functions.Worker.DurableTask.Examples.csproj" -c $BUILD_CONFIGURATION -warnaserror -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Keda.Scaler.Functions.Worker.DurableTask.Examples.csproj" -c $BUILD_CONFIGURATION -warnaserror -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0-mariner@sha256:3dd610034bc961733c3db9425d0d8a7c506d2c2bda7a064b7968451a47775373 AS runtime
RUN chown -R $APP_UID /azure-functions-host
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
USER $APP_UID
EXPOSE 8080

FROM runtime AS func
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
