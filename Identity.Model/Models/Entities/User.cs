using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Identity;

public class User : IdentityUser, IUser
{
    public User(string email)
        : base(email)
        => Email = email;
}