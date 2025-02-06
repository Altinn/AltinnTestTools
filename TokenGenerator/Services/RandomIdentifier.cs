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
        var basePathMaybe = "C:/home/site/wwwroot/";
        var endusersFile = "Data/endusers.txt";
        var enterprisesFile = "Data/enterprises.txt";
        
        if (File.Exists(basePathMaybe + endusersFile))
        {
            randomPersonalIdentifiers = File.ReadAllLines(basePathMaybe + endusersFile);
        }
        else if (File.Exists(endusersFile))
        {
            randomPersonalIdentifiers = File.ReadAllLines(endusersFile);
        }
        else
        {
            logger.LogWarning($"Could not find file with random personal identifiers at path: {basePathMaybe + endusersFile}");
            randomPersonalIdentifiers = Array.Empty<string>();
        }
        
        if (File.Exists(basePathMaybe + enterprisesFile))
        {
            randomEnterpriseIdentifiers = File.ReadAllLines(basePathMaybe + enterprisesFile);
        }
        else if (File.Exists(enterprisesFile))
        {
            randomEnterpriseIdentifiers = File.ReadAllLines(enterprisesFile);
        }
        else
        {
            logger.LogWarning($"Could not find file with random enterpise identifiers at path: {basePathMaybe + enterprisesFile}");
            randomEnterpriseIdentifiers = Array.Empty<string>();
        }
    }
}