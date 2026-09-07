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

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CnGalWebSite/CnGalWebSite.IdentityServer.Admin.SSR/CnGalWebSite.IdentityServer.Admin.SSR.csproj", "CnGalWebSite/CnGalWebSite.IdentityServer.Admin.SSR/"]
COPY ["CnGalWebSite/CnGalWebSite.IdentityServer.Admin.Shared/CnGalWebSite.IdentityServer.Admin.Shared.csproj", "CnGalWebSite/CnGalWebSite.IdentityServer.Admin.Shared/"]
COPY ["CnGalWebSite/CnGalWebSite.Components/CnGalWebSite.Components.csproj", "CnGalWebSite/CnGalWebSite.Components/"]
COPY ["CnGalWebSite/CnGalWebSite.Core/CnGalWebSite.Core.csproj", "CnGalWebSite/CnGalWebSite.Core/"]
COPY ["CnGalWebSite/CnGalWebSite.DataModel/CnGalWebSite.DataModel.csproj", "CnGalWebSite/CnGalWebSite.DataModel/"]
COPY ["CnGalWebSite/CnGalWebSite.DrawingBed.Helper/CnGalWebSite.DrawingBed.Helper.csproj", "CnGalWebSite/CnGalWebSite.DrawingBed.Helper/"]
COPY ["CnGalWebSite/CnGalWebSite.DrawingBed.Models/CnGalWebSite.DrawingBed.Models.csproj", "CnGalWebSite/CnGalWebSite.DrawingBed.Models/"]
COPY ["CnGalWebSite/CnGalWebSite.Extensions/CnGalWebSite.Extensions.csproj", "CnGalWebSite/CnGalWebSite.Extensions/"]
COPY ["CnGalWebSite/CnGalWebSite.IdentityServer.Models/CnGalWebSite.IdentityServer.Models.csproj", "CnGalWebSite/CnGalWebSite.IdentityServer.Models/"]
RUN dotnet restore "CnGalWebSite/CnGalWebSite.IdentityServer.Admin.SSR/CnGalWebSite.IdentityServer.Admin.SSR.csproj"
COPY . .
WORKDIR "/src/CnGalWebSite/CnGalWebSite.IdentityServer.Admin.SSR"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
# 不使用 --no-restore，确保 Blazor 框架脚本进入发布产物。
RUN dotnet publish "CnGalWebSite.IdentityServer.Admin.SSR.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CnGalWebSite.IdentityServer.Admin.SSR.dll"]
