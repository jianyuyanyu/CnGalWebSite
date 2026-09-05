#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R $APP_UID:root /home/app/.aspnet \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CnGalWebSite/CnGalWebSite.TimedTask/CnGalWebSite.TimedTask.csproj", "CnGalWebSite/CnGalWebSite.TimedTask/"]
COPY ["CnGalWebSite/CnGalWebSite.Core/CnGalWebSite.Core.csproj", "CnGalWebSite/CnGalWebSite.Core/"]
COPY ["CnGalWebSite/CnGalWebSite.DataModel/CnGalWebSite.DataModel.csproj", "CnGalWebSite/CnGalWebSite.DataModel/"]
COPY ["CnGalWebSite/CnGalWebSite.EventBus/CnGalWebSite.EventBus.csproj", "CnGalWebSite/CnGalWebSite.EventBus/"]
COPY ["CnGalWebSite/CnGalWebSite.Extensions/CnGalWebSite.Extensions.csproj", "CnGalWebSite/CnGalWebSite.Extensions/"]
COPY ["CnGalWebSite/CnGalWebSite.HealthCheck/CnGalWebSite.HealthCheck.csproj", "CnGalWebSite/CnGalWebSite.HealthCheck/"]
COPY ["CnGalWebSite/CnGalWebSite.TimedTask.Models/CnGalWebSite.TimedTask.Models.csproj", "CnGalWebSite/CnGalWebSite.TimedTask.Models/"]
RUN dotnet restore "./CnGalWebSite/CnGalWebSite.TimedTask/CnGalWebSite.TimedTask.csproj"
COPY . .
WORKDIR "/src/CnGalWebSite/CnGalWebSite.TimedTask"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CnGalWebSite.TimedTask.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CnGalWebSite.TimedTask.dll"]
