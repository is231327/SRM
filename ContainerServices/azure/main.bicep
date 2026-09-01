@description('Azure region allowed by the subscription.')
param location string

@minLength(2)
@maxLength(16)
param prefix string = 'srm'

@description('Existing Container Apps environment to adopt. Leave empty to create <prefix>-environment.')
param containerAppsEnvironmentName string = ''

@description('Resource group containing the existing Container Apps environment. Required when containerAppsEnvironmentName is set.')
param containerAppsEnvironmentResourceGroup string = ''

@minLength(5)
@maxLength(50)
param registryName string

@minLength(3)
@maxLength(24)
param storageAccountName string

param imageTag string
param deployApplications bool = true
param deployAgent bool = false

@description('Complete environment-specific runtime configuration. Supplied from the ignored .env.azure file.')
@secure()
param runtimeConfiguration object

var useExistingEnvironment = !empty(containerAppsEnvironmentName)
var environmentName = useExistingEnvironment ? containerAppsEnvironmentName : '${prefix}-environment'
var identityName = '${prefix}-identity'
var sqlEnvironmentStorageName = '${prefix}-sql-storage'
var redisEnvironmentStorageName = '${prefix}-redis-storage'
var redmineEnvironmentStorageName = '${prefix}-redmine-storage'
var sqlName = runtimeConfiguration['SQL_HOST']
var redisName = runtimeConfiguration['REDIS_HOST']
// PostgreSQL is deliberately isolated from the retired Azure Files-backed app.
// Its data is ephemeral in the student/demo topology; see the deployment guide.
var postgresName = runtimeConfiguration['REDMINE_DB_HOST']
var redmineName = runtimeConfiguration['REDMINE_HOST']
var authName = runtimeConfiguration['AUTH_HOST']
var coreName = runtimeConfiguration['CORE_HOST']
var appName = runtimeConfiguration['APP_HOST']
var agentName = runtimeConfiguration['AGENT_HOST']
var acrPullRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: registryName
  location: location
  sku: { name: 'Basic' }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, identity.id, acrPullRoleId)
  scope: registry
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleId
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource fileService 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource sqlShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: fileService
  name: 'sqlserver'
  properties: { shareQuota: 10 }
}

resource redisShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: fileService
  name: 'redis'
  properties: { shareQuota: 5 }
}

resource redmineShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: fileService
  name: 'redmine'
  properties: { shareQuota: 5 }
}

resource newEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = if (!useExistingEnvironment) {
  name: environmentName
  location: location
  properties: {}
}

resource existingEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = if (useExistingEnvironment) {
  name: environmentName
  scope: resourceGroup(containerAppsEnvironmentResourceGroup)
}

var environmentId = useExistingEnvironment ? existingEnvironment.id : newEnvironment.id

resource sqlEnvironmentStorage 'Microsoft.App/managedEnvironments/storages@2024-03-01' = if (!useExistingEnvironment) {
  parent: newEnvironment
  name: sqlEnvironmentStorageName
  properties: {
    azureFile: {
      accountName: storage.name
      accountKey: storage.listKeys().keys[0].value
      shareName: sqlShare.name
      accessMode: 'ReadWrite'
    }
  }
}

resource redisEnvironmentStorage 'Microsoft.App/managedEnvironments/storages@2024-03-01' = if (!useExistingEnvironment) {
  parent: newEnvironment
  name: redisEnvironmentStorageName
  properties: {
    azureFile: {
      accountName: storage.name
      accountKey: storage.listKeys().keys[0].value
      shareName: redisShare.name
      accessMode: 'ReadWrite'
    }
  }
}

resource redmineEnvironmentStorage 'Microsoft.App/managedEnvironments/storages@2024-03-01' = if (!useExistingEnvironment) {
  parent: newEnvironment
  name: redmineEnvironmentStorageName
  properties: {
    azureFile: {
      accountName: storage.name
      accountKey: storage.listKeys().keys[0].value
      shareName: redmineShare.name
      accessMode: 'ReadWrite'
    }
  }
}

resource sql 'Microsoft.App/containerApps@2024-03-01' = if (deployApplications) {
  name: sqlName
  location: location
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        transport: 'tcp'
        targetPort: int(runtimeConfiguration['SQL_PORT'])
        exposedPort: int(runtimeConfiguration['SQL_PORT'])
      }
      secrets: [{ name: 'sql-password', value: runtimeConfiguration['SQL_PASSWORD'] }]
    }
    template: {
      containers: [{
        name: 'sqlserver'
        image: 'mcr.microsoft.com/mssql/server:2022-latest'
        env: [
          { name: 'ACCEPT_EULA', value: runtimeConfiguration['SQL_ACCEPT_EULA'] }
          { name: 'MSSQL_PID', value: runtimeConfiguration['SQL_EDITION'] }
          { name: 'MSSQL_SA_PASSWORD', secretRef: 'sql-password' }
        ]
        resources: { cpu: json('1.0'), memory: '2Gi' }
        volumeMounts: [{ volumeName: 'data', mountPath: '/var/opt/mssql' }]
      }]
      scale: { minReplicas: 1, maxReplicas: 1 }
      volumes: [{ name: 'data', storageType: 'AzureFile', storageName: sqlEnvironmentStorageName }]
    }
  }
}

resource redis 'Microsoft.App/containerApps@2024-03-01' = if (deployApplications) {
  name: redisName
  location: location
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: { external: false, transport: 'tcp', targetPort: int(runtimeConfiguration['REDIS_PORT']), exposedPort: int(runtimeConfiguration['REDIS_PORT']) }
    }
    template: {
      containers: [{
        name: 'redis'
        image: 'redis:7-alpine'
        command: ['redis-server']
        args: ['--appendonly', 'yes']
        resources: { cpu: json('0.25'), memory: '0.5Gi' }
        volumeMounts: [{ volumeName: 'data', mountPath: '/data' }]
      }]
      scale: { minReplicas: 1, maxReplicas: 1 }
      volumes: [{ name: 'data', storageType: 'AzureFile', storageName: redisEnvironmentStorageName }]
    }
  }
}

resource postgres 'Microsoft.App/containerApps@2024-03-01' = if (deployApplications) {
  name: postgresName
  location: location
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: { external: false, transport: 'tcp', targetPort: int(runtimeConfiguration['REDMINE_DB_PORT']), exposedPort: int(runtimeConfiguration['REDMINE_DB_PORT']) }
      secrets: [{ name: 'postgres-password', value: runtimeConfiguration['REDMINE_DB_PASSWORD'] }]
    }
    template: {
      containers: [{
        name: 'postgres'
        image: 'postgres:16'
        env: [
          { name: 'POSTGRES_DB', value: runtimeConfiguration['REDMINE_DB_NAME'] }
          { name: 'POSTGRES_USER', value: runtimeConfiguration['REDMINE_DB_USERNAME'] }
          { name: 'POSTGRES_PASSWORD', secretRef: 'postgres-password' }
        ]
        resources: { cpu: json('0.5'), memory: '1Gi' }
      }]
      scale: { minReplicas: 1, maxReplicas: 1 }
    }
  }
}

resource redmine 'Microsoft.App/containerApps@2024-03-01' = if (deployApplications) {
  name: redmineName
  location: location
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: { external: true, allowInsecure: false, transport: 'auto', targetPort: int(runtimeConfiguration['REDMINE_PORT']) }
      secrets: [
        { name: 'postgres-password', value: runtimeConfiguration['REDMINE_DB_PASSWORD'] }
        { name: 'redmine-admin-password', value: runtimeConfiguration['REDMINE_ADMIN_PASSWORD'] }
      ]
      registries: [{ server: registry.properties.loginServer, identity: identity.id }]
    }
    template: {
      containers: [{
        name: 'redmine'
        image: '${registry.properties.loginServer}/srm-redmine:${imageTag}'
        env: [
          { name: 'REDMINE_DB_POSTGRES', value: postgresName }
          { name: 'REDMINE_DB_DATABASE', value: runtimeConfiguration['REDMINE_DB_NAME'] }
          { name: 'REDMINE_DB_USERNAME', value: runtimeConfiguration['REDMINE_DB_USERNAME'] }
          { name: 'REDMINE_DB_PASSWORD', secretRef: 'postgres-password' }
          { name: 'SRM_REDMINE_ADMIN_USERNAME', value: runtimeConfiguration['REDMINE_ADMIN_USERNAME'] }
          { name: 'SRM_REDMINE_ADMIN_PASSWORD', secretRef: 'redmine-admin-password' }
          { name: 'SRM_REDMINE_PROJECT_IDENTIFIER', value: runtimeConfiguration['REDMINE_PROJECT_IDENTIFIER'] }
        ]
        resources: { cpu: json('0.5'), memory: '1Gi' }
        volumeMounts: [{ volumeName: 'files', mountPath: '/usr/src/redmine/files' }]
      }]
      scale: { minReplicas: 1, maxReplicas: 1 }
      volumes: [{ name: 'files', storageType: 'AzureFile', storageName: redmineEnvironmentStorageName }]
    }
  }
  dependsOn: [acrPull, postgres]
}

resource auth 'Microsoft.App/containerApps@2024-03-01' = if (deployApplications) {
  name: authName
  location: location
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: { external: false, allowInsecure: false, transport: 'auto', targetPort: int(runtimeConfiguration['DOTNET_HTTP_PORTS']) }
      registries: [{ server: registry.properties.loginServer, identity: identity.id }]
      secrets: [
        { name: 'sql-password', value: runtimeConfiguration['SQL_PASSWORD'] }
        { name: 'jwt-signing-key', value: runtimeConfiguration['JWT_SIGNING_KEY'] }
        { name: 'bootstrap-admin-password', value: runtimeConfiguration['BOOTSTRAP_ADMIN_PASSWORD'] }
      ]
    }
    template: {
      containers: [{
        name: 'auth'
        image: '${registry.properties.loginServer}/srm-auth:${imageTag}'
        env: [
          { name: 'ASPNETCORE_ENVIRONMENT', value: runtimeConfiguration['DOTNET_ENVIRONMENT'] }
          { name: 'ASPNETCORE_HTTP_PORTS', value: runtimeConfiguration['DOTNET_HTTP_PORTS'] }
          { name: 'SqlServer__Host', value: sqlName }
          { name: 'SqlServer__Port', value: runtimeConfiguration['SQL_PORT'] }
          { name: 'SqlServer__Username', value: runtimeConfiguration['SQL_USERNAME'] }
          { name: 'SqlServer__Password', secretRef: 'sql-password' }
          { name: 'SqlServer__AuthDatabase', value: runtimeConfiguration['SQL_AUTH_DATABASE'] }
          { name: 'Redis__ConnectionString', value: '${redisName}:${runtimeConfiguration['REDIS_PORT']},abortConnect=false' }
          { name: 'Jwt__Issuer', value: runtimeConfiguration['JWT_ISSUER'] }
          { name: 'Jwt__Audience', value: runtimeConfiguration['JWT_AUDIENCE'] }
          { name: 'Jwt__SigningKey', secretRef: 'jwt-signing-key' }
          { name: 'Jwt__AccessTokenLifetimeMinutes', value: runtimeConfiguration['JWT_ACCESS_TOKEN_LIFETIME_MINUTES'] }
          { name: 'BootstrapAdmin__Username', value: runtimeConfiguration['BOOTSTRAP_ADMIN_USERNAME'] }
          { name: 'BootstrapAdmin__Email', value: runtimeConfiguration['BOOTSTRAP_ADMIN_EMAIL'] }
          { name: 'BootstrapAdmin__Password', secretRef: 'bootstrap-admin-password' }
          { name: 'BootstrapAdmin__FirstName', value: runtimeConfiguration['BOOTSTRAP_ADMIN_FIRST_NAME'] }
          { name: 'BootstrapAdmin__LastName', value: runtimeConfiguration['BOOTSTRAP_ADMIN_LAST_NAME'] }
          { name: 'BootstrapAdmin__PhoneNumber', value: runtimeConfiguration['BOOTSTRAP_ADMIN_PHONE_NUMBER'] }
          { name: 'BootstrapAdmin__MustChangePassword', value: runtimeConfiguration['BOOTSTRAP_ADMIN_MUST_CHANGE_PASSWORD'] }
        ]
        resources: { cpu: json('0.5'), memory: '1Gi' }
        probes: [{ type: 'Liveness', httpGet: { path: '/health', port: int(runtimeConfiguration['DOTNET_HTTP_PORTS']) }, initialDelaySeconds: 60, periodSeconds: 30 }]
      }]
      // Authentication is latency-sensitive. A cold start can outlive the
      // calling Blazor circuit and lose an otherwise successful login.
      scale: { minReplicas: 1, maxReplicas: 1 }
    }
  }
  dependsOn: [acrPull, sql, redis]
}

resource core 'Microsoft.App/containerApps@2024-03-01' = if (deployApplications) {
  name: coreName
  location: location
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: { external: false, allowInsecure: false, transport: 'auto', targetPort: int(runtimeConfiguration['DOTNET_HTTP_PORTS']) }
      registries: [{ server: registry.properties.loginServer, identity: identity.id }]
      secrets: [
        { name: 'sql-password', value: runtimeConfiguration['SQL_PASSWORD'] }
        { name: 'jwt-signing-key', value: runtimeConfiguration['JWT_SIGNING_KEY'] }
        { name: 'redmine-api-key', value: runtimeConfiguration['REDMINE_API_KEY'] }
      ]
    }
    template: {
      containers: [{
        name: 'core'
        image: '${registry.properties.loginServer}/srm-core:${imageTag}'
        env: [
          { name: 'ASPNETCORE_ENVIRONMENT', value: runtimeConfiguration['DOTNET_ENVIRONMENT'] }
          { name: 'ASPNETCORE_HTTP_PORTS', value: runtimeConfiguration['DOTNET_HTTP_PORTS'] }
          { name: 'SqlServer__Host', value: sqlName }
          { name: 'SqlServer__Port', value: runtimeConfiguration['SQL_PORT'] }
          { name: 'SqlServer__Username', value: runtimeConfiguration['SQL_USERNAME'] }
          { name: 'SqlServer__Password', secretRef: 'sql-password' }
          { name: 'SqlServer__CoreDatabase', value: runtimeConfiguration['SQL_CORE_DATABASE'] }
          { name: 'Redis__ConnectionString', value: '${redisName}:${runtimeConfiguration['REDIS_PORT']},abortConnect=false' }
          { name: 'Jwt__Issuer', value: runtimeConfiguration['JWT_ISSUER'] }
          { name: 'Jwt__Audience', value: runtimeConfiguration['JWT_AUDIENCE'] }
          { name: 'Jwt__SigningKey', secretRef: 'jwt-signing-key' }
          { name: 'Redmine__Enabled', value: runtimeConfiguration['REDMINE_ENABLED'] }
          { name: 'Redmine__BaseUrl', value: 'http://${redmineName}' }
          { name: 'Redmine__PublicBaseUrl', value: 'https://${redmineName}.${environment.properties.defaultDomain}' }
          { name: 'Redmine__ApiKey', secretRef: 'redmine-api-key' }
          { name: 'Redmine__ProjectIdentifier', value: runtimeConfiguration['REDMINE_PROJECT_IDENTIFIER'] }
          { name: 'Redmine__TrackerId', value: runtimeConfiguration['REDMINE_TRACKER_ID'] }
          { name: 'Redmine__StatusId', value: runtimeConfiguration['REDMINE_STATUS_ID'] }
          { name: 'Redmine__PollIntervalSeconds', value: runtimeConfiguration['REDMINE_POLL_INTERVAL_SECONDS'] }
          { name: 'Redmine__WarningPriorityId', value: runtimeConfiguration['REDMINE_WARNING_PRIORITY_ID'] }
          { name: 'Redmine__MajorPriorityId', value: runtimeConfiguration['REDMINE_MAJOR_PRIORITY_ID'] }
          { name: 'Redmine__CriticalPriorityId', value: runtimeConfiguration['REDMINE_CRITICAL_PRIORITY_ID'] }
        ]
        resources: { cpu: json('0.5'), memory: '1Gi' }
        probes: [{ type: 'Liveness', httpGet: { path: '/health', port: int(runtimeConfiguration['DOTNET_HTTP_PORTS']) }, initialDelaySeconds: 60, periodSeconds: 30 }]
      }]
      // Core serves interactive CRUD requests and runs the Redmine worker.
      // Keeping it warm avoids minute-long demo writes and delayed tickets.
      scale: { minReplicas: 1, maxReplicas: 1 }
    }
  }
  dependsOn: [acrPull, sql, redis, redmine]
}

resource app 'Microsoft.App/containerApps@2024-03-01' = if (deployApplications) {
  name: appName
  location: location
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: { external: true, allowInsecure: false, transport: 'auto', targetPort: int(runtimeConfiguration['DOTNET_HTTP_PORTS']) }
      registries: [{ server: registry.properties.loginServer, identity: identity.id }]
    }
    template: {
      containers: [{
        name: 'app'
        image: '${registry.properties.loginServer}/srm-app:${imageTag}'
        env: [
          { name: 'ASPNETCORE_ENVIRONMENT', value: runtimeConfiguration['DOTNET_ENVIRONMENT'] }
          { name: 'ASPNETCORE_HTTP_PORTS', value: runtimeConfiguration['DOTNET_HTTP_PORTS'] }
          { name: 'CoreApi__BaseUrl', value: 'http://${coreName}/' }
          { name: 'AuthApi__BaseUrl', value: 'http://${authName}/' }
          { name: 'Redis__ConnectionString', value: '${redisName}:${runtimeConfiguration['REDIS_PORT']},abortConnect=false' }
        ]
        resources: { cpu: json('0.5'), memory: '1Gi' }
        probes: [{ type: 'Liveness', httpGet: { path: '/health', port: int(runtimeConfiguration['DOTNET_HTTP_PORTS']) }, initialDelaySeconds: 30, periodSeconds: 30 }]
      }]
      scale: { minReplicas: 0, maxReplicas: 1 }
    }
  }
  dependsOn: [acrPull, auth, core]
}

resource agent 'Microsoft.App/containerApps@2024-03-01' = if (deployApplications && deployAgent) {
  name: agentName
  location: location
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: { external: false, allowInsecure: false, transport: 'auto', targetPort: int(runtimeConfiguration['DOTNET_HTTP_PORTS']) }
      registries: [{ server: registry.properties.loginServer, identity: identity.id }]
      secrets: [{ name: 'agent-client-secret', value: runtimeConfiguration['AGENT_CLIENT_SECRET'] }]
    }
    template: {
      containers: [{
        name: 'agent'
        image: '${registry.properties.loginServer}/srm-agent:${imageTag}'
        env: [
          { name: 'ASPNETCORE_ENVIRONMENT', value: runtimeConfiguration['DOTNET_ENVIRONMENT'] }
          { name: 'ASPNETCORE_HTTP_PORTS', value: runtimeConfiguration['DOTNET_HTTP_PORTS'] }
          { name: 'AgentApi__AuthBaseUrl', value: 'http://${authName}/' }
          { name: 'AgentApi__CoreBaseUrl', value: 'http://${coreName}/' }
          { name: 'AgentApi__ClientIdentifier', value: runtimeConfiguration['AGENT_CLIENT_IDENTIFIER'] }
          { name: 'AgentApi__ClientSecret', secretRef: 'agent-client-secret' }
        ]
        resources: { cpu: json('0.25'), memory: '0.5Gi' }
        probes: [{ type: 'Liveness', httpGet: { path: '/health', port: int(runtimeConfiguration['DOTNET_HTTP_PORTS']) }, initialDelaySeconds: 30, periodSeconds: 30 }]
      }]
      scale: { minReplicas: 1, maxReplicas: 1 }
    }
  }
  dependsOn: [acrPull, auth, core]
}

output registryLoginServer string = registry.properties.loginServer
output appUrl string = deployApplications ? 'https://${app!.properties.configuration.ingress.fqdn}' : ''
output redmineUrl string = deployApplications ? 'https://${redmine!.properties.configuration.ingress.fqdn}' : ''
output applicationNames object = {
  auth: authName
  core: coreName
  app: appName
  agent: deployAgent ? agentName : ''
}
