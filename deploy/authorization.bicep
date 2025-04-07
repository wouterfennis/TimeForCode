targetScope = 'subscription'
param location string = 'WestEurope'

resource rgInfrastructure 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'rg-infrastructure'
  location: location
}

resource rgAuthApp 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'rg-auth-app'
  location: location
}

module appServicePlanModule 'appServicePlan.bicep' = {
  scope: rgInfrastructure
  name: 'appServicePlanModule'
  params: {
    location: location
  }
}

// module apiAppServiceModule 'appService.bicep' = {
//   scope: rgAuthApp
//   name: 'apiAppServiceModule'
//   params: {
//     location: location
//     appServicePlanName: appServicePlanModule.outputs.name
//   	appServiceName: 'timeforcode-app-auth'
//   	imageName: 'ghcr.io/wouterfennis/timeforcode/timeforcode-authorization-api:latest'
//   }
// }

module websiteAppServiceModule 'appService.bicep' = {
  scope: rgAuthApp
  name: 'websiteAppServiceModule'
  params: {
    location: location
    appServicePlanName: appServicePlanModule.outputs.name
  	appServiceName: 'timeforcode-app-auth'
  	imageName: 'ghcr.io/wouterfennis/timeforcode/timeforcode-website:latest'
  }
}
