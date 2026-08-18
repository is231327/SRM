FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SRMShared/SRMShared.csproj SRMShared/
COPY SRMAgent/SRMAgent.csproj SRMAgent/
RUN dotnet restore SRMAgent/SRMAgent.csproj

COPY . .
RUN dotnet publish SRMAgent/SRMAgent.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SRMAgent.dll"]
