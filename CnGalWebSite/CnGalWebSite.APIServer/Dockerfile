#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R $APP_UID:root /home/app/.aspnet \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CnGalWebSite/CnGalWebSite.APIServer/CnGalWebSite.APIServer.csproj", "CnGalWebSite/CnGalWebSite.APIServer/"]
COPY ["CnGalWebSite/CnGalWebSite.Core/CnGalWebSite.Core.csproj", "CnGalWebSite/CnGalWebSite.Core/"]
COPY ["CnGalWebSite/CnGalWebSite.DataModel/CnGalWebSite.DataModel.csproj", "CnGalWebSite/CnGalWebSite.DataModel/"]
COPY ["CnGalWebSite/CnGalWebSite.DrawingBed.Helper/CnGalWebSite.DrawingBed.Helper.csproj", "CnGalWebSite/CnGalWebSite.DrawingBed.Helper/"]
COPY ["CnGalWebSite/CnGalWebSite.DrawingBed.Models/CnGalWebSite.DrawingBed.Models.csproj", "CnGalWebSite/CnGalWebSite.DrawingBed.Models/"]
COPY ["CnGalWebSite/CnGalWebSite.EventBus/CnGalWebSite.EventBus.csproj", "CnGalWebSite/CnGalWebSite.EventBus/"]
COPY ["CnGalWebSite/CnGalWebSite.Extensions/CnGalWebSite.Extensions.csproj", "CnGalWebSite/CnGalWebSite.Extensions/"]
COPY ["CnGalWebSite/CnGalWebSite.HealthCheck/CnGalWebSite.HealthCheck.csproj", "CnGalWebSite/CnGalWebSite.HealthCheck/"]
COPY ["CnGalWebSite/CnGalWebSite.Helper/CnGalWebSite.Helper.csproj", "CnGalWebSite/CnGalWebSite.Helper/"]
COPY ["CnGalWebSite/CnGalWebSite.TimedTask.Models/CnGalWebSite.TimedTask.Models.csproj", "CnGalWebSite/CnGalWebSite.TimedTask.Models/"]
RUN dotnet restore "CnGalWebSite/CnGalWebSite.APIServer/CnGalWebSite.APIServer.csproj"
COPY . .
WORKDIR "/src/CnGalWebSite/CnGalWebSite.APIServer"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "CnGalWebSite.APIServer.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CnGalWebSite.APIServer.dll"]
