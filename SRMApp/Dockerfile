FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SRMShared/SRMShared.csproj SRMShared/
COPY SRMApp/SRMApp.csproj SRMApp/
RUN dotnet restore SRMApp/SRMApp.csproj

COPY . .
RUN dotnet publish SRMApp/SRMApp.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SRMApp.dll"]
