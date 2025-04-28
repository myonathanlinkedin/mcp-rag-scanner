using Identity.Application.Commands.Jwks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Web.Features
{
    public class JwksController : ApiController
    {

        [HttpGet]
        [Route(nameof(GetPublicKey))]
        public async Task<ActionResult<JsonWebKey>> GetPublicKey([FromQuery] GetPublicKeyCommand command) => await Send(command);
    }
}
