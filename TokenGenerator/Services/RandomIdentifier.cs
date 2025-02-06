using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services;

public class RandomIdentifier : IRandomIdentifier
{
    private readonly ILogger<RandomIdentifier> logger;
    private string[] randomPersonalIdentifiers;
    private string[] randomEnterpriseIdentifiers;
    
    public RandomIdentifier(ILogger<RandomIdentifier> logger)
    {
        this.logger = logger;
        LoadData();
    }
    
    public List<string> GetRandomPersonalIdentifiers(uint count)
    {
        return Shuffle(randomPersonalIdentifiers).Take((int)count).ToList();
    }

    public List<string> GetRandomEnterpriseIdentifiers(uint count)
    {
        return Shuffle(randomEnterpriseIdentifiers).Take((int)count).ToList();
    }

    private static string[] Shuffle(string[] array)
    {
        Random random = new();
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]); // Swap
        }

        return array;
    }

    private void LoadData()
    {
        string basePath = Environment.GetEnvironmentVariable("WEBROOT_PATH") ?? "";
        string endusersPath = Path.Combine(basePath, "Data", "endusers.txt");
        string enterprisesPath = Path.Combine(basePath, "Data", "enterprises.txt");

        if (File.Exists(endusersPath))
        {
            randomPersonalIdentifiers = File.ReadAllLines(endusersPath);
        }
        else
        {
            logger.LogWarning($"Could not find file with random personal identifiers at path: {endusersPath}");
            randomPersonalIdentifiers = Array.Empty<string>();
        }
        
        if (File.Exists(enterprisesPath))
        {
            randomEnterpriseIdentifiers = File.ReadAllLines(enterprisesPath);
        }
        else
        {
            logger.LogWarning($"Could not find file with random enterprise identifiers at path: {enterprisesPath}");
            randomEnterpriseIdentifiers = Array.Empty<string>();
        }
    }
}