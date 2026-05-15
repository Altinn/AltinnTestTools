using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services;

public class RandomIdentifier(ILogger<RandomIdentifier> logger) : IRandomIdentifier
{
    private readonly string[] randomPersonalIdentifiers = LoadData("Data/endusers.txt", "personal", logger);
    private readonly string[] randomEnterpriseIdentifiers = LoadData("Data/enterprises.txt", "enterpise", logger);

    public List<string> GetRandomPersonalIdentifiers(uint count)
    {
        return [.. Shuffle(randomPersonalIdentifiers).Take((int)count)];
    }

    public List<string> GetRandomEnterpriseIdentifiers(uint count)
    {
        return [.. Shuffle(randomEnterpriseIdentifiers).Take((int)count)];
    }

    private static string[] Shuffle(string[] array)
    {
        var random = new Random();
        for (var i = array.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]); // Swap
        }

        return array;
    }

    private static string[] LoadData(string file, string identifierType, ILogger<RandomIdentifier> logger)
    {
        var basePathMaybe = "C:/home/site/wwwroot/";

        if (File.Exists(basePathMaybe + file))
        {
            return File.ReadAllLines(basePathMaybe + file);
        }

        if (File.Exists(file))
        {
            return File.ReadAllLines(file);
        }

        logger.LogWarning($"Could not find file with random {identifierType} identifiers at path: {basePathMaybe + file}");
        return [];
    }
}
