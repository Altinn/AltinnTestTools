# AltinnTestTools
Various tools used for testing Altinn

## Token Generator

Simple HTTP API for generating arbitrary enterprise and person access tokens for test environments, used for automated testing. Matches tokens produced by [Altinn3 token exchange](https://docs.altinn.studio/altinn-api/authentication/#exchange-of-jwt-token) or consent-token endpoints. Implemented as a Azure Function running at https://altinn-testtools-token-generator.azurewebsites.net.

### Access

The application requires authentication. See https://altinn.github.io/docs/api/rest/kom-i-gang/tokengenerator/ for more information.

### Usage:

`{environment}` is an Altinn test environment name eg. `at24` og `tt02`

#### Enterprise tokens (aka Maskinporten):
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetEnterpriseToken?env={environment}&scopes={scopes}&org={orgName}&orgNo={orgNo}`

#### Enterprise user tokens (aka Maskinporten + Enterprise user authentication):
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetEnterpriseUserToken?env={environment}&orgNo={orgNo}&partyId={partyId}&userId={userId}&userName={userName}`

#### Person tokens (aka ID-porten)
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetPersonalToken?env={environment}&scopes={scopes}&userId={userId}&partyId={partyId}&pid={pid}`

#### Optional parameters:

* `supplierOrgNo` (Enterprise tokens only)
* `ttl` (Default: 1800 seconds)
* `authLvl` (Personal tokens only. Default: `3`)
* `consumerOrgNo` (Personal tokens only. Default: `991825827`)
* `userName` (Personal tokens only. Default: *empty string*)
* `clientAmr` (Personal tokens only. Default: `virksomhetssertifikat`)
* `dump` (displays a human readable decoded JWT)

#### Consent tokens: 
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetConsentToken?env={environment}&serviceCodes={serviceCodes}&offeredBy={offeredBy}&coveredBy={coveredBy}`

* `serviceCodes` is a delimited list of `{servicesCode}_{serviceEditionCode}.{servicesCode2}_{serviceEditionCode2}`, eg. `5120_1.5678_2`
* `offeredBy`, `coveredBy` and `handledBy` (optional) is a 9 or 11 long all-digit string. 
* `authorizationCode` is av valid GUID. If omitted a random one is used.
* `ttl` (optional) is the valid lifetime of the token in seconds. Default: 30 seconds.

Consent service metadata (parameters) are supplied the same way as in [URL-based consent requests](https://altinn.github.io/docs/utviklingsguider/samtykke/datakonsument/be-om-samtykke/lenkebasert-legacy/), eg. `..&5120_1_myparam=myvalue`.

## TokenGeneratorCli

Simple console app for calling the API from a shell script. Uses `appsettings.json` for default settings, all of which can be overridden by command line arguments. 

### Usage :

Build, and run `TokenGeneratorCli.exe --help`
