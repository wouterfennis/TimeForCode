param location string
param appServicePlanParameters object

resource appServicePlan 'Microsoft.Web/serverfarms@2021-02-01' = {
  name: appServicePlanParameters.name
  location: location
  sku: {
    name: appServicePlanParameters.sku
    tier: appServicePlanParameters.tier
    capacity: appServicePlanParameters.capacity
    size: appServicePlanParameters.size
    family: appServicePlanParameters.family
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

output name string = appServicePlan.name

