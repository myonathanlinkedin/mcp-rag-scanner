using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class IdentityController : ApiController
{
    [HttpPost]
    [Route(nameof(Register))]
    public async Task<ActionResult> Register(
        RegisterUserCommand command)
        => await Send(command);

    [HttpPost]
    [Route(nameof(Login))]
    public async Task<ActionResult<UserResponseModel>> Login(
        LoginUserCommand command)
        => await Send(command);

    [HttpPut]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route(nameof(ChangePassword))]
    public async Task<ActionResult> ChangePassword(
        ChangePasswordCommand command)
        => await Send(command);

    [HttpPost]
    [Route(nameof(ResetPassword))]
    public async Task<ActionResult> ResetPassword(
        ResetPasswordCommand command)
        => await Send(command);
}
