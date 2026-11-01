using InventoryOrderManagement.Application.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace InventoryOrderManagement.Infrastructure.Identity;

internal static class IdentityResultGuard
{
    public static void EnsureSucceeded(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        if (result.Errors.Any(error => error.Code is "DuplicateUserName" or "DuplicateEmail"))
        {
            throw new IdentityConflictException("A user with this email already exists.");
        }

        if (result.Errors.Any(error => error.Code == "DuplicateRoleName"))
        {
            throw new IdentityConflictException("A role with this name already exists.");
        }

        throw new IdentityValidationException(
            string.Join(" ", result.Errors.Select(error => error.Description)));
    }
}
