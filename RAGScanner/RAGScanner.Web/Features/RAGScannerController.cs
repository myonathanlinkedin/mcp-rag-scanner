using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Threading.Tasks;

public class RAGScannerController : ApiController
{
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route(nameof(ScanUrl))]
    public async Task<ActionResult> ScanUrl(ScanUrlCommand command)
        => await Send(command);

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route(nameof(RAGSearch))]
    public async Task<ActionResult<List<RAGSearchResult>>> RAGSearch(RAGSearchCommand command)
        => await Send(command);
}
