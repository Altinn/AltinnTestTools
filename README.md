# AltinnTestTools
Various tools used for testing Altinn

## Token Generator

Simple HTTP API for generating arbitrary enterprise and person access tokens for test environments, used for automated testing. Matches tokens produced by [Altinn3 token exchange](https://docs.altinn.studio/altinn-api/authentication/#exchange-of-jwt-token) or consent-token endpoints, using the key material for the supplied environment. Implemented as a Azure Function running at https://altinn-testtools-token-generator.azurewebsites.net.

### Access

The application requires authentication. See https://altinn.github.io/docs/api/rest/kom-i-gang/tokengenerator/ for more information.

### Usage:

`{environment}` is an Altinn test environment name eg. `at24` og `tt02`. 

`none` is also a valid value, which indicates that the token generator itself becomes the issuers and uses its own key material for signing. See "Issuer" and "Bulk mode" below.


#### Enterprise tokens (aka Maskinporten):
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetEnterpriseToken?env={environment}&scopes={scopes}&org={orgName}&orgNo={orgNo}`

#### Person tokens (aka ID-porten)
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetPersonalToken?env={environment}&scopes={scopes}&userId={userId}&partyId={partyId}&pid={pid}`

#### Systemuser tokens ([see docs](https://docs.digdir.no/docs/Maskinporten/maskinporten_func_systembruker))
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetSystemUserToken?env={environment}&scopes={scopes}&systemUserId={systemUserId}&systemUserOrg={systemUserOrg}&clientId={clientId}`

#### Legacy (Altinn 2) Enterprise user tokens (aka Maskinporten + Enterprise user authentication):
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetEnterpriseUserToken?env={environment}&orgNo={orgNo}&partyId={partyId}&userId={userId}&userName={userName}`

#### Optional parameters:

* `supplierOrgNo` (Enterprise tokens only)
* `ttl` (Default: 1800 seconds)
* `authLvl` (Personal tokens only. Default: `3`)
* `consumerOrgNo` (Personal tokens only. Default: `991825827`)
* `userName` (Personal tokens only. Default: *empty string*)
* `clientAmr` (Personal tokens only. Default: `virksomhetssertifikat`)
* `bulkCount` (Personal or enterpise tokens only.) Enables bulk mode. See below.
* `dump` (displays a human readable decoded JWT)

#### Consent tokens: 
`https://altinn-testtools-token-generator.azurewebsites.net/api/GetConsentToken?env={environment}&serviceCodes={serviceCodes}&offeredBy={offeredBy}&coveredBy={coveredBy}`

* `serviceCodes` is a delimited list of `{servicesCode}_{serviceEditionCode}.{servicesCode2}_{serviceEditionCode2}`, eg. `5120_1.5678_2`
* `offeredBy`, `coveredBy` and `handledBy` (optional) is a 9 or 11 long all-digit string. 
* `authorizationCode` is av valid GUID. If omitted a random one is used.
* `ttl` (optional) is the valid lifetime of the token in seconds. Default: 30 seconds.

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
