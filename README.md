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

#### Person tokens (aka ID-porten)
`https://altinn-token-generator.azurewebsites.net/apiGetPersonalToken?env={environment}&scopes={scopes}&userid={userId}&partyId={partyId}&pid={pid}`

#### Optional parameters:

* `supplierOrgNo` (Enterprise tokens only)
* `ttl` (Default: 1800 seconds)
* `authlvl` (Personal tokens only. Default: `3`)
* `consumerOrgNo` (Personal tokens only. Default: `991825827`)
* `username` (Personal tokens only. Default: *empty string*)
* `client_amr` (Personal tokens only. Default: `virksomhetssertifikat`)