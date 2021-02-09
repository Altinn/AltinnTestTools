using System;
using System.IO;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            
            builder.Services.Configure<Settings>(config.GetSection("ConfigurationItems"));

            builder.Services.AddOptions();
        }
    }
}