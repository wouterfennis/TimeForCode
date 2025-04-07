param location string = resourceGroup().location
param appServicePlanName string
@description('The name of the App Service. It should be between 3 and 24 characters and can contain only alphanumeric characters and hyphens.')
param appServiceName string
param imageName string

resource appServicePlan 'Microsoft.Web/serverfarms@2021-02-01' existing = {
  name: appServicePlanName
  scope: resourceGroup('rg-infrastructure')
}

resource appService 'Microsoft.Web/sites@2021-02-01' = {
  name: appServiceName
  location: location
  kind: 'app,linux'
  properties: {
     siteConfig:{
       linuxFxVersion: 'DOCKER|${imageName}'
       appSettings: [
         {
           name: 'DOCKER_REGISTRY_SERVER_URL'
           value: 'https://ghcr.io'
         }
       ]
    }
    clientCertEnabled: false
    httpsOnly: true
    serverFarmId: appServicePlan.id
  }
  identity: {
    type: 'SystemAssigned'
  }
}

output websiteEndpoint string = appService.properties.defaultHostName
