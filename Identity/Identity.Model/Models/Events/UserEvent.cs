using Identity.Application.Handlers;

public class UserRegisteredEvent : IDomainEvent
{
    public string Email { get; }
    public string Password { get; }

    public UserRegisteredEvent(string email, string password)
    {
        Email = email;
        Password = password;
    }
}

public class PasswordResetEvent : IDomainEvent
{
    public string Email { get; }
    public string NewPassword { get; }

    public PasswordResetEvent(string email, string newPassword)
    {
        Email = email;
        NewPassword = newPassword;
    }
}

public class PasswordChangedEvent : IDomainEvent
{
    public string Email { get; }
    public string NewPassword { get; }

    public PasswordChangedEvent(string email, string newPassword)
    {
        Email = email;
        NewPassword = newPassword;
    }
}
