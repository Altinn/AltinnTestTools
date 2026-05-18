using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TokenGenerator.Services.Interfaces;

public interface IAuthorization
{
    Task<ActionResult> Authorize(string requiredScope, HttpRequest req);
}
