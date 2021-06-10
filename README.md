# AltinnTestTools
Various tools used for testing Altinn

## Token Generator

Simple HTTP API for generating arbitrary enterprise and person access tokens for test environments, used for automated testing. Matches tokens produced by [Altinn3 token exchange](https://docs.altinn.studio/altinn-api/authentication/#exchange-of-jwt-token). Implemented as a Azure Function running at https://altinn-token-generator.azurewebsites.net.

### Access

The application requires authentication. Currently only available for internal use in Digdir. Contact the repo owner for more information.

### Usage:

`{environment}` is an Altinn Cloud environment name eg. `at24`

#### Enterprise tokens (aka Maskinporten):
`https://altinn-token-generator.azurewebsites.net/api/GetEnterpriseToken?env={environment}&scopes={scopes}&org={orgName}&orgNo={orgNo}`

#### Enterprise user tokens (aka Maskinporten + Enterprise user authentication):
`https://altinn-token-generator.azurewebsites.net/api/GetEnterpriseUserToken?env={environment}&orgNo={orgNo}&partyId={partyId}&userId={userId}&userName={userName}`

#### Person tokens (aka ID-porten)
`https://altinn-token-generator.azurewebsites.net/api/GetPersonalToken?env={environment}&scopes={scopes}&userId={userId}&partyId={partyId}&pid={pid}`

#### Optional parameters:

* `supplierOrgNo` (Enterprise tokens only)
* `ttl` (Default: 1800 seconds)
* `authLvl` (Personal tokens only. Default: `3`)
* `consumerOrgNo` (Personal tokens only. Default: `991825827`)
* `userName` (Personal tokens only. Default: *empty string*)
* `clientAmr` (Personal tokens only. Default: `virksomhetssertifikat`)

## TokenGeneratorCli

Simple console app for calling the API from a shell script. Uses `appsettings.json` for default settings, all of which can be overridden by command line arguments. 

### Usage :

Build, and run `TokenGeneratorCli.exe --help`
