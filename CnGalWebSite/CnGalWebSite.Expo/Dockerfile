# 请参阅 https://aka.ms/customizecontainer 以了解如何自定义调试容器，以及 Visual Studio 如何使用此 Dockerfile 生成映像以更快地进行调试。

# 此阶段用于在快速模式(默认为调试配置)下从 VS 运行时
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


# 此阶段用于生成服务项目
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CnGalWebSite/CnGalWebSite.Expo/CnGalWebSite.Expo.csproj", "CnGalWebSite/CnGalWebSite.Expo/"]
COPY ["CnGalWebSite/CnGalWebSite.Components/CnGalWebSite.Components.csproj", "CnGalWebSite/CnGalWebSite.Components/"]
COPY ["CnGalWebSite/CnGalWebSite.Core/CnGalWebSite.Core.csproj", "CnGalWebSite/CnGalWebSite.Core/"]
COPY ["CnGalWebSite/CnGalWebSite.DrawingBed.Helper/CnGalWebSite.DrawingBed.Helper.csproj", "CnGalWebSite/CnGalWebSite.DrawingBed.Helper/"]
COPY ["CnGalWebSite/CnGalWebSite.DrawingBed.Models/CnGalWebSite.DrawingBed.Models.csproj", "CnGalWebSite/CnGalWebSite.DrawingBed.Models/"]
COPY ["CnGalWebSite/CnGalWebSite.Extensions/CnGalWebSite.Extensions.csproj", "CnGalWebSite/CnGalWebSite.Extensions/"]
COPY ["CnGalWebSite/CnGalWebSite.DataModel/CnGalWebSite.DataModel.csproj", "CnGalWebSite/CnGalWebSite.DataModel/"]
COPY ["CnGalWebSite/CnGalWebSite.HealthCheck/CnGalWebSite.HealthCheck.csproj", "CnGalWebSite/CnGalWebSite.HealthCheck/"]
COPY ["CnGalWebSite/CnGalWebSite.Helper/CnGalWebSite.Helper.csproj", "CnGalWebSite/CnGalWebSite.Helper/"]
RUN dotnet restore "./CnGalWebSite/CnGalWebSite.Expo/CnGalWebSite.Expo.csproj"
COPY . .
WORKDIR "/src/CnGalWebSite/CnGalWebSite.Expo"

# 此阶段用于发布要复制到最终阶段的服务项目
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
# 不使用 --no-restore，确保 Blazor 框架脚本进入发布产物。
RUN dotnet publish "./CnGalWebSite.Expo.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 此阶段在生产中使用，或在常规模式下从 VS 运行时使用(在不使用调试配置时为默认值)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CnGalWebSite.Expo.dll"]
