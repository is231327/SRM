FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SRMShared/SRMShared.csproj SRMShared/
COPY SRMDemoSeeder/SRMDemoSeeder.csproj SRMDemoSeeder/
RUN dotnet restore SRMDemoSeeder/SRMDemoSeeder.csproj

COPY . .
RUN dotnet publish SRMDemoSeeder/SRMDemoSeeder.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "SRMDemoSeeder.dll"]
