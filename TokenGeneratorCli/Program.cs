using System;
using System.Net.Http;
using System.Text;
using System.Web;
using Microsoft.Extensions.Configuration;
using Mono.Options;

namespace TokenGeneratorCli
{
    class Program
    {
        private static Settings settings;

        static void Main(string[] args)
        {
            var showHelp = false;
            settings = GetSettings();

            var p = new OptionSet() {
                { "e=|environment=", "Environment to use",
                    v => settings.Environment = v },
                { "s=|scopes=", "List of scopes (comma-separated)",
                    v => settings.Scopes = v },
                { "t=|ttl=", "Token TTL (default: 1800 seconds)",
                    v => settings.Ttl = v },
                { "o=|org=", "(Enterprise only) Name of org",
                    v => settings.Org = v },
                { "n=|org-no=", "(Enterprise only) Organization number to use",
                    v => settings.OrgNo = v },
                { "S=|supplier-org-no=", "(Enterprise only) Organization number to use for supplier (optional)",
                    v =>  settings.SupplierOrgNo = v },
                { "u=|user-id=",  "(Personal only) UserID for user",
                    v => settings.UserId = v },
                { "p=|party-id=",  "(Personal only) PartyID for user",
                    v => settings.PartyId = v },
                { "P=|pid=",  "(Personal only) PID / SSN for user",
                    v => settings.Pid = v },
                { "a=|auth-lvl=",  "(Personal only) Auth lvl for token (default: 3)",
                    v => settings.AuthLvl = v },
                { "c|consumer-org=", "(Personal only) Consumer organzation number (default: 991825827 (Digdir))",
                    v => settings.ConsumerOrgNo = v },
                { "U|username",  "(Personal only) Username (optional)",
                    v => settings.UserName = v },
                { "C|client-amr=",  "(Personal only) Override client_amr claim (default: virksomhetssertifikat)",
                    v => settings.ClientAmr = v },
                { "authorization-username=",  "Username for authorizing to the API",
                    v => settings.AuthorzationUserName = v },
                { "authorization-password=",  "Password for authorizing to the API",
                    v => settings.AuthorizationPassword = v },
                { "endpoint-baseurl=",  "Base URL to API-endpoint",
                    v => settings.BaseUrl = v },

                { "h|help",  "Show this message and exit",
                    v => showHelp = v != null },
            };

            try
            {
                p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("TokenGeneratorCli: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `TokenGeneratorCli.exe --help' for more information.");
                return;
            }

            if (showHelp)
            {
                ShowHelp(p);
                return;
            }

            CheckParameters(p);

            Environment.Exit(MakeRequest()); 

        }

        static int MakeRequest()
        {
            var urlbuilder = new UriBuilder(settings.BaseUrl + (string.IsNullOrEmpty(settings.Pid) ? "GetEnterpriseToken" : "GetPersonalToken"));
            var query = HttpUtility.ParseQueryString(urlbuilder.Query);

            if (!string.IsNullOrEmpty(settings.Environment)) query["env"] = settings.Environment;
            if (!string.IsNullOrEmpty(settings.Scopes)) query["scopes"] = settings.Scopes;
            if (!string.IsNullOrEmpty(settings.Ttl)) query["ttl"] = settings.Ttl;
            if (!string.IsNullOrEmpty(settings.Org)) query["org"] = settings.Org;
            if (!string.IsNullOrEmpty(settings.OrgNo)) query["orgNo"] = settings.OrgNo;
            if (!string.IsNullOrEmpty(settings.SupplierOrgNo)) query["supplierOrgNo"] = settings.SupplierOrgNo;
            if (!string.IsNullOrEmpty(settings.UserId)) query["userId"] = settings.UserId;
            if (!string.IsNullOrEmpty(settings.PartyId)) query["partyId"] = settings.PartyId;
            if (!string.IsNullOrEmpty(settings.Pid)) query["pid"] = settings.Pid;
            if (!string.IsNullOrEmpty(settings.AuthLvl)) query["authLvl"] = settings.AuthLvl;
            if (!string.IsNullOrEmpty(settings.ConsumerOrgNo)) query["consumerOrgNo"] = settings.ConsumerOrgNo;
            if (!string.IsNullOrEmpty(settings.UserName)) query["userName"] = settings.UserName;
            if (!string.IsNullOrEmpty(settings.ClientAmr)) query["clientAmr"] = settings.ClientAmr;

            urlbuilder.Query = query.ToString();

            // Console.WriteLine(urlbuilder.ToString());

            var request = new HttpRequestMessage(HttpMethod.Get, urlbuilder.ToString());
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(settings.AuthorzationUserName + ":" + settings.AuthorizationPassword)));

            using var client = new HttpClient();
            var result = client.SendAsync(request).GetAwaiter().GetResult();

            var resultstring = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("## Unauthorized. Make sure appsettings.json has a correct AuthorizationUserName and AuthorizationPassword set");
                return 1;
            }

            if (result.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                Console.WriteLine("## Internal server error. Something broke, but it's probably not your fault.");
                return 1;
            }

            if (result.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                Console.WriteLine("## Invalid input:");
                Console.WriteLine(resultstring);
                return 1;
            }

            Console.WriteLine(resultstring);
            return 0;
        }

        static Settings GetSettings()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            return builder.GetSection(nameof(Settings)).Get<Settings>();
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: TokenGeneratorCli.exe [OPTIONS]");
            Console.WriteLine("Generates a custom Altinn token for use against ");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        static void CheckParameters(OptionSet p)
        {
            var hasErrors = false;

            if (string.IsNullOrEmpty(settings.Pid) && string.IsNullOrEmpty(settings.OrgNo) || !string.IsNullOrEmpty(settings.Pid) && !string.IsNullOrEmpty(settings.OrgNo))
            {
                Console.WriteLine("Requires one of --pid or --org-no (not both)");
                hasErrors = true;
            }
            else if (!string.IsNullOrEmpty(settings.OrgNo))
            {
                if (settings.Org == null)
                {
                    Console.WriteLine("Requires --org");
                    hasErrors = true;
                }

                if (!string.IsNullOrEmpty(settings.UserId) || !string.IsNullOrEmpty(settings.PartyId) || !string.IsNullOrEmpty(settings.AuthLvl) || !string.IsNullOrEmpty(settings.ConsumerOrgNo) || !string.IsNullOrEmpty(settings.ClientAmr))
                {
                    Console.WriteLine("Invalid options for --org-no: --user-id, --party-id, --auth-lvl, --consumer-org, --client-amr");
                    hasErrors = true;
                }
            }
            else if (!string.IsNullOrEmpty(settings.Pid))
            {
                if (string.IsNullOrEmpty(settings.PartyId))
                {
                    Console.WriteLine("Requires --party-id");
                    hasErrors = true;
                }

                if (string.IsNullOrEmpty(settings.UserId))
                {
                    Console.WriteLine("Requires --user-id");
                    hasErrors = true;
                }

                if (!string.IsNullOrEmpty(settings.Org))
                {
                    Console.WriteLine("Invalid options for --pid: --org");
                    hasErrors = true;
                }
            }

            if (string.IsNullOrEmpty(settings.Environment))
            {
                Console.WriteLine("Requires --env");
                hasErrors = true;
            }

            if (string.IsNullOrEmpty(settings.Scopes))
            {
                Console.WriteLine("Requires --scopes");
                hasErrors = true;
            }
           
            if (!hasErrors) return;
            Console.WriteLine("----");
            Console.WriteLine("Try --help for options");

            Environment.Exit(1);
        }
    }
}
