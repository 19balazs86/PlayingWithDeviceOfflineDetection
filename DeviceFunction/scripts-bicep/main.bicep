param storageAccountName string
param tags object
param rgLocation string = resourceGroup().location

// --> Storage account
// https://learn.microsoft.com/en-us/azure/templates/microsoft.storage/storageaccounts

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: rgLocation
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
}

// --> SignalR
// https://learn.microsoft.com/en-us/azure/templates/microsoft.signalrservice/signalr

resource signalR 'Microsoft.SignalRService/signalR@2023-08-01-preview' = {
  name: 'DeviceOfflineDetection-Test'
  location: rgLocation
  tags: tags
  sku: {
    name: 'Free_F1'
  }
  properties: {
    features: [
      {
        flag: 'ServiceMode'
        value: 'Serverless'
      }
    ]
  }
}

// --> LogAnalytics workspace
// https://learn.microsoft.com/en-us/azure/templates/microsoft.operationalinsights/workspaces

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'DeviceOfflineDetection'
  location: rgLocation
  tags: tags
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

// --> Application Insights
// https://learn.microsoft.com/en-us/azure/templates/microsoft.insights/components

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'DeviceOfflineDetection'
  location: rgLocation
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// --> App Service plan
// https://learn.microsoft.com/en-us/azure/templates/microsoft.web/serverfarms

resource hostingPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: 'DeviceOfflineDetection'
  location: rgLocation
  tags: tags
  kind: '' // Leave it empty for windows. Otherwise: 'linux' and required to set properties.reserved: true
  sku: { // 'Y1-Dynamic' is for Consumption plan Functions
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {}
}

// --> Function App
// https://learn.microsoft.com/en-us/azure/templates/microsoft.web/sites

var storageAccountConnString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
var signalRConnString = signalR.listKeys().primaryConnectionString

resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: 'DeviceOfflineDetection'
  location: rgLocation
  tags: tags
  kind: 'functionapp'
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      functionAppScaleLimit: 3
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageAccountConnString
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: storageAccountConnString
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'AzureSignalRConnectionString'
          value: signalRConnString
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower('DeviceOfflineDetection')
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED'
          value: '1'
        }
      ]
    }
  }
}

output StorageAccountConnectionString string = storageAccountConnString