#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0.3-alpine3.14@sha256:f37ba7086c2ee0a71db3f7a51130b81602767611c190365df8c5c370d449346a AS base
RUN set -x && \
    addgroup nonroot && \
    adduser -S -D -H -s /sbin/nologin -G nonroot -g nonroot nonroot
USER nonroot
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0.201-alpine3.14@sha256:c1a73b72c02e7b837e9a93030d545bc4181193e1bab1033364ed2d00986d78ff AS build
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
