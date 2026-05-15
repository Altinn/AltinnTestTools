using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TokenGenerator;
using TokenGenerator.Services;
using TokenGenerator.Services.Interfaces;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Configuration
    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// This is required for CertificatePfx implementation to work in both Azure and locally.
Environment.SetEnvironmentVariable("ApplicationRootPath", builder.Environment.ContentRootPath);

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.Configure<Settings>(builder.Configuration.GetSection("Settings"));
builder.Services.AddOptions();

builder.Services.AddSingleton<ICertificateService, CertificateKeyVault>();
builder.Services.AddSingleton<IToken, Token>();
builder.Services.AddSingleton<IIssuer, Issuer>();
builder.Services.AddSingleton<IRandomIdentifier, RandomIdentifier>();
builder.Services.AddScoped<IAuthorizationBearer, AuthorizationBearer>();
builder.Services.AddScoped<IAuthorizationBasic, AuthorizationBasic>();
builder.Services.AddScoped<IRequestValidator, RequestValidator>();
builder.Services.AddScoped<IAuthorization, Authorization>();

builder.Build().Run();
