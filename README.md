# AltinnTestTools
Various tools used for testing Altinn

## Token Generator

Simple HTTP API for generating arbitrary enterprise and person access tokens for test environments, used for automated testing. Matches tokens produced by [Altinn3 token exchange](https://docs.altinn.studio/altinn-api/authentication/#exchange-of-jwt-token) or consent-token endpoints, using the key material for the supplied environment. Implemented as an Azure Function running at https://altinn-testtools-token-generator.azurewebsites.net.

### Access

The token endpoints require authentication. The API accepts either:

* Basic authentication, using users configured in `Settings:BasicAuthorizationUsers`.
* Bearer authentication, where the token is validated against `Settings:TokenAuthorizationWellKnownEndpoint` and must contain a scope that grants access to the requested endpoint.

The `.well-known` endpoints are anonymous. See https://altinn.github.io/docs/api/rest/kom-i-gang/tokengenerator/ for more information about access to the hosted instance.

### Usage:

`{environment}` is an Altinn test environment name, for example `at24`, `tt02` or `dev`.

`none` is also a valid value for API token endpoints, which indicates that the token generator itself becomes the issuer and uses its own key material for signing. See "Issuer" and "Bulk mode" below. Consent tokens and platform access tokens require an environment with configured Key Vault certificate material.


#### Enterprise tokens (aka Maskinporten):
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetEnterpriseToken?env={environment}&scopes={scopes}&org={orgName}&orgNo={orgNo}&clientId={clientId}`

Required parameters:

* `env`
* `scopes`

If `orgNo` is omitted, a random organization number from `TokenGenerator/Data/enterprises.txt` is used.

#### Person tokens (aka ID-porten)
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetPersonalToken?env={environment}&scopes={scopes}&userId={userId}&partyId={partyId}&pid={pid}`

Required parameters:

* `env`

If `scopes` is omitted, `altinn:enduser` is used. If `pid` is omitted, a random person identifier from `TokenGenerator/Data/endusers.txt` is used.

#### Systemuser tokens ([see docs](https://docs.digdir.no/docs/Maskinporten/maskinporten_func_systembruker))
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetSystemUserToken?env={environment}&scopes={scopes}&orgNo={orgNo}&systemUserId={systemUserId}&systemUserOrg={systemUserOrg}&clientId={clientId}`

Required parameters:

* `env`
* `systemUserId`

If `scopes` is omitted, `altinn:enduser` is used. If `orgNo` or `systemUserOrg` is omitted, `991825827` is used. If `clientId` is omitted, a random GUID is used.

#### Legacy (Altinn 2) Enterprise user tokens (aka Maskinporten + Enterprise user authentication):
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetEnterpriseUserToken?env={environment}&orgNo={orgNo}&partyId={partyId}&userId={userId}&userName={userName}`

Required parameters:

* `env`
* `orgNo`
* `partyId`
* `userId`
* `userName`

If `scopes` is omitted, `altinn:enduser` is used.

#### Legacy (Altinn 2) self-identified user tokens
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetSelfIdentifiedUserToken?env={environment}&partyId={partyId}&userId={userId}&username={username}`

Required parameters:

* `env`

If `scopes` is omitted, `altinn:portal/enduser` is used. If `partyId`, `partyuuid` or `username` is omitted, random values are used.

#### ID-porten self-registered email user tokens
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetSelfRegisteredEmailUserToken?env={environment}&partyId={partyId}&userId={userId}&email={email}`

Required parameters:

* `env`
* `email`

If `scopes` is omitted, `altinn:portal/enduser` is used. If `partyId` or `partyuuid` is omitted, random values are used.

#### Platform tokens
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetPlatformToken?env={environment}&app={app}`

This creates a legacy platform access token signed with the regular API token certificate for the environment.

Required parameters:

* `env`
* `app`

#### Platform access tokens
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetPlatformAccessToken?env={environment}&app={app}&org={issuer}`

This creates a platform access token signed with the platform or TTD access token certificate for the environment.

Required parameters:

* `env`
* `app`

If `org` is omitted, `Settings:PlatformAccessTokenIssuerName` is used. In the default local settings, supported issuer values are `platform` and `ttd`.

#### Optional parameters:

* `ttl`: Token lifetime in seconds. Default is `1800` for all token types except consent tokens, where the default is `30`.
* `dump`: Displays a human-readable decoded JWT instead of the raw token.
* `supplierOrgNo`: Enterprise, enterprise user and system user tokens only.
* `clientId`: Enterprise and system user tokens only. Default is a random GUID in the `client_id` claim.
* `delegationSource`: Enterprise, personal and enterprise user tokens only. Must be an absolute URI.
* `partyuuid`: Personal, enterprise user, self-identified user and self-registered email user tokens only.
* `authLvl`: Personal tokens only. Default: `3`.
* `consumerOrgNo`: Personal tokens only.
* `userName`: Personal and enterprise user tokens only. Default for personal tokens: empty string.
* `username`: Self-identified user tokens only. Default: random `SIUser####` value.
* `clientAmr`: Personal tokens only. Default: `virksomhetssertifikat`.
* `bulkCount`: Personal or enterprise tokens only. Enables bulk mode. See below.

#### Consent tokens: 
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetConsentToken?env={environment}&serviceCodes={serviceCodes}&offeredBy={offeredBy}&coveredBy={coveredBy}`

Required parameters:

* `env`
* `serviceCodes`
* `offeredBy`
* `coveredBy`

* `serviceCodes` is a delimited list of `{servicesCode}_{serviceEditionCode}.{servicesCode2}_{serviceEditionCode2}`, eg. `5120_1.5678_2`
* `offeredBy`, `coveredBy` and `handledBy` (optional) must be either a 9-digit organization number or an 11-digit person identifier.
* `authorizationCode` is a valid GUID. If omitted, a random one is used.
* `ttl` (optional) is the valid lifetime of the token in seconds. Default: 30 seconds.
* `dump` (optional) displays a human-readable decoded JWT.

Consent service metadata (parameters) are supplied the same way as in [URL-based consent requests](https://altinn.github.io/docs/utviklingsguider/samtykke/datakonsument/be-om-samtykke/lenkebasert-legacy/), eg. `..&5120_1_myparam=myvalue`.

#### Issuer

The token generator can act as its own issuer, using its own key material for signing. This is useful for testing scenarios where the Altinn environment is irrelevant/unreachable or higher performance is required. 
[RFC 8414 OAuth 2.0 Authorization Server Metadata](https://datatracker.ietf.org/doc/html/rfc8414) with public key material is available at `https://altinn-testtools-token-generator.azurewebsites.net/api/.well-known/oauth-authorization-server`

This can be utilized in ASP.NET with something like this:
```
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MetadataAddress = "https://altinn-testtools-token-generator.azurewebsites.net/api/.well-known/oauth-authorization-server";
        options.RequireHttpsMetadata = true;
    });
```

#### Bulk mode

For personal and enterprise tokens, the token generator can also return a list of multiple tokens in a single request, containing random pids or organization numbers, selected from a curated list of Tenor test data. This is useful for performance testing or when a large number of tokens are needed.

All parameters are the same as for single tokens, but the `pid` or `orgNo` parameter is replaced with `bulkCount` to specify the number of tokens to generate. This will return a JSON dictionary with the tokens as values and the corresponding pids or org numbers as keys. 

It is _highly recommended_ to use the `none` environment when using bulk mode, which will cause the token generator to use its own key material for signing. Using other environments such as `at24` or `tt02` is accepted, but will be _excruciatingly_ slow as generating lots of RSA256 tokens is computationally expensive (1000 tokens takes easily 1 minute). The token generator issued tokens uses EcDSA256, which is several orders of magnitude faster than RSA256 (generating thousands of tokens in milliseconds).

### Local development

The function app uses the .NET isolated worker model and targets .NET 10. It can be built from the solution file:

```
dotnet build AltinnTestTools.slnx
```

To run locally:

1. Install Azure Functions Core Tools v4 and the .NET 10 SDK.
2. Copy `TokenGenerator/local.settings.json.COPYME` to `TokenGenerator/local.settings.json`.
3. Start the function app from the project directory:

```
cd TokenGenerator
func start
```

When using an environment backed by Key Vault, local execution uses `DefaultAzureCredential`. For typical developer machines this means the signed-in Azure CLI or IDE identity must have Key Vault data-plane access to read both certificates and secrets, for example `Key Vault Certificate User` and `Key Vault Secrets User` on the relevant vault.

`local.settings.json` is intentionally used for local Azure Functions settings. The project currently also reads the file directly so the custom `Settings` section can be bound locally; in Azure, the same settings are provided by application settings.
