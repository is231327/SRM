FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SRMShared/SRMShared.csproj SRMShared/
COPY SRMApp/SRMApp.csproj SRMApp/
RUN dotnet restore SRMApp/SRMApp.csproj

COPY . .
# Restore again with the complete source tree available. Razor/Blazor static web
# assets are discovered from project content and must be present during restore.
RUN dotnet publish SRMApp/SRMApp.csproj -c Release -o /app/publish /p:UseAppHost=false /p:ContinuousIntegrationBuild=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "SRMApp.dll"]
