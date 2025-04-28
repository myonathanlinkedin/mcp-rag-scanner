using Identity.Application.Commands.MCP;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.Features
{
    public class PromptController : ApiController
    {

        [HttpPost]
        [Route(nameof(SendUserPrompt))]
        public async Task<ActionResult<string>> SendUserPrompt([FromBody] UserPromptCommand command) => await Send(command);
    }
}
