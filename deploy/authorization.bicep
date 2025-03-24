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

module appServiceModule 'appService.bicep' = {
  scope: rgAuthApp
  name: 'appServiceModule'
  params: {
    location: location
    appServicePlanName: appServicePlanModule.outputs.name
  	appServiceName: 'app-auth'
  	dockerHubImageName: 'mydockerhub/myimage:latest'
  }
}
