using System.Collections.Generic;

namespace TokenGenerator.Services.Interfaces;

public interface IRandomIdentifier
{
    public List<string> GetRandomPersonalIdentifiers(uint count);
    public List<string> GetRandomEnterpriseIdentifiers(uint count);
}