targetScope = 'subscription'
param oidcSubjectPrefix string
var configuration = loadJsonContent('./environments.json')
var environments = ['Production']
resource groups 'Microsoft.Resources/resourceGroups@2024-03-01' = [for environment in environments: {
  name: configuration[environment].resourceGroup
  location: configuration[environment].location
}]
module identity 'modules/identity.bicep' = [for (environment, i) in environments: {
  scope: groups[i]
  name: 'identity-${environment}'
  params: {
    name: configuration[environment].identityName
    location: configuration[environment].location
    oidcSubjectPrefix: oidcSubjectPrefix
    environment: environment
  }
}]
module roles 'modules/role.bicep' = [for (environment, i) in environments: {
  scope: groups[i]
  name: 'role-${environment}'
  params: { principalId: identity[i].outputs.principalId }
}]
output environments array = [for (environment, i) in environments: {
  environment: environment
  clientId: identity[i].outputs.clientId
  resourceGroup: groups[i].name
}]
