param location string = resourceGroup().location

// Note that the max length of prefix + name can be max 24 characters long for storage accounts.
var prefix = 'FunctionApp-a7d6f5h38a'

var serverFarmName = '${prefix}asp'
var functionAppName = '${prefix}func'
var storageAccountName = '${prefix}st'
var applicationInsightsName = '${prefix}appi'

var queueClientName = 'client-queue'
var queueExternalName = 'external-queue'

var storageAccountConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'

resource serverFarm 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: serverFarmName
  location: location
  tags: resourceGroup().tags
  sku: {
    tier: 'Consumption'
    name: 'Y1'
  }
  kind: 'elastic'
}

resource functionApp 'Microsoft.Web/sites@2021-03-01' = {
  name: functionAppName
  location: location
  tags: resourceGroup().tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
        '${managedIdentity.id}': {}
    }
  }
  kind: 'functionapp'
  properties: {
    enabled: true
    serverFarmId: serverFarm.id
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      http20Enabled: true
    }
    clientAffinityEnabled: false
    httpsOnly: true
    containerSize: 1536
    redundancyMode: 'None'
  }

  resource functionAppConfig 'config@2021-03-01' = {
    name: 'appsettings'
    properties: {
        // function app settings
        FUNCTIONS_EXTENSION_VERSION: '~4'
        FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
        WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED: '1'
        AzureWebJobsStorage: storageAccountConnectionString
        WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: storageAccountConnectionString
        WEBSITE_CONTENTSHARE: toLower(functionAppName)
        AZURE_CLIENT_ID: managedIdentity.properties.clientId
        // application insights settings
        APPLICATIONINSIGHTS_CONNECTION_STRING: reference('Microsoft.Insights/components/${applicationInsights.name}', '2015-05-01').ConnectionString
        ApplicationInsightsAgent_EXTENSION_VERSION: '~2'
      }
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  tags: resourceGroup().tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: true
    minimumTlsVersion: 'TLS1_2'
    accessTier: 'Hot'
    publicNetworkAccess: 'Enabled'
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
    name: applicationInsightsName
    location: location
    tags: resourceGroup().tags
    kind: 'web'
    properties: {
        Application_Type: 'web'
        Flow_Type: 'Bluefield'
        Request_Source: 'rest'
        RetentionInDays: 30
    }
}

resource queueServices 'Microsoft.Storage/storageAccounts/queueServices@2023-05-01' = {
    name: 'default'
    parent: storageAccount
}

resource queueClient 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
    name: queueClientName
    parent: queueServices
}

resource queueExternal 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
    name: queueExternalName
    parent: queueServices
}

resource blobContributorDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
    scope: resourceGroup()
    name: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
    name: 'MyManagedIdentity'
    location: location
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(subscription().id, managedIdentity.id, blobContributorDefinition.id)
    properties: {
        roleDefinitionId: blobContributorDefinition.id
        principalId: managedIdentity.properties.principalId
        principalType: 'ServicePrincipal'
    }
}
