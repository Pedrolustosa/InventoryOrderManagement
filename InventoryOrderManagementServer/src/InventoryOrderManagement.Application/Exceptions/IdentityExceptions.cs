namespace InventoryOrderManagement.Application.Exceptions;

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("The email or password is incorrect.")
    {
    }
}

public sealed class AccountLockedException : Exception
{
    public AccountLockedException()
        : base("The account is temporarily locked.")
    {
    }
}

public sealed class UserNotFoundException : Exception
{
    public UserNotFoundException()
        : base("User was not found.")
    {
    }
}

public sealed class RoleNotFoundException : Exception
{
    public RoleNotFoundException()
        : base("Role was not found.")
    {
    }
}

public sealed class IdentityConflictException : Exception
{
    public IdentityConflictException(string message)
        : base(message)
    {
    }
}

public sealed class IdentityValidationException : Exception
{
    public IdentityValidationException(string message)
        : base(message)
    {
    }
}

public sealed class IdentityForbiddenException : Exception
{
    public IdentityForbiddenException(string message)
        : base(message)
    {
    }
}
