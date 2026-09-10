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
  expect(JSON.stringify(actual) === JSON.stringify(expected), `${label} has unexpected values: ${JSON.stringify(actual)}`)
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
  expect(document.info?.version === '0.1.0-m008', 'unexpected artifact version')
  expect(typeof document.info?.description === 'string', 'artifact description is required')

  sameValues(
    Object.keys(document.paths ?? {}),
    [
      '/api/capabilities',
      '/api/accounts/register',
      '/api/accounts/confirm-email',
      '/api/accounts/resend-verification',
      '/api/accounts/antiforgery',
      '/api/accounts/sign-in',
      '/api/accounts/session',
      '/api/accounts/sign-out',
    ],
    'document paths',
  )
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

  const verificationOperations = [
    {
      path: '/api/accounts/confirm-email',
      operationId: 'confirmLocalAccountEmail',
      requestSchema: 'ConfirmEmailRequest',
      successStatus: '200',
      successSchema: 'ConfirmEmailAccepted',
    },
    {
      path: '/api/accounts/resend-verification',
      operationId: 'resendLocalAccountVerification',
      requestSchema: 'ResendVerificationRequest',
      successStatus: '202',
      successSchema: 'ResendVerificationAccepted',
    },
  ]
  for (const item of verificationOperations) {
    const path = document.paths[item.path]
    sameValues(Object.keys(path), ['post'], `${item.operationId} operations`)
    const selectedOperation = path.post
    expect(selectedOperation.operationId === item.operationId, `unexpected ${item.operationId} operation ID`)
    expect(selectedOperation.security === undefined, `${item.operationId} must not declare authentication`)
    expect(
      typeof selectedOperation.summary === 'string' && typeof selectedOperation.description === 'string',
      `${item.operationId} descriptions are required`,
    )
    sameValues(
      Object.keys(selectedOperation.responses ?? {}),
      [item.successStatus, '400', '405', '415', '503'],
      `${item.operationId} responses`,
    )
    expect(selectedOperation.requestBody?.required === true, `${item.operationId} request body must be required`)
    expect(
      selectedOperation.requestBody.content?.['application/json']?.schema?.$ref ===
        `#/components/schemas/${item.requestSchema}`,
      `${item.operationId} JSON request schema is missing`,
    )
    for (const status of [item.successStatus, '400', '405', '415', '503']) {
      const selectedResponse = selectedOperation.responses[status]
      expect(selectedResponse.headers?.['Cache-Control']?.required === true, `${item.operationId} ${status} must require Cache-Control`)
      expect(selectedResponse.headers['Cache-Control'].schema?.type === 'string', `${item.operationId} ${status} Cache-Control must be a string`)
    }
    expect(
      selectedOperation.responses[item.successStatus].content?.['application/json']?.schema?.$ref ===
        `#/components/schemas/${item.successSchema}`,
      `${item.operationId} success schema is missing`,
    )
    for (const status of ['400', '415', '503']) {
      expect(
        selectedOperation.responses[status].content?.['application/problem+json']?.schema?.$ref ===
          '#/components/schemas/VerificationProblemDetails',
        `${item.operationId} ${status} Problem Details schema is missing`,
      )
    }
  }

  const confirmationRequest = document.components.schemas.ConfirmEmailRequest
  sameValues(confirmationRequest.required, ['userId', 'code'], 'confirmation request required fields')
  sameValues(Object.keys(confirmationRequest.properties), ['userId', 'code'], 'confirmation request fields')
  expect(confirmationRequest.properties.userId.minLength === 1, 'confirmation user ID minimum is missing')
  expect(confirmationRequest.properties.userId.maxLength === 450, 'confirmation user ID maximum is missing')
  expect(confirmationRequest.properties.code.minLength === 1, 'confirmation code minimum is missing')
  expect(confirmationRequest.properties.code.maxLength === 4096, 'confirmation code maximum is missing')
  expect(confirmationRequest.properties.code.writeOnly === true, 'confirmation code must be write-only')
  sameValues(document.components.schemas.ConfirmationStatus.enum, ['verified'], 'confirmation status values')

  const resendRequest = document.components.schemas.ResendVerificationRequest
  sameValues(resendRequest.required, ['email'], 'resend request required fields')
  sameValues(Object.keys(resendRequest.properties), ['email'], 'resend request fields')
  expect(resendRequest.properties.email.format === 'email', 'resend email format is missing')
  expect(resendRequest.properties.email.maxLength === 254, 'resend email maximum is missing')
  const resendAccepted = document.components.schemas.ResendVerificationAccepted
  sameValues(resendAccepted.required, ['status', 'retryAfterSeconds'], 'resend acknowledgment required fields')
  expect(resendAccepted.properties.retryAfterSeconds.type === 'integer', 'resend retry interval must be integer-only')
  sameValues(document.components.schemas.ResendVerificationStatus.enum, ['verificationRequested'], 'resend status values')

  const verificationProblem = document.components.schemas.VerificationProblemDetails
  sameValues(
    verificationProblem.required,
    ['title', 'status', 'detail', 'category', 'correlationId'],
    'verification Problem Details required fields',
  )
  sameValues(
    Object.keys(verificationProblem.properties),
    ['type', 'title', 'status', 'detail', 'category', 'correlationId', 'errors'],
    'verification Problem Details fields',
  )

  const sessionOperations = [
    { path: '/api/accounts/antiforgery', method: 'get', id: 'getAccountAntiforgeryToken', responses: ['200', '400', '405'] },
    { path: '/api/accounts/sign-in', method: 'post', id: 'signInLocalAccount', responses: ['200', '400', '401', '405', '415', '503'] },
    { path: '/api/accounts/session', method: 'get', id: 'getLocalAccountSession', responses: ['200', '400', '401', '405', '503'] },
    { path: '/api/accounts/sign-out', method: 'post', id: 'signOutLocalAccount', responses: ['204', '400', '405'] },
  ]
  for (const item of sessionOperations) {
    const path = document.paths[item.path]
    sameValues(Object.keys(path), [item.method], `${item.id} operations`)
    const selectedOperation = path[item.method]
    expect(selectedOperation.operationId === item.id, `unexpected ${item.id} operation ID`)
    expect(typeof selectedOperation.summary === 'string' && typeof selectedOperation.description === 'string', `${item.id} descriptions are required`)
    sameValues(Object.keys(selectedOperation.responses ?? {}), item.responses, `${item.id} responses`)
    for (const response of Object.values(selectedOperation.responses)) {
      expect(response.headers?.['Cache-Control']?.required === true, `${item.id} response must require Cache-Control`)
    }
  }
  expect(document.components.securitySchemes.sessionCookie.type === 'apiKey', 'session cookie scheme is missing')
  expect(document.components.securitySchemes.sessionCookie.in === 'cookie', 'session cookie scheme must be a cookie')
  expect(document.components.securitySchemes.sessionCookie.name === '__Host-LinguaDesk.Session', 'session cookie name is wrong')
  expect(document.components.securitySchemes.antiforgeryHeader.type === 'apiKey', 'antiforgery scheme is missing')
  expect(document.components.securitySchemes.antiforgeryHeader.in === 'header', 'antiforgery scheme must be a header')
  expect(document.components.securitySchemes.antiforgeryHeader.name === 'X-LinguaDesk-Antiforgery', 'antiforgery header name is wrong')
  expect(document.components.securitySchemes.antiforgeryCookie.type === 'apiKey', 'antiforgery cookie scheme is missing')
  expect(document.components.securitySchemes.antiforgeryCookie.in === 'cookie', 'antiforgery cookie scheme must be a cookie')
  expect(document.components.securitySchemes.antiforgeryCookie.name === '__Host-LinguaDesk.Antiforgery', 'antiforgery cookie name is wrong')
  expect(document.paths['/api/accounts/session'].get.security?.[0]?.sessionCookie !== undefined, 'session cookie security is missing')
  expect(document.paths['/api/accounts/sign-in'].post.security?.[0]?.antiforgeryHeader !== undefined, 'sign-in antiforgery security is missing')
  expect(document.paths['/api/accounts/sign-in'].post.security?.[0]?.antiforgeryCookie !== undefined, 'sign-in antiforgery cookie security is missing')
  expect(document.paths['/api/accounts/sign-out'].post.security?.[0]?.antiforgeryHeader !== undefined, 'sign-out antiforgery security is missing')
  expect(document.paths['/api/accounts/sign-out'].post.security?.[0]?.antiforgeryCookie !== undefined, 'sign-out antiforgery cookie security is missing')
  expect(document.paths['/api/accounts/antiforgery'].get.security === undefined, 'bootstrap must remain anonymous')
  expect(document.paths['/api/accounts/sign-in'].post.requestBody?.content?.['application/json']?.schema?.$ref === '#/components/schemas/SignInRequest', 'sign-in request schema is missing')
  const signInRequest = document.components.schemas.SignInRequest
  sameValues(signInRequest.required, ['email', 'password'], 'sign-in required fields')
  expect(signInRequest.properties.email.format === 'email' && signInRequest.properties.email.maxLength === 254, 'sign-in email constraints are missing')
  expect(signInRequest.properties.password.format === 'password' && signInRequest.properties.password.minLength === 15 && signInRequest.properties.password.maxLength === 128 && signInRequest.properties.password.writeOnly === true, 'sign-in password constraints are missing')

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
