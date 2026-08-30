# SRM build, container, Azure, and CI/CD runbook

This runbook explains how to reproduce the development, local-container, Azure Container Apps, and release environments from the current repository. Run commands from:

```powershell
Set-Location C:\Users\Public\Projekte\SRMNew
```

Never commit or print `.env.*`, connection strings, passwords, API keys, tokens, host names, database names, usernames, URLs, or deployment identifiers. In this project all environment-specific runtime values are handled as sensitive.

## 1. Target architecture

### Direct development

- SQL Server, Redis, PostgreSQL, and Redmine run in Docker.
- SRMAuth, SRMCore, SRMApp, and SRMAgent run directly with `dotnet run`.
- `.env.development` is the only configuration source for this environment.
- Tracked `appsettings.Development.json` files contain logging only.

### Complete local container deployment

- Compose runs infrastructure, all SRM services, and three Shelly simulators.
- `.env.local` is the only source of local-container runtime values.
- `Deployment-Local.ps1` turns the canonical values into service-scoped ignored files under `.runtime-env`.
- `docker-compose.yml` contains only topology: images/builds, ports, profiles, health checks, dependencies, volumes, and resource limits.

### Azure development/demo deployment

- One Azure Container Apps environment hosts SQL Server, Redis, PostgreSQL, Redmine, Auth, Core, App, and optionally Agent.
- A Basic ACR stores immutable application images.
- A user-assigned managed identity pulls private images without registry passwords.
- Azure Files persists SQL Server, Redis, and Redmine attachments.
- PostgreSQL is intentionally ephemeral in this student/demo topology. The official image needs POSIX permissions that an SMB Azure Files mount does not provide. Use managed PostgreSQL for durable production data.
- `.env.azure` is the single source for Container App names and runtime values.
- `azure.parameters.json` contains resource-group-level deployment inputs.
- `Deployment-Azure.ps1` creates the complete environment and automatically performs the one-time Redmine bootstrap.
- `Release-Azure.ps1` handles later versions by changing only existing application images; it never reapplies Bicep or initializes Redmine.

The Bicep template keeps Auth and Core at one replica to avoid login and CRUD cold-start delays. The UI may scale to zero. SQL Server Developer edition and containerized PostgreSQL are development/demo choices, not a production architecture.

## 2. Configuration ownership

Create exactly one private file per environment:

| Environment | Private file | Template |
|---|---|---|
| Direct development | `ContainerServices/.env.development` | `.env.development.example` |
| Complete local containers | `ContainerServices/.env.local` | `.env.local.example` |
| Azure Container Apps | `ContainerServices/.env.azure` | `.env.azure.example` |

The files are separate because their network names and URLs differ. A password or other canonical value occurs once within a file; scripts map it to the names expected by each image.

Also create ignored `ContainerServices/azure.parameters.json` from its example for the Azure resource group, region, ACR, storage account, Container Apps environment, and agent feature flag.

Obsolete `.env`, `.env-development`, `.env-azure`, and `.azure-services` files are not read. Migrate needed values, then remove those copies.

To create an entirely independent test environment with randomly generated credentials:

```powershell
./ContainerServices/New-DeploymentConfiguration.ps1 -Name srm-test
```

This writes the three ignored `.env.*` files and `azure.parameters.json`. It refuses to overwrite them unless `-Force` is explicitly supplied.

## 3. Prerequisites

Install and verify:

```powershell
dotnet --info
docker version
docker compose version
az version
git --version
gh --version
```

The solution currently targets .NET 10. Docker Desktop must use Linux containers. PowerShell 7 is recommended.

For Azure:

```powershell
az login
az account list --output table
az account set --subscription '<Azure-for-Students-subscription>'
az account show --output table

az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.ContainerRegistry
az provider register --namespace Microsoft.ManagedIdentity
az provider register --namespace Microsoft.Storage
```

Choose a Container Apps region permitted by the subscription. Azure for Students quotas and available regions can differ from paid subscriptions.

## 4. Direct local development

Create and fill the private file:

```powershell
Copy-Item ContainerServices/.env.development.example ContainerServices/.env.development
```

Start infrastructure:

```powershell
./ContainerServices/Deployment-Local.ps1
```

The mode table is:

| Mode | Infrastructure | SRM apps | Shelly simulators | Input |
|---|---:|---:|---:|---|
| `Infrastructure` (default) | Yes | No | No | `.env.development` |
| `Simulators` | Yes | No | Yes | `.env.development` |
| `Full` | Yes | Yes | Yes | `.env.local` |

Run the apps in separate terminals:

```powershell
dotnet run --project SRMAuth --launch-profile http
dotnet run --project SRMCore --launch-profile http
dotnet run --project SRMApp --launch-profile http
dotnet run --project SRMAgent --launch-profile http
```

The projects load `.env.development` only when the .NET environment is `Development`. Unknown keys fail fast, helping catch misspellings.

## 5. Complete local container deployment

```powershell
Copy-Item ContainerServices/.env.local.example ContainerServices/.env.local
# Replace every placeholder.
./ContainerServices/Deployment-Local.ps1 -Mode Full -Build
```

For an isolated test deployment with new containers, networks, and volumes:

```powershell
./ContainerServices/Deployment-Local.ps1 -Mode Full -Build -ProjectName srm-test
```

When `REDMINE_ENABLED=false`, every local mode waits for Redmine, initializes its administrator, REST API, and project, and stores the generated API key in the selected private environment file. `Full` mode updates Core's generated scoped environment and recreates Core; direct-development modes read the updated `.env.development` when the .NET projects start. The enabled marker prevents later local starts from repeating initialization.

Compose creates:

| Container | Profile | Configuration source | Persistent data |
|---|---|---|---|
| SQL Server | default | generated `srm-sqlserver.env` | named SQL volume |
| Redis | default | command/topology only | named Redis volume |
| PostgreSQL | default | generated `srm-redmine-db.env` | named PostgreSQL volume |
| Redmine | default | generated `srm-redmine.env` | named files volume |
| Auth | `apps` | generated `srm-auth.env` | SQL Server |
| Core | `apps` | generated `srm-core.env` | SQL Server |
| App | `apps` | generated `srm-app.env` | none |
| Agent | `apps` | generated `srm-agent.env` | none |
| Shelly 1-3 | `simulators` | Compose topology | none |

Validate without starting:

```powershell
./ContainerServices/Deployment-Local.ps1 -Mode Full -PrepareOnly
Push-Location ContainerServices
docker compose -f docker-compose.yml -f .runtime-env/docker-compose.runtime.yml --profile apps --profile simulators config --quiet
Pop-Location
```

Inspect or stop:

```powershell
Push-Location ContainerServices
docker compose -f docker-compose.yml -f .runtime-env/docker-compose.runtime.yml ps
docker compose -f docker-compose.yml -f .runtime-env/docker-compose.runtime.yml logs --follow srm-app srm-auth srm-core
docker compose -f docker-compose.yml -f .runtime-env/docker-compose.runtime.yml down
Pop-Location
```

`down` preserves data. `down --volumes` deletes local data and should be used only for an intentional reset.

## 6. Prepare Azure inputs

```powershell
Copy-Item ContainerServices/azure.parameters.example.json ContainerServices/azure.parameters.json
Copy-Item ContainerServices/.env.azure.example ContainerServices/.env.azure
```

Set every placeholder. Container App host names must be unique within the Container Apps environment. ACR and storage names must satisfy Azure naming rules and be globally unique.

`azure.parameters.json` controls:

- target resource group and location;
- shared resource prefix;
- an existing Container Apps environment to adopt, or blank to create one;
- ACR and storage account names;
- whether the demo Agent is deployed.

`.env.azure` controls all container names and runtime configuration. `Deployment-Azure.ps1` rejects missing, unknown, empty, or placeholder values (except the optional bootstrap phone field). Keep a non-empty random value for `REDMINE_API_KEY` even while `REDMINE_ENABLED=false`, because Azure Container App secrets cannot be empty.

Validate source and Bicep:

```powershell
dotnet test SRM.sln
az bicep build --file ContainerServices/azure/main.bicep --stdout | Out-Null
```

## 7. What Bicep creates

Shared resources:

- Basic Azure Container Registry with admin access disabled;
- user-assigned managed identity and `AcrPull` assignment;
- Standard LRS storage account and Azure Files shares;
- Azure Container Apps managed environment, unless an existing one is selected.

Container Apps:

| App | Ingress | Replicas | Image | Storage |
|---|---|---:|---|---|
| SQL Server | internal TCP | 1 | official SQL Server 2022 | Azure Files |
| Redis | internal TCP | 1 | official Redis 7 Alpine | Azure Files/AOF |
| PostgreSQL | internal TCP | 1 | official PostgreSQL 16 | ephemeral |
| Redmine | external HTTPS | 1 | official Redmine 6 | Azure Files attachments |
| Auth | internal HTTP | 1 | private ACR image | none |
| Core | internal HTTP | 1 | private ACR image | none |
| App | external HTTPS | 0..1 | private ACR image | none |
| Agent | internal HTTP | 1 when enabled | private ACR image | none |

The complete `.env.azure` map is passed to Bicep as one `@secure()` object. Bicep creates only the scoped secrets required by each app and maps the remaining values into its container environment. Do not assume `@secure()` hides resource properties from authorized Azure users; it protects deployment parameter handling, not the deployed resource from administrators.

## 8. First Azure deployment

```powershell
$initialTag = (git rev-parse HEAD).Trim()
./ContainerServices/Deployment-Azure.ps1 -ImageTag $initialTag
```

The mandatory tag identifies the exact source used for the initial environment and must not be reused for different image contents.

The one-time deployment script:

1. validates `azure.parameters.json` and `.env.azure`;
2. checks Azure CLI authentication;
3. creates or reconciles the resource group so a failed first deployment can be resumed safely;
4. deploys foundation resources with application creation disabled;
5. builds Auth, Core, App, Redmine, and optionally Agent remotely in ACR;
6. deploys the full template with the immutable image tag;
7. waits for and initializes the fresh Redmine instance while `REDMINE_ENABLED=false`;
8. writes only Redmine's generated API key and enabled marker back to `.env.azure`;
9. reapplies the full template once so Core receives the final Redmine configuration;
10. deletes every temporary secure parameter file in `finally`.

Inspect the result without hard-coding private resource names:

```powershell
$settings = Get-Content ContainerServices/azure.parameters.json -Raw | ConvertFrom-Json
$resourceGroup = [string]$settings.resourceGroup

az deployment group list --resource-group $resourceGroup --output table
az containerapp list --resource-group $resourceGroup `
  --query "[].{name:name,status:properties.runningStatus,latest:properties.latestRevisionName,ready:properties.latestReadyRevisionName,fqdn:properties.configuration.ingress.fqdn}" `
  --output table
```

For each active app, the latest revision should equal the latest ready revision.

## 9. Automatic Redmine initialization

Keep `REDMINE_ENABLED=false` and a non-empty random placeholder in `REDMINE_API_KEY` before the first deployment. After the first full Container Apps deployment, `Deployment-Azure.ps1` runs its internal Redmine initialization function. The function waits for migrations, loads defaults, runs the fixed initializer shipped in the `srm-redmine` image, changes the administrator to the private configured credentials, enables REST, creates the private project, captures Redmine's generated API key without printing it, and updates `.env.azure`. The deployment then reapplies Core with that key. There is no separate Redmine command to run.

To perform the equivalent setup manually:

1. Open the Redmine HTTPS FQDN shown by `az containerapp list`.
2. Sign in to the fresh Redmine administrator account and immediately change its default password.
3. Open **Administration > Settings > API** and enable REST web service.
4. Load defaults if trackers and statuses are absent:

```powershell
. ./ContainerServices/Deployment.Library.ps1
$azure = Read-KeyValueFile -Path ContainerServices/.env.azure
$settings = Get-Content ContainerServices/azure.parameters.json -Raw | ConvertFrom-Json
az containerapp exec --name $azure['REDMINE_HOST'] --resource-group $settings.resourceGroup `
  --command "bin/rake redmine:load_default_data RAILS_ENV=production REDMINE_LANG=en"
```

5. Create the project whose identifier matches `REDMINE_PROJECT_IDENTIFIER` and enable issue tracking.
6. Generate an API access key through **My account > API access key**.
7. Put it in `.env.azure` as `REDMINE_API_KEY` and change `REDMINE_ENABLED=true`.
8. Determine Core's current immutable tag and reapply configuration only when repairing a manual Redmine setup:

```powershell
$coreImage = az containerapp show --name $azure['CORE_HOST'] --resource-group $settings.resourceGroup `
  --query properties.template.containers[0].image --output tsv
$currentTag = ($coreImage -split ':')[-1]
./ContainerServices/Deployment-Azure.ps1 -ImageTag $currentTag -ConfigurationOnly
```

Do not try to force a chosen Redmine token through database automation; Redmine callbacks can replace it. Use the API key Redmine generated.

For local containers, every `Deployment-Local.ps1` mode runs its internal Redmine initialization function while its selected configuration says `REDMINE_ENABLED=false`. The function performs the equivalent setup against only the named Compose project and recreates Core only in `Full` mode. There is no separate Redmine command to run.

For a later Azure runtime-configuration change that preserves all resource names and topology, determine the currently deployed immutable tag and run:

```powershell
./ContainerServices/Deployment-Azure.ps1 -ImageTag '<current-image-tag>' -ConfigurationOnly
```

This mode refuses to proceed unless the resource group and all expected Container Apps already exist. It reapplies Bicep without building images or initializing Redmine. Update the protected `SRM_AZURE_PARAMETERS` and `SRM_AZURE_ENV` GitHub Environment secrets afterward so future releases target the same configuration.

## 10. Deploy the Azure Shelly simulator

Shelly is a demo add-on and is not created by `main.bicep` or the release workflow. Its name and port live in `.env.azure`.

```powershell
$prefix = [string]$settings.parameters.prefix.value
$environmentName = [string]$settings.parameters.containerAppsEnvironmentName.value
$environmentResourceGroup = [string]$settings.parameters.containerAppsEnvironmentResourceGroup.value
if ([string]::IsNullOrWhiteSpace($environmentName)) {
  $environmentName = "$prefix-environment"
  $environmentResourceGroup = [string]$settings.resourceGroup
}
$environmentId = az containerapp env show --name $environmentName --resource-group $environmentResourceGroup --query id -o tsv
$registry = [string]$settings.parameters.registryName.value
$tag = (git rev-parse HEAD).Trim()
$identityId = az identity show --name "$prefix-identity" --resource-group $settings.resourceGroup --query id -o tsv

az acr build --registry $registry --image "shelly-demo:$tag" --file PythonShelly/dockerfile PythonShelly
az containerapp create `
  --name $azure['SHELLY_HOST'] `
  --resource-group $settings.resourceGroup `
  --environment $environmentId `
  --image "$registry.azurecr.io/shelly-demo:$tag" `
  --ingress internal `
  --target-port $azure['SHELLY_PORT'] `
  --min-replicas 1 --max-replicas 1 `
  --cpu 0.25 --memory 0.5Gi `
  --user-assigned $identityId `
  --registry-server "$registry.azurecr.io" `
  --registry-identity $identityId
```

Configure the SRM Shelly record as `http://<SHELLY_HOST>` without the target port. Container Apps internal HTTP ingress exposes the app through the environment on port 80 and forwards to `SHELLY_PORT`.

## 11. Seed demo data

`SRMDemoSeeder` is idempotent: it inserts the presentation entities when missing and updates the matching agent credential. Build it, pass only its scoped values, run it once, and remove the job.

```powershell
$seedTag = "demo-seeder-$(Get-Date -Format yyyyMMdd-HHmmss)"
az acr build --registry $registry --image "srm-demo-seeder:$seedTag" --file SRMDemoSeeder/Dockerfile .

$jobName = 'test-job-srm-demo-seed'
$shellyBaseUrl = "http://$($azure['SHELLY_HOST'])"

az containerapp job create `
  --name $jobName --resource-group $settings.resourceGroup --environment $environmentId `
  --trigger-type Manual --replica-timeout 600 --replica-retry-limit 1 `
  --image "$registry.azurecr.io/srm-demo-seeder:$seedTag" --cpu 0.5 --memory 1Gi `
  --mi-user-assigned $identityId --registry-server "$registry.azurecr.io" --registry-identity $identityId `
  --secrets "sql-password=$($azure['SQL_PASSWORD'])" "agent-secret=$($azure['AGENT_CLIENT_SECRET'])" `
  --env-vars `
    SQL_HOST="$($azure['SQL_HOST'])" SQL_PORT="$($azure['SQL_PORT'])" `
    SQL_USERNAME="$($azure['SQL_USERNAME'])" SQL_PASSWORD=secretref:sql-password `
    SQL_AUTH_DATABASE="$($azure['SQL_AUTH_DATABASE'])" SQL_CORE_DATABASE="$($azure['SQL_CORE_DATABASE'])" `
    AGENT_CLIENT_IDENTIFIER="$($azure['AGENT_CLIENT_IDENTIFIER'])" `
    AGENT_CLIENT_SECRET=secretref:agent-secret SHELLY_BASE_URL="$shellyBaseUrl"

az containerapp job start --name $jobName --resource-group $settings.resourceGroup
az containerapp job execution list --name $jobName --resource-group $settings.resourceGroup --output table
az containerapp job delete --name $jobName --resource-group $settings.resourceGroup --yes
```

The command necessarily sends private values to Azure CLI; do not enable shell tracing, echo the variables, or paste terminal output into tickets. The temporary job owns only scoped copies of the SQL and agent secrets, which are deleted with the job.

On Windows, old Azure CLI builds can fail while streaming a Python ACR build log because the console uses CP1252. If that happens, build the same Dockerfile locally after `az acr login`, tag it with the new registry login server and immutable tag, and `docker push` it. Do not change the Dockerfile or use a floating tag as a workaround.

## 12. CI pipeline

`.github/workflows/ci.yml` runs for pushes and pull requests to `master`. It:

1. starts a SQL Server service container;
2. restores, builds, and tests the solution;
3. generates scoped Compose env files and validates the complete Compose model;
4. builds Auth, Core, App, Agent, and demo-seeder images;
5. verifies that the App image contains `/app/wwwroot/_framework/blazor.web.js`.

Configure these repository or Environment secrets:

- `CI_SQL_ACCEPT_EULA`
- `CI_SQL_EDITION`
- `CI_SQL_HOST`
- `CI_SQL_USERNAME`
- `CI_SQL_SA_PASSWORD`
- `CI_AUTH_CONNECTION_STRING`
- `CI_CORE_CONNECTION_STRING`
- `CI_LOCAL_ENV` (complete multiline `.env.local`-format CI configuration)

Use dedicated CI values and databases. Do not reuse development or Azure credentials.

## 13. GitHub-to-Azure OIDC

Create an Entra application/service principal, add a federated credential for the GitHub Environment, and grant only the permissions needed to push images to the existing ACR and update the existing application Container Apps. The release identity does not need permission to create infrastructure or role assignments.

Example subject for repository `<owner>/<repo>` and Environment `azure-development`:

```text
repo:<owner>/<repo>:environment:azure-development
```

The issuer is `https://token.actions.githubusercontent.com` and audience is `api://AzureADTokenExchange`.

Configure GitHub Environment `azure-development` with these secrets because this project treats all environment-specific identifiers as sensitive:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `SRM_AZURE_PARAMETERS`: complete multiline `azure.parameters.json` contents.
- `SRM_AZURE_ENV`: complete multiline `.env.azure` contents after the initial deployment has initialized Redmine.

Add required reviewers to the Environment if deployments need approval. OIDC avoids a long-lived Azure client secret.

## 14. Release workflow

`.github/workflows/deploy-azure.yml`:

1. resolves the selected release/ref to a commit SHA;
2. requires a successful CI run for exactly that SHA;
3. authenticates to Azure with OIDC;
4. builds and pushes SHA-tagged Auth, Core, App, Redmine, and Agent images;
5. creates ephemeral ignored inputs from the two protected configuration secrets;
6. runs `Release-Azure.ps1 -SkipBuild` to preflight the existing target apps and update only their images;
7. waits for every updated revision to become ready.

The workflow never runs Bicep, creates Azure resources, changes runtime configuration, or initializes Redmine. `deployAgent` in the protected Azure parameters determines whether the existing Agent app is updated; the Agent image is still built so the matrix remains deterministic.

Create a release:

```powershell
git status --short
dotnet test SRM.sln
git tag v1.0.0
git push origin v1.0.0
gh release create v1.0.0 --verify-tag --generate-notes
```

Publishing triggers the release. You can also manually run **Release Azure** and choose a ref.

Local fallback:

```powershell
gh auth login
az login
./ContainerServices/Deploy-Release.ps1 -Tag v1.0.0
```

The fallback verifies CI, checks out the exact release into a detached temporary worktree, builds it in ACR, updates only the existing application Container Apps, waits for their revisions, and removes the worktree.

## 15. Verification

```powershell
az containerapp list --resource-group $settings.resourceGroup `
  --query "[].{name:name,running:properties.runningStatus,latest:properties.latestRevisionName,ready:properties.latestReadyRevisionName,fqdn:properties.configuration.ingress.fqdn}" -o table

az containerapp logs show --name $azure['AUTH_HOST'] --resource-group $settings.resourceGroup --tail 100
az containerapp logs show --name $azure['CORE_HOST'] --resource-group $settings.resourceGroup --tail 100
az containerapp logs show --name $azure['APP_HOST'] --resource-group $settings.resourceGroup --tail 100
```

Functional checks:

- App HTTPS endpoint loads without browser-console errors.
- `/_framework/blazor.web.js` returns HTTP 200.
- Bootstrap administrator can log in and receives the expected role.
- Customer, room, server, agent, Shelly, maintenance, and incident CRUD works.
- Agent authenticates and submits new readings.
- Shelly readings change over time.
- With Redmine enabled, Core resolves the project and creates an issue.

## 16. Troubleshooting

### Blank page and Blazor framework 404

The App image must contain `/app/wwwroot/_framework/blazor.web.js`, and the web host must map static assets. Rebuild from a commit that passed the container asset check; do not patch the running container.

### Login or writes take about a minute

Check Auth/Core minimum replicas and revision health. Both are deliberately configured with one warm replica. Inspect logs for SQL or Redis connection retries.

### A private image revision will not activate

Verify the user-assigned identity is attached, has `AcrPull`, and is selected as the Container App registry identity. Confirm the SHA tag exists in ACR.

### PostgreSQL fails with permission errors

Do not mount Azure Files at the official PostgreSQL data directory. The demo topology uses ephemeral storage. Move to managed PostgreSQL for durability.

### Redmine returns 500

Wait for initial migrations, inspect Redmine and PostgreSQL logs, and remember that replacing ephemeral PostgreSQL creates a fresh database requiring initialization.

### Redmine integration returns 401 or 404

- 401: API key differs from the key generated by Redmine.
- 404: configured project identifier/tracker/status does not exist.

### Agent cannot authenticate

The identifier and secret must match between `.env.azure`, the Auth credential record, and Agent. Rerun the idempotent demo seeder after changing them.

### Agent authenticates but monitoring cycles fail because `ping` is unavailable

The Agent uses the operating system's `ping` utility for reachability checks. Build it from `SRMAgent/Dockerfile`, which installs `iputils-ping`; do not substitute an unmodified ASP.NET runtime image. The CI container-build job verifies that `ping` exists before an image can be released.

## 17. Rollback and cost

Rollback is an image-only release using a previously successful commit SHA that still exists in ACR:

```powershell
./ContainerServices/Release-Azure.ps1 -ImageTag '<previous-successful-sha>' -SkipBuild
```

This does not roll back database schema or data. Treat migrations separately.

For Azure for Students:

- monitor remaining credit and resource usage;
- keep ACR at Basic and storage at Standard LRS;
- understand that minimum replicas consume credit continuously;
- check the Container Apps managed-environment quota before provisioning an isolated environment. Some Students subscriptions allow only one managed environment globally. In that case a second, fully isolated deployment requires another subscription or an approved quota increase; do not delete or reuse a working environment unless that is an explicit deployment decision;

When reuse is intentional, put the existing environment's name and resource group into `containerAppsEnvironmentName` and `containerAppsEnvironmentResourceGroup`. The deployment still creates a new resource group, registry, identity, storage account, shares, databases, and Container Apps. It only attaches newly prefixed Azure Files entries to the shared environment. The deployment location must match the environment location.
- remove temporary jobs and unused images;
- never expose SQL Server, Redis, or PostgreSQL publicly;
- use managed databases, Key Vault references, backups, and production SQL licensing before treating this as production.

## 18. Complete order of operations

1. Create and fill `.env.development`; run direct development.
2. Create and fill `.env.local`; validate and run the complete Compose stack.
3. Run tests and container builds.
4. Create `azure.parameters.json` and `.env.azure` with independent Azure values.
5. Validate Bicep.
6. Run the one-time Azure deployment; it initializes Redmine and reapplies Core automatically.
7. Deploy the optional Shelly simulator.
8. Run and delete the demo-seeder job.
9. Verify browser, CRUD, live agent/Shelly readings, and Redmine tickets.
10. Configure GitHub CI secrets and Azure OIDC Environment settings.
11. Publish a CI-verified release and observe the image-only release workflow.
