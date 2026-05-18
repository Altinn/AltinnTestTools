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
    private readonly string[] randomEnterpriseIdentifiers = LoadData("Data/enterprises.txt", "enterprise", logger);

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
        var shuffled = array.ToArray();
        var random = new Random();
        for (var i = shuffled.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]); // Swap
        }

        return shuffled;
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
