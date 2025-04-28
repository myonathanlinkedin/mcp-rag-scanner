using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

public class JwksController : ApiController
{

    [HttpGet]
    [Route(nameof(GetPublicKey))]
    public async Task<ActionResult<JsonWebKey>> GetPublicKey([FromQuery] GetPublicKeyCommand command) => await Send(command);
}
