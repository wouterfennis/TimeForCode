targetScope = 'subscription'
param location string = 'WestEurope'

var parameters = loadJsonContent('parameters.json')

resource rgInfrastructure 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: parameters.applicationInfrastructure.resourceGroup
  location: location
}

module appServicePlanModule 'modules/appServicePlan.bicep' = {
  scope: rgInfrastructure
  name: 'appServicePlanModule'
  params: {
    location: location
    appServicePlanParameters: parameters.applicationInfrastructure.appServicePlan
  }
}
