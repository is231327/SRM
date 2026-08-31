# SRM2

- Local containers: [ContainerServices/README.md](ContainerServices/README.md)
- Azure and CI/CD: [AZURE_CICD_DEPLOYMENT_GUIDE.md](AZURE_CICD_DEPLOYMENT_GUIDE.md)

## Container Services

This folder contains the local Docker and Azure deployment helpers for SRM.

## Development

When actively developing, comment `SRMCore`, `SRMApp`, `SRMAuth`, and `SRMAgent` out of `docker-compose.yml` and start them locally instead.

That way, only infrastructure and helper services such as SQL Server, Redis, Redmine, PostgreSQL, and the Shelly containers run in Docker.

This makes development faster because code changes do not require rebuilding all application containers, which can otherwise take 10 to 15 minutes.

Use these files during development:

- `.env-development` for local app development against containerized infrastructure
- `docker-compose.yml` for the infrastructure containers that still run in Docker

## Local Deployment

Use these files:

- `docker-compose.yml` for local container startup
- `.env` for local container configuration

Start the local stack with:

```powershell
docker compose up -d
```

If you want to remove the local stack completely and rebuild it from scratch, run:

```powershell
docker compose down -v
```

This stops the containers and removes the attached Docker volumes, so the databases and other persisted container data are recreated on the next start.

## Azure Deployment

Use these files:

- `.azure-services` for Azure resource and Container App names
- `.env-azure` for Azure-specific runtime values
- `Refresh-AzureEnvironmentFile.ps1` to regenerate `.env-azure` from the running Azure Container Apps
- `Apply-AzureContainerAppConfiguration.ps1` to apply `.env-azure` values to Azure Container Apps in bulk and restart changed apps

Refresh `.env-azure` with:

```powershell
powershell -ExecutionPolicy Bypass -File .\Refresh-AzureEnvironmentFile.ps1
```

Apply the Azure configuration with:

```powershell
powershell -ExecutionPolicy Bypass -File .\Apply-AzureContainerAppConfiguration.ps1
```

Optional:

```powershell
powershell -ExecutionPolicy Bypass -File .\Apply-AzureContainerAppConfiguration.ps1 -IncludeAgent
```

For the full Azure deployment workflow, see [AZURE_CICD_DEPLOYMENT_GUIDE.md](../AZURE_CICD_DEPLOYMENT_GUIDE.md).
