using './main.bicep'
param environment = 'Production'
param dailyCapMb = 50
param providerKey = readEnvironmentVariable('DEEPSEEK_API_KEY')
