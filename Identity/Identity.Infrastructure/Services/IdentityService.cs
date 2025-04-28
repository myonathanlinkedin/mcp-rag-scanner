using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Threading.Tasks;

internal class IdentityService : IIdentity
{
    private const string InvalidErrorMessage = "Invalid credentials.";

    private readonly UserManager<User> userManager;
    private readonly IJwtGenerator jwtGenerator;
    private readonly IEmailSender emailSenderService;

    public IdentityService(
        UserManager<User> userManager,
        IJwtGenerator jwtGenerator,
        IEmailSender emailSender)
    {
        this.userManager = userManager;
        this.jwtGenerator = jwtGenerator;
        this.emailSenderService = emailSender;
    }

    public async Task<Result<IUser>> Register(UserRequestModel userRequest)
    {
        var user = new User(userRequest.Email);

        var identityResult = await userManager.CreateAsync(user, userRequest.Password);

        var errors = identityResult.Errors.Select(e => e.Description);

        if (identityResult.Succeeded)
        {
            var subject = "🎉 Hooray! Your Account is Ready 🎉";
            var body = $"Congrats! 🎉 Your account has been created successfully. Now, the fun begins! 😎\n\n" +
                       $"Please log in using your email: '{userRequest.Email}' and password: '{userRequest.Password}'.\n\n" +
                       "Don’t forget to change your password once you're in... We won't judge! 😜";

            await emailSenderService.SendEmailAsync(userRequest.Email, subject, body);

            return Result<IUser>.SuccessWith(user);
        }

        return Result<IUser>.Failure(errors);
    }

    public async Task<Result<UserResponseModel>> Login(UserRequestModel userRequest)
    {
        var user = await userManager.FindByEmailAsync(userRequest.Email);
        if (user == null)
        {
            return InvalidErrorMessage;
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, userRequest.Password);

        if (!passwordValid)
        {
            return InvalidErrorMessage;
        }

        var token = await jwtGenerator.GenerateToken(user);

        return new UserResponseModel(token);
    }

    public async Task<Result> ChangePassword(ChangePasswordRequestModel changePasswordRequest)
    {
        var user = await userManager.FindByIdAsync(changePasswordRequest.UserId);

        if (user == null)
        {
            return InvalidErrorMessage;
        }

        var identityResult = await userManager.ChangePasswordAsync(
            user,
            changePasswordRequest.CurrentPassword,
            changePasswordRequest.NewPassword);

        var errors = identityResult.Errors.Select(e => e.Description);

        return identityResult.Succeeded
            ? Result.Success
            : Result.Failure(errors);
    }

    public async Task<Result> ResetPassword(string email)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return InvalidErrorMessage;
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

        var newPassword = PasswordGenerator.Generate(6); // <== Using PasswordGenerator class

        var identityResult = await userManager.ResetPasswordAsync(
            user,
            resetToken,
            newPassword);

        var errors = identityResult.Errors.Select(e => e.Description);

        if (identityResult.Succeeded)
        {
            var subject = "🔒 Your Password Has Been Reset";
            var body = $"Hello,\n\nYour password has been reset successfully. Here is your new password:\n\n" +
                       $"Password: {newPassword}\n\n" +
                       "Please log in and change it after logging in for better security.\n\nStay safe!";

            await emailSenderService.SendEmailAsync(user.Email, subject, body);

            return Result.Success;
        }

        return Result.Failure(errors);
    }

    public Result<JsonWebKey> GetPublicKey()
    {
        return Result<JsonWebKey>.SuccessWith(this.jwtGenerator.GetPublicKey());
    }
}
