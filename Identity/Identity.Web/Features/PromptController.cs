using Microsoft.AspNetCore.Mvc;

public class PromptController : ApiController
{

    [HttpPost]
    [Route(nameof(SendUserPrompt))]
    public async Task<ActionResult<string>> SendUserPrompt([FromBody] UserPromptCommand command) => await Send(command);
}

