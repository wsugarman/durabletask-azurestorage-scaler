#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/sdk:9.0.305-azurelinux3.0@sha256:da0e8c8a48e65179440476bfafa04f9d431fafe729356a73e533c1d67b403265 AS build
ARG BUILD_CONFIGURATION=Release
COPY [".editorconfig", ".globalconfig", "Directory.Build.props", "Directory.Packages.props", "global.json", "NuGet.config", "/example/"]
COPY ["./tests/Keda.Scaler.Functions.Worker.DurableTask.Examples/", "/example/src/"]
WORKDIR /example/src
RUN dotnet restore "Keda.Scaler.Functions.Worker.DurableTask.Examples.csproj"
RUN dotnet build "Keda.Scaler.Functions.Worker.DurableTask.Examples.csproj" -c $BUILD_CONFIGURATION -warnaserror -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Keda.Scaler.Functions.Worker.DurableTask.Examples.csproj" -c $BUILD_CONFIGURATION -warnaserror -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated9.0-azurelinux3@sha256:16d59560175a26fc607cd124deac6f0886e47c5d83d48fbb6b0f04ce8573af40 AS runtime
RUN chown -R $APP_UID /azure-functions-host
ENV ASPNETCORE_URLS=http://+:8080 \
    AzureFunctionsJobHost__FileWatchingEnabled=false \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    AzureFunctionsJobHost__Logging__FileLoggingMode=Never \
    AzureWebJobsScriptRoot=/home/site/wwwroot \
    DOTNET_EnableDiagnostics=0 \
    LANG=en_US.UTF-8 \
    WEBSITE_HOSTNAME=localhost:8080
USER $APP_UID
EXPOSE 8080

FROM runtime AS func
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
