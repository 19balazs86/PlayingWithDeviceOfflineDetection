﻿param storageAccountName string
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

//var signalRConnString = signalR.listKeys().primaryConnectionString

resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: 'DeviceOfflineDetection'
  location: rgLocation
  tags: tags
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
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
        // {
        //   name: 'AzureSignalRConnectionString' // Use the Service Connector to set the connection string
        //   value: signalRConnString
        // }
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

// --> Service Connector/Linker
// - Bicep: https://learn.microsoft.com/en-us/azure/templates/microsoft.servicelinker/linkers
// - Doc: https://learn.microsoft.com/en-us/azure/service-connector/how-to-integrate-signalr

// To show the resource definition
// 1) az webapp connection list --resource-group <RgName> --name <AppName>
// 2) az resource show --ids /subscriptions/<GUID>/resourceGroups/<RgName>/providers/Microsoft.Web/sites/<AppName>/providers/Microsoft.ServiceLinker/linkers/<LinkerName>

resource serviceConnector 'Microsoft.ServiceLinker/linkers@2022-11-01-preview' = {
  name: 'SignalR_Connector'
  scope: functionApp
  properties: {
    clientType: 'dotnet'
    targetService: {
      type: 'AzureResource'
      id: signalR.id
    }
    authInfo: {
      authType: 'systemAssignedIdentity'
      deleteOrUpdateBehavior: 'Default'
      roles: [] // If not defined, the default will be used: SignalR Service Owner: 7e4f1700-ea5a-4f59-8f37-079cfe29dce3
    }
    // authInfo: { // This version uses the full connection string with a key, so it does not require Managed Identity
    //   authType: 'secret'
    //   secretInfo: {
    //     secretType: 'rawValue'
    //   }
    // }
    configurationInfo: {
      customizedKeys: {
        // Change the default linker environment variable name to match the one used by Azure Function SignalR binding by default
        AZURE_SIGNALR_CONNECTIONSTRING: 'AzureSignalRConnectionString'
      }
    }
  }
}

output StorageAccountConnectionString string = storageAccountConnString