using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TokenGenerator.Services.Interfaces
{
    public interface IAuthorizationMethod
    {
        Task<ActionResult> IsAuthorized(string authorizationString);
    }
}