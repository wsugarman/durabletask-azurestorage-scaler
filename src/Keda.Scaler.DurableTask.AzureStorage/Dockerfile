#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0.2-alpine3.14@sha256:7f9385ad945d06b35bc93b3d91a2be3e65a921ff0c872f6982b01b27088c4149 AS base
RUN set -x && \
    addgroup nonroot && \
    adduser -S -D -H -s /sbin/nologin -G nonroot -g nonroot nonroot
USER nonroot
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0.200-alpine3.14@sha256:d79b8ba0b0544c2545083876c08550d4cb3547b9118d6f77cd95a906311597cb AS build
WORKDIR /src
COPY ["Keda.Scaler.DurableTask.AzureStorage.csproj", "."]
RUN dotnet restore "./Keda.Scaler.DurableTask.AzureStorage.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Keda.Scaler.DurableTask.AzureStorage.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Keda.Scaler.DurableTask.AzureStorage.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Keda.Scaler.DurableTask.AzureStorage.dll"]
