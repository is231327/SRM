FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SRMShared/SRMShared.csproj SRMShared/
COPY SRMAgent/SRMAgent.csproj SRMAgent/
RUN dotnet restore SRMAgent/SRMAgent.csproj

COPY . .
RUN dotnet publish SRMAgent/SRMAgent.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false /p:ContinuousIntegrationBuild=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends iputils-ping \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "SRMAgent.dll"]
