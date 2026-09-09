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
    sameValues(schema.required, Object.keys(schema.properties), `${schemaName}.required`)
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
  expect(document.info?.version === '0.1.0-m005', 'unexpected artifact version')
  expect(typeof document.info?.description === 'string', 'artifact description is required')

  sameValues(Object.keys(document.paths ?? {}), ['/api/capabilities'], 'document paths')
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
