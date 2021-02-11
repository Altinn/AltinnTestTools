using Microsoft.AspNetCore.Mvc;

namespace TokenGenerator.Services
{
    public interface IAuthorizationMethod
    {
        bool IsAuthorized(string authorizationString, out ActionResult authorizationFailureResult);
    }
}