using System;
using System.IO;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TokenGenerator.Services;

[assembly: FunctionsStartup(typeof(TokenGenerator.Startup))]

namespace TokenGenerator
{
    internal class Startup : FunctionsStartup
    {
        public Startup()
        {
        }
 
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(builder.GetContext().ApplicationRootPath, "local.settings.json"), optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            
            // This is required for CertificatePfx implementation to work in both Azure and locally
            Environment.SetEnvironmentVariable("ApplicationRootPath", builder.GetContext().ApplicationRootPath);

            builder.Services.Configure<Settings>(config.GetSection("Settings"));
            builder.Services.AddOptions();

            //builder.Services.AddSingleton<ICertificateService, CertificatePfx>();
            builder.Services.AddSingleton<ICertificateService, CertificateKeyVault>();
            builder.Services.AddSingleton<IToken, Token>();
            builder.Services.AddSingleton<IAuthorizationBearer, AuthorizationBearer>();
            builder.Services.AddSingleton<IAuthorizationBasic, AuthorizationBasic>();

            builder.Services.AddScoped<IRequestValidator, RequestValidator>();
            builder.Services.AddScoped<IAuthorization, Authorization>();
        }
    }
}