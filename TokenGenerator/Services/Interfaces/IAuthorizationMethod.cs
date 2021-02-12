using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace TokenGenerator.Services
{
    public interface IAuthorizationMethod
    {
        Task<ActionResult> IsAuthorized(string authorizationString);
    }
}