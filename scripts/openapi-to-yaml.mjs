import { readFile, writeFile } from 'node:fs/promises'
import { createRequire } from 'node:module'
import process from 'node:process'

const require = createRequire(import.meta.url)
const YAML = require('../frontend/node_modules/yaml')

function fail(message) {
  throw new Error(`OpenAPI contract check failed: ${message}`)
}

function expect(condition, message) {
  if (!condition) fail(message)
}

function sameValues(actual, expected, label) {
  expect(Array.isArray(actual), `${label} must be an array`)
  expect(JSON.stringify(actual) === JSON.stringify(expected), `${label} has unexpected values`)
}

function referencedSchema(document, reference) {
  const prefix = '#/components/schemas/'
  expect(typeof reference === 'string' && reference.startsWith(prefix), `unexpected schema reference ${reference}`)
  const name = reference.slice(prefix.length)
  const schema = document.components?.schemas?.[name]
  expect(schema !== undefined, `missing referenced schema ${name}`)
  return schema
}

function assertPropertyDescriptions(document) {
  for (const [schemaName, schema] of Object.entries(document.components.schemas)) {
    if (schema.properties === undefined) continue
    for (const [propertyName, property] of Object.entries(schema.properties)) {
      expect(
        typeof property.description === 'string' && property.description.length > 0,
        `${schemaName}.${propertyName} needs a semantic description`,
      )
    }
  }
}

function validateContract(document) {
  expect(/^3\.1(?:\.|$)/u.test(document.openapi), 'document must use OpenAPI 3.1')
  expect(document.info?.title === 'LinguaDesk API', 'unexpected API title')
  expect(document.info?.version === '0.1.0-m006', 'unexpected artifact version')
  expect(typeof document.info?.description === 'string', 'artifact description is required')

  sameValues(Object.keys(document.paths ?? {}), ['/api/capabilities', '/api/accounts/register'], 'document paths')
  const pathItem = document.paths['/api/capabilities']
  sameValues(Object.keys(pathItem), ['get'], 'capabilities operations')
  const operation = pathItem.get
  expect(operation.operationId === 'getCapabilities', 'unexpected operation ID')
  expect(typeof operation.summary === 'string' && typeof operation.description === 'string', 'operation descriptions are required')
  expect(operation.security === undefined, 'public capabilities operation must not declare authentication')
  sameValues(Object.keys(operation.responses ?? {}), ['200'], 'capabilities responses')

  const response = operation.responses['200']
  expect(response.headers?.['Cache-Control']?.required === true, 'required Cache-Control response header is missing')
  expect(response.headers['Cache-Control'].schema?.type === 'string', 'Cache-Control header must be a string')
  const responseSchema = response.content?.['application/json']?.schema
  expect(responseSchema?.$ref === '#/components/schemas/CapabilitiesResponse', 'JSON response schema is missing')

  const registrationPath = document.paths['/api/accounts/register']
  sameValues(Object.keys(registrationPath), ['post'], 'registration operations')
  const registration = registrationPath.post
  expect(registration.operationId === 'registerLocalAccount', 'unexpected registration operation ID')
  expect(
    typeof registration.summary === 'string' && typeof registration.description === 'string',
    'registration descriptions are required',
  )
  expect(registration.security === undefined, 'anonymous registration must not declare authentication')
  sameValues(Object.keys(registration.responses ?? {}), ['202', '400', '415', '503'], 'registration responses')
  expect(registration.requestBody?.required === true, 'registration request body must be required')
  expect(
    registration.requestBody.content?.['application/json']?.schema?.$ref === '#/components/schemas/RegistrationRequest',
    'registration JSON request schema is missing',
  )
  for (const status of ['202', '400', '415', '503']) {
    const registrationResponse = registration.responses[status]
    expect(
      registrationResponse.headers?.['Cache-Control']?.required === true,
      `registration ${status} must require Cache-Control`,
    )
    expect(
      registrationResponse.headers['Cache-Control'].schema?.type === 'string',
      `registration ${status} Cache-Control must be a string`,
    )
  }
  expect(
    registration.responses['202'].content?.['application/json']?.schema?.$ref ===
      '#/components/schemas/RegistrationAccepted',
    'registration 202 schema is missing',
  )
  for (const status of ['400', '415', '503']) {
    expect(
      registration.responses[status].content?.['application/problem+json']?.schema?.$ref ===
        '#/components/schemas/RegistrationProblemDetails',
      `registration ${status} Problem Details schema is missing`,
    )
  }

  const registrationRequest = document.components.schemas.RegistrationRequest
  sameValues(registrationRequest.required, ['email', 'password'], 'registration request required fields')
  sameValues(Object.keys(registrationRequest.properties), ['email', 'password'], 'registration request fields')
  expect(registrationRequest.properties.email.format === 'email', 'registration email format is missing')
  expect(registrationRequest.properties.email.maxLength === 254, 'registration email maximum is missing')
  expect(registrationRequest.properties.password.format === 'password', 'registration password format is missing')
  expect(registrationRequest.properties.password.minLength === 15, 'registration password minimum is missing')
  expect(registrationRequest.properties.password.maxLength === 128, 'registration password maximum is missing')
  expect(registrationRequest.properties.password.writeOnly === true, 'registration password must be write-only')

  const registrationAccepted = document.components.schemas.RegistrationAccepted
  sameValues(registrationAccepted.required, ['status'], 'registration acknowledgment required fields')
  expect(
    registrationAccepted.properties.status.$ref === '#/components/schemas/RegistrationStatus',
    'registration acknowledgment status schema is missing',
  )
  sameValues(
    document.components.schemas.RegistrationStatus.enum,
    ['verificationRequired'],
    'registration status values',
  )

  const registrationProblem = document.components.schemas.RegistrationProblemDetails
  sameValues(
    registrationProblem.required,
    ['title', 'status', 'detail', 'category', 'correlationId'],
    'registration Problem Details required fields',
  )
  sameValues(
    Object.keys(registrationProblem.properties),
    ['type', 'title', 'status', 'detail', 'category', 'correlationId', 'errors'],
    'registration Problem Details fields',
  )

  const root = referencedSchema(document, responseSchema.$ref)
  const topLevelFields = [
    'serverTimeUtc',
    'languages',
    'sourceSelection',
    'chineseScriptPolicy',
    'countingPolicy',
    'translation',
    'rewriting',
    'operationIdentity',
  ]
  sameValues(root.required, topLevelFields, 'capability response required fields')
  sameValues(Object.keys(root.properties), topLevelFields, 'capability response fields')
  expect(root.properties.serverTimeUtc.type === 'string', 'serverTimeUtc must be a string')
  expect(root.properties.serverTimeUtc.format === 'date-time', 'serverTimeUtc must use date-time format')

  const expectedEnums = {
    ChineseScript: ['simplified', 'traditional'],
    CountingUnit: ['unicodeScalar'],
    EmptyOrWhitespacePolicy: ['reject'],
    InvalidUnicodePolicy: ['reject'],
    LanguageId: ['en', 'ru', 'ro', 'zh'],
    LineEndingPolicy: ['preserve'],
    NormalizationPolicy: ['none'],
    OperationIdentityFormat: ['uuidV7'],
    OversizeHandling: ['rejectWhole'],
    RewritingModeId: [
      'correctionOnly',
      'simple',
      'casual',
      'business',
      'academic',
      'enthusiastic',
      'friendly',
      'confident',
      'diplomatic',
    ],
    RewritingModeKind: ['correction', 'style', 'tone'],
    SourceSelectionValue: ['auto', 'en', 'ru', 'ro', 'zh'],
  }
  for (const [schemaName, values] of Object.entries(expectedEnums)) {
    const schema = document.components?.schemas?.[schemaName]
    expect(schema?.type === 'string', `${schemaName} must be a string enum`)
    sameValues(schema.enum, values, `${schemaName}.enum`)
  }

  for (const [schemaName, propertyNames] of Object.entries({
    TranslationCapability: ['maximumSourceCharacters', 'overallDeadlineSeconds'],
    RewritingCapability: ['maximumSourceCharacters', 'overallDeadlineSeconds'],
    OperationIdentityCapability: ['validForSeconds', 'maximumFutureSkewSeconds'],
  })) {
    const schema = document.components.schemas[schemaName]
    for (const propertyName of propertyNames) {
      expect(schema.properties[propertyName].type === 'integer', `${schemaName}.${propertyName} must be integer-only`)
    }
  }

  assertPropertyDescriptions(document)
}

if (process.argv.length !== 4) {
  process.stderr.write('Usage: node scripts/openapi-to-yaml.mjs <input.json> <output.yaml>\n')
  process.exit(2)
}

const [, , inputPath, outputPath] = process.argv
const source = await readFile(inputPath, 'utf8')
const document = JSON.parse(source)
validateContract(document)

const yaml = YAML.stringify(document, {
  indent: 2,
  lineWidth: 0,
  sortMapEntries: false,
})
await writeFile(
  outputPath,
  `# Generated from C# endpoint metadata by scripts/contract.sh. Do not edit by hand.\n${yaml}`,
  'utf8',
)
