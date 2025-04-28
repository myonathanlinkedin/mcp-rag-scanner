using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class RAGScannerController : ApiController
{
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route(nameof(ScanUrl))]
    public async Task<ActionResult> ScanUrl(
        ScanUrlCommand command)
        => await Send(command);
}
