using System;
using System.Collections.Generic;
using System.Linq;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services;

public class RandomIdentifier : IRandomIdentifier
{
    private string[] randomPersonalIdentifiers;
    private string[] randomEnterpriseIdentifiers;
    
    public RandomIdentifier()
    {
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
        randomPersonalIdentifiers = System.IO.File.ReadAllLines("Data/endusers.txt");
        randomEnterpriseIdentifiers = System.IO.File.ReadAllLines("Data/enterprises.txt");
    }
}