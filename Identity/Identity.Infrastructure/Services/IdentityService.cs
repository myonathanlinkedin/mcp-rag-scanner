using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

public class IdentityService : IIdentity
{
    private const string InvalidErrorMessage = "Invalid credentials.";
    private readonly UserManager<User> userManager;
    private readonly IJwtGenerator jwtGenerator;
    private readonly IEventDispatcher eventDispatcher;
    private readonly ILogger<IdentityService> logger;

    public IdentityService(
        UserManager<User> userManager,
        IJwtGenerator jwtGenerator,
        IEventDispatcher eventDispatcher,
        ILogger<IdentityService> logger)
    {
        this.userManager = userManager;
        this.jwtGenerator = jwtGenerator;
        this.eventDispatcher = eventDispatcher;
        this.logger = logger;
    }

    public async Task<Result<IUser>> Register(UserRequestModel userRequest)
    {
        var user = new User(userRequest.Email);
        var identityResult = await userManager.CreateAsync(user, userRequest.Password);
        var errors = identityResult.Errors.Select(e => e.Description);

        if (identityResult.Succeeded)
        {
            logger.LogInformation("User registered successfully with email: {Email}", userRequest.Email);

            // Dispatch UserRegisteredEvent (if necessary)
            await eventDispatcher.Dispatch(new UserRegisteredEvent(userRequest.Email, userRequest.Password));
            return Result<IUser>.SuccessWith(user);
        }

        // Log registration failure
        logger.LogWarning("User registration failed for email: {Email}. Errors: {Errors}", userRequest.Email, string.Join(", ", errors));
        return Result<IUser>.Failure(errors);
    }

    public async Task<Result<UserResponseModel>> Login(UserRequestModel userRequest)
    {
        var user = await userManager.FindByEmailAsync(userRequest.Email);
        if (user == null)
        {
            logger.LogWarning("Login failed. User not found for email: {Email}", userRequest.Email);
            return Result<UserResponseModel>.Failure(new[] { InvalidErrorMessage });
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, userRequest.Password);
        if (!passwordValid)
        {
            logger.LogWarning("Login failed. Invalid password for email: {Email}", userRequest.Email);
            return Result<UserResponseModel>.Failure(new[] { InvalidErrorMessage });
        }

        var token = await jwtGenerator.GenerateToken(user);
        logger.LogInformation("User logged in successfully with email: {Email}", userRequest.Email);
        return Result<UserResponseModel>.SuccessWith(new UserResponseModel(token));
    }

    public async Task<Result> ChangePassword(ChangePasswordRequestModel changePasswordRequest)
    {
        try
        {
            var user = await userManager.FindByIdAsync(changePasswordRequest.UserId);
            if (user == null)
            {
                logger.LogWarning("Change password failed. User not found for UserId: {UserId}", changePasswordRequest.UserId);
                return Result.Failure(new[] { InvalidErrorMessage });
            }

            var identityResult = await userManager.ChangePasswordAsync(
                user,
                changePasswordRequest.CurrentPassword,
                changePasswordRequest.NewPassword);

            var errors = identityResult.Errors.Select(e => e.Description);

            if (identityResult.Succeeded)
            {
                logger.LogInformation("Password changed successfully for email: {Email}", user.Email);

                await eventDispatcher.Dispatch(new PasswordChangedEvent(user.Email, changePasswordRequest.NewPassword));
                return Result.Success;
            }

            logger.LogWarning("Password change failed for email: {Email}. Errors: {Errors}", user.Email, string.Join(", ", errors));
            return Result.Failure(errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while changing the password for UserId: {UserId}", changePasswordRequest.UserId);
            return Result.Failure(new[] { "An unexpected error occurred. Please try again later." });
        }
    }


    public async Task<Result> ResetPassword(string email)
    {
        try
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                logger.LogWarning("Password reset failed. User not found for email: {Email}", email);
                return Result.Failure(new[] { InvalidErrorMessage });
            }

            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var newPassword = PasswordGenerator.Generate(6);
            var identityResult = await userManager.ResetPasswordAsync(user, resetToken, newPassword);
            var errors = identityResult.Errors.Select(e => e.Description);

            if (identityResult.Succeeded)
            {
                logger.LogInformation("Password reset successfully for email: {Email}", email);

                // Dispatch PasswordResetEvent (if necessary)
                await eventDispatcher.Dispatch(new PasswordResetEvent(user.Email, newPassword));
                return Result.Success;
            }

            logger.LogWarning("Password reset failed for email: {Email}. Errors: {Errors}", email, string.Join(", ", errors));
            return Result.Failure(errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while resetting the password for email: {Email}", email);
            return Result.Failure(new[] { "An unexpected error occurred. Please try again later." });
        }
    }

    public Result<JsonWebKey> GetPublicKey()
    {
        return Result<JsonWebKey>.SuccessWith(jwtGenerator.GetPublicKey());
    }
}
