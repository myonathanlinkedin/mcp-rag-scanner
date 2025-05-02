using Refit;

public interface IIdentityApi
{
    [Post("/api/Identity/Register/Register")]
    Task<HttpResponseMessage> RegisterAsync([Body] object payload);

    [Post("/api/Identity/Login/Login")]
    Task<string> LoginAsync([Body] object payload);

    [Put("/api/Identity/ChangePassword/ChangePassword")]
    Task<HttpResponseMessage> ChangePasswordAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/Identity/ResetPassword/ResetPassword")]
    Task<HttpResponseMessage> ResetPasswordAsync([Body] object payload);
}