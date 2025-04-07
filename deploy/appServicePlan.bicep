param location string

resource appServicePlan 'Microsoft.Web/serverfarms@2021-02-01' = {
  name: 'myAppServicePlan'
  location: location
  kind: 'linux'
  sku: {
    name: 'F1'
    tier: 'Free'
    capacity: 1
    size: 'F1'
    family: 'F'
  }
  properties: {
    reserved: true
  }
}

output name string = appServicePlan.name

