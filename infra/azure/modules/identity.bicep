param name string
param location string
param repository string
param environment string
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: name
  location: location
}
resource federation 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: identity
  name: 'github'
  properties: {
    issuer: 'https://token.actions.githubusercontent.com'
    subject: 'repo:${repository}:environment:${environment}'
    audiences: ['api://AzureADTokenExchange']
  }
}
output clientId string = identity.properties.clientId
output principalId string = identity.properties.principalId
