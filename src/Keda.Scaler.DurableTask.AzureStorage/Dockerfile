#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0.6-alpine3.14@sha256:cde5d41b0cd6d9ca27151fb5d35f67267cdc0d465a4e92081dd0d00bbe195156 AS base
RUN set -x && \
    addgroup nonroot && \
    adduser -S -D -H -s /sbin/nologin -G nonroot -g nonroot nonroot
USER nonroot
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0.301-alpine3.14@sha256:fc3b535877460e7097af7dbbf64ce0815148206fa086340cd4c4db9ec064115e AS build
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
