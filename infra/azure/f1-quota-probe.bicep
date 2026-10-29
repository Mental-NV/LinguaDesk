targetScope = 'resourceGroup'
param location string
param planName string

// Validation only: a NEW plan exercises allocation quota rather than an update
// to an existing plan. No Web App, secret, reference() or nested deployment.
resource plan 'Microsoft.Web/serverfarms@2024-11-01' = {
  name: planName
  location: location
  kind: 'linux'
  sku: { name: 'F1', tier: 'Free', capacity: 1 }
  properties: { reserved: true }
}
