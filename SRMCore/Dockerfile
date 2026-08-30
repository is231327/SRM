FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SRMShared/SRMShared.csproj SRMShared/
COPY SRMCore/SRMCore.csproj SRMCore/
RUN dotnet restore SRMCore/SRMCore.csproj

COPY . .
RUN dotnet publish SRMCore/SRMCore.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false /p:ContinuousIntegrationBuild=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "SRMCore.dll"]
