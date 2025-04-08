param location string = resourceGroup().location
param appServicePlanName string
param appServicePlanResourceGroup string
@description('The name of the App Service. It should be between 3 and 24 characters and can contain only alphanumeric characters and hyphens.')
param appServiceName string
param imageName string
@description('Additional app settings to configure for the App Service.')
param additionalAppSettings array = []

resource appServicePlan 'Microsoft.Web/serverfarms@2021-02-01' existing = {
  name: appServicePlanName
  scope: resourceGroup(appServicePlanResourceGroup)
}

resource appService 'Microsoft.Web/sites@2021-02-01' = {
  name: appServiceName
  location: location
  kind: 'app,linux'
  properties: {
     siteConfig:{
       linuxFxVersion: 'DOCKER|${imageName}'
       appSettings: union([
        {
          name: 'DOCKER_REGISTRY_SERVER_URL'
          value: 'https://ghcr.io'
        }
      ], additionalAppSettings)
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
