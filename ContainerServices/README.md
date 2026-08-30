# ContainerServices

This directory owns local container orchestration and Azure deployment. Runtime values are never stored in Compose, Bicep defaults, application settings, scripts, or workflow YAML. Each environment has one ignored configuration file.

## Files

| File | Purpose |
|---|---|
| `Deploy-Release.ps1` | Resolves and verifies an exact GitHub Release, then passes it to the image-only Azure release script |
| `Deployment-Azure.ps1` | Creates a complete Azure environment and automatically initializes its fresh Redmine instance |
| `Deployment.Library.ps1` | Shared parsing, validation, and external-command helpers; not run directly |
| `Deployment-Local.ps1` | Starts the selected Compose topology and automatically initializes its fresh Redmine instance |
| `New-DeploymentConfiguration.ps1` | Creates independent ignored development, local, and Azure configuration files with random credentials |
| `Release-Azure.ps1` | Builds optional immutable images and updates only existing application Container Apps |
| `.env.azure.example` | Template for all Azure Container App names and runtime values |
| `.env.development.example` | Template for direct .NET development plus its infrastructure containers |
| `.env.local.example` | Template for the complete local container deployment |
| `azure.parameters.example.json` | Template for Azure resource-group-level inputs and deployment flags |
| `azure/main.bicep` | Azure resource topology and Container App definitions |
| `docker-compose.yml` | Local topology, images, ports, profiles, dependencies, health checks, volumes, and resource limits |

Generated or private files are ignored by Git:

| File | Used for |
|---|---|
| `.env.azure` | Azure Container App names and runtime configuration |
| `.env.development` | Direct .NET development and `Infrastructure`/`Simulators` mode |
| `.env.local` | `Full` local container mode |
| `.runtime-env/*.env` | Generated, service-scoped Compose input; never edit manually |
| `azure.parameters.json` | Actual Azure resource group, region, registry, storage, and flags |

Do not use the obsolete `.env`, `.env-development`, `.env-azure`, or `.azure-services` files. After migrating any values you need, delete those private copies yourself.

For a completely new isolated environment, generate all private inputs at once:

```powershell
./ContainerServices/New-DeploymentConfiguration.ps1 -Name srm-test
```

The script refuses to overwrite existing private files unless `-Force` is supplied. `-Force` rotates every generated credential and must only be used when intentionally replacing the complete environment.

To reuse an existing Container Apps environment while keeping every other resource isolated, set both `containerAppsEnvironmentName` and `containerAppsEnvironmentResourceGroup` in `azure.parameters.json`. The deployment creates uniquely prefixed storage attachments in that environment; it does not modify existing Container Apps.

## Configuration model

All values in the three `.env.*` files are treated as sensitive, including host names, ports, database names, usernames, URLs, identifiers, and profile data.

There is one source file per execution environment:

- `.env.development`: apps run directly with `dotnet run`; infrastructure runs in Docker.
- `.env.local`: infrastructure and SRM apps all run in Docker.
- `.env.azure`: resources and SRM apps run in Azure Container Apps.

These files are deliberately separate because their hosts and URLs differ. A credential occurs only once inside a file. The scripts map that canonical value to the environment-variable names required by different images. For example, `SQL_PASSWORD` becomes `MSSQL_SA_PASSWORD` for SQL Server and `SqlServer__Password` for the SRM services.

Tracked `appsettings*.json` files contain generic application behavior such as logging only.

## Docker Compose explained

Compose defines these services:

| Service | Started by default | Profile | Role |
|---|---:|---|---|
| `srm-sqlserver` | Yes | none | SRM databases; persistent named volume |
| `srm-redis` | Yes | none | Shared runtime/token state; persistent named volume |
| `srm-redmine-db` | Yes | none | PostgreSQL database for Redmine; persistent named volume |
| `srm-redmine` | Yes | none | Redmine web application; persistent files volume |
| `srm-auth` | No | `apps` | Authentication API |
| `srm-core` | No | `apps` | SRM API, monitoring, and Redmine integration |
| `srm-app` | No | `apps` | Blazor UI |
| `srm-agent` | No | `apps` | Monitoring agent |
| `shelly1`-`shelly3` | No | `simulators` | Shelly device simulators |

Important Compose behavior:

- `depends_on` controls startup ordering. Health conditions wait for SQL Server, Redis, and PostgreSQL before dependent containers start.
- Named volumes keep local data when containers are recreated.
- Compose service names provide internal DNS. A container uses names such as the configured SQL host, not `localhost`.
- Published ports expose selected services to the host for development and testing.
- `env_file` points only to ignored files generated under `.runtime-env`. Compose itself contains no runtime values.
- `profiles` keep application and simulator containers optional.
- CPU and memory limits keep the full stack practical on a development machine.
- The Agent image installs the Linux `ping` utility because monitored-device reachability checks use it at runtime. CI verifies that dependency is present.

Do not run `docker compose up` directly: first use `Deployment-Local.ps1`, which creates the required scoped environment files.

## Local development

### First setup

```powershell
Copy-Item ContainerServices/.env.development.example ContainerServices/.env.development
```

Replace every placeholder. Use development-only values. In particular, `REDMINE_DB_HOST` must be the configured Compose PostgreSQL service host, while the direct .NET endpoints use host-accessible URLs such as `localhost`.

### Modes

`Deployment-Local.ps1` always starts the infrastructure. `Mode` controls whether simulators and/or SRM application containers are added:

| Mode | Infrastructure | SRM apps | Shelly simulators | Configuration file |
|---|---:|---:|---:|---|
| `Infrastructure` (default) | Yes | No | No | `.env.development` |
| `Simulators` | Yes | No | Yes | `.env.development` |
| `Full` | Yes | Yes | Yes | `.env.local` |

Start infrastructure, then run the .NET projects from separate terminals:

```powershell
./ContainerServices/Deployment-Local.ps1
dotnet run --project SRMAuth --launch-profile http
dotnet run --project SRMCore --launch-profile http
dotnet run --project SRMApp --launch-profile http
dotnet run --project SRMAgent --launch-profile http
```

Start infrastructure plus Shelly simulators:

```powershell
./ContainerServices/Deployment-Local.ps1 -Mode Simulators
```

`-PrepareOnly` validates configuration and generates scoped environment files without starting Docker:

```powershell
./ContainerServices/Deployment-Local.ps1 -Mode Infrastructure -PrepareOnly
```

If Windows PowerShell blocks local scripts, run `Set-ExecutionPolicy -Scope Process Bypass` in that terminal only.

## Complete local container deployment

Create and configure its independent file:

```powershell
Copy-Item ContainerServices/.env.local.example ContainerServices/.env.local
./ContainerServices/Deployment-Local.ps1 -Mode Full -Build
```

Use `-ProjectName` to create an isolated Compose project with independent containers, networks, and volumes:

```powershell
./ContainerServices/Deployment-Local.ps1 -Mode Full -Build -ProjectName srm-test
```

For a fresh deployment, the command waits for Redmine, loads its defaults, replaces the default administrator login, enables REST API access, creates the configured private project, and stores Redmine's generated API key in the selected private environment file. `Full` mode also recreates Core with the key; direct-development modes read the updated `.env.development` when the .NET projects start. Later invocations see `REDMINE_ENABLED=true` and do not initialize Redmine again.

Use Compose from `ContainerServices` after the launcher has generated `.runtime-env`:

```powershell
Push-Location ContainerServices
docker compose -f docker-compose.yml -f .runtime-env/docker-compose.runtime.yml --profile apps --profile simulators config
docker compose -f docker-compose.yml -f .runtime-env/docker-compose.runtime.yml ps
docker compose -f docker-compose.yml -f .runtime-env/docker-compose.runtime.yml logs --follow srm-app srm-auth srm-core
docker compose -f docker-compose.yml -f .runtime-env/docker-compose.runtime.yml down
Pop-Location
```

`docker compose ... down` preserves named volumes. Adding `--volumes` irreversibly deletes local SQL, Redis, PostgreSQL, and Redmine data; use it only for an intentional reset.

## Azure input files

Create the private files:

```powershell
Copy-Item ContainerServices/azure.parameters.example.json ContainerServices/azure.parameters.json
Copy-Item ContainerServices/.env.azure.example ContainerServices/.env.azure
```

`azure.parameters.json` contains subscription/resource-level inputs:

- `resourceGroup`: target resource group.
- `location`: an Azure Container Apps region permitted by the subscription.
- `prefix`: prefix for shared resources.
- `containerAppsEnvironmentName`: existing environment to adopt, or blank to create one.
- `containerAppsEnvironmentResourceGroup`: resource group containing that existing environment, or blank when creating one.
- `registryName`: globally unique Azure Container Registry name.
- `storageAccountName`: globally unique storage account name.
- `deployAgent`: whether to deploy the agent image and Container App.

`.env.azure` is the single source for Container App names and runtime values. `Deployment-Azure.ps1` validates every key and passes the complete map to Bicep as one `@secure()` object. Bicep creates scoped Container App secrets and maps values to each container. Azure resource properties are still visible to authorized Azure users; `@secure()` prevents the parameter object from being recorded as ordinary deployment output.

Use independent Azure credentials. Do not copy the local files wholesale.

## First Azure deployment

Prerequisites: Azure CLI, an authenticated Azure for Students subscription, and the required providers (`Microsoft.App`, `Microsoft.ContainerRegistry`, `Microsoft.ManagedIdentity`, and `Microsoft.Storage`).

Validate first:

```powershell
az account show --output table
az bicep build --file ContainerServices/azure/main.bicep --stdout | Out-Null
dotnet test SRM.sln
```

Provision resources, build immutable images remotely in ACR, deploy, and initialize the fresh Redmine instance:

```powershell
$initialTag = (git rev-parse HEAD).Trim()
./ContainerServices/Deployment-Azure.ps1 -ImageTag $initialTag
```

The tag is mandatory and should be the exact source commit so the initial deployment is reproducible.

The script performs a foundation deployment; builds `srm-auth`, `srm-core`, `srm-app`, and `srm-redmine` (plus `srm-agent` when enabled); deploys the complete environment; initializes Redmine while `REDMINE_ENABLED=false`; stores Redmine's generated API key in `.env.azure`; enables the integration; and reapplies the full template once with the final configuration. Later runs see `REDMINE_ENABLED=true` and leave Redmine state unchanged. SQL Server and Redis use Azure Files. The student/demo PostgreSQL container is ephemeral because the official PostgreSQL image cannot apply its required POSIX permissions to an SMB Azure Files mount. Use managed PostgreSQL for durable production data.

## Releases

### Recommended: GitHub Actions

The release workflow is `.github/workflows/deploy-azure.yml`. It releases a published GitHub Release or a manually selected ref only after that commit has a successful `ci.yml` run. Images are tagged with the immutable commit SHA.

Configure the protected GitHub Environment `azure-development` with these secrets:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `SRM_AZURE_PARAMETERS`: the complete multiline contents of `ContainerServices/azure.parameters.json`.
- `SRM_AZURE_ENV`: the complete multiline contents of `ContainerServices/.env.azure` after the initial deployment has initialized Redmine.

The release OIDC service principal needs permission only to push images to the existing ACR and update the existing application Container Apps. Initial infrastructure creation and ACR pull-role assignment remain part of the separately authenticated one-time deployment.

Release procedure:

```powershell
git status --short
dotnet test SRM.sln
git tag v1.0.0
git push origin v1.0.0
gh release create v1.0.0 --verify-tag --generate-notes
```

Publishing the release triggers an application-only release. The workflow builds immutable images and uses `Release-Azure.ps1`; it does not run Bicep, create infrastructure, change runtime configuration, or initialize Redmine. Protect the GitHub Environment with required reviewers if desired. A manual release is available through **Actions > Release Azure > Run workflow**.

### Local release fallback

```powershell
gh auth login
az login
./ContainerServices/Deploy-Release.ps1 -Tag v1.0.0
```

The script resolves the release commit, verifies CI, creates a detached worktree, builds that exact source in ACR, updates only the existing application Container Apps, verifies their new revisions, and removes the worktree.

### Configuration-only reapply

After editing `.env.azure`, reuse an existing image tag:

```powershell
./ContainerServices/Deployment-Azure.ps1 -ImageTag '<existing-commit-sha>' -ConfigurationOnly
```

This mode first requires the configured resource group and every expected Container App to exist, then reapplies Bicep with the existing image tag. It does not build images or initialize Redmine. Use it for runtime-value changes that preserve resource names and topology; create a new environment for naming or topology changes. Do not use it for a normal release.

## Verification

```powershell
$settings = Get-Content ContainerServices/azure.parameters.json -Raw | ConvertFrom-Json
$resourceGroup = $settings.resourceGroup
az containerapp list --resource-group $resourceGroup --query "[].{name:name,revision:properties.latestReadyRevisionName,fqdn:properties.configuration.ingress.fqdn}" --output table
az containerapp revision list --resource-group $resourceGroup --name '<app-name>' --query "[].{name:name,active:properties.active,healthy:properties.healthState}" --output table
az containerapp logs show --resource-group $resourceGroup --name '<app-name>' --tail 100
```

Then verify in the UI: login, create/read/update/delete a customer and server, receive an agent or Shelly reading, and create a Redmine incident when integration is enabled.

Never print `.env.*`, Container App secret values, access tokens, or connection strings into CI logs. For Azure for Students, monitor credit regularly and remember that minimum replicas for SQL, Redis, PostgreSQL, Redmine, Auth, Core, and the optional Agent consume credit continuously.
