using Microsoft.AspNetCore.Mvc;

namespace TokenGenerator.Services
{
    public interface IAuthorization
    {
        bool Authorize(out ActionResult result);
    }
}