using Microsoft.IdentityModel.Tokens;

public interface IIdentity
{
    Task<Result<IUser>> Register(UserRequestModel userRequest);

    Task<Result<UserResponseModel>> Login(UserRequestModel userRequest);

    Task<Result> ChangePassword(ChangePasswordRequestModel changePasswordRequest);
    Result<JsonWebKey> GetPublicKey();
    Task<Result> ResetPassword(string email);
}