param location string = resourceGroup().location
param appServicePlanName string
@description('The name of the App Service. It should be between 3 and 24 characters and can contain only alphanumeric characters and hyphens.')
param appServiceName string
param dockerHubImageName string

resource appServicePlan 'Microsoft.Web/serverfarms@2021-02-01' existing = {
  name: appServicePlanName
}

resource appService 'Microsoft.Web/sites@2021-02-01' = {
  name: appServiceName
  location: location
  properties: {
     siteConfig:{
       linuxFxVersion: 'DOCKER|${dockerHubImageName}'
       appSettings: [
         {
           name: 'DOCKER_REGISTRY_SERVER_URL'
           value: 'https://ghcr.io'
         }
       ]
     }
    serverFarmId: appServicePlan.id
  }
}

output appServiceEndpoint string = appService.properties.defaultHostName
