using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace TokenGenerator.Services
{
    public interface IAuthorization
    {
        Task<ActionResult> Authorize();
    }
}