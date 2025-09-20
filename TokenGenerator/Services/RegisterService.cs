using Altinn.Register.Models;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services;

public class RegisterService : IRegisterService
{
    private readonly HttpClient httpClient;

    public RegisterService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<(bool Success, uint UserId, uint PartyId, Guid PartyUuid)> GetEnvironmentIdentifiers(string env, string pid, string platformAccessToken, string subscriptionKey, CancellationToken cancellationToken)
    {
        string requestUri = $"https://platform.{env}.altinn.cloud/register/api/v1/access-management/parties/query";
        if (env.Equals("tt02", StringComparison.OrdinalIgnoreCase))
        {
            requestUri = $"https://platform.tt02.altinn.no/register/api/v1/access-management/parties/query";
        }

        ListObject<string> body = ListObject.Create(new[] { pid });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("PlatformAccessToken", platformAccessToken);
        request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            ListObject<Party> result = await response.Content.ReadFromJsonAsync<ListObject<Party>>(cancellationToken: cancellationToken);
            if (result == null || !result.Items.Any())
            {
                return (false, 0, 0, Guid.Empty);
            }

            var party = result.Items.First();
            return (true, party.UserId, party.PartyId, party.Uuid);
        }
        
        return (false, 0, 0, Guid.Empty);
    }
}
