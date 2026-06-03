using LoyaltyApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace LoyaltyApi.Infrastructure.Services;

internal sealed class IdentityService(
    UserManager<IdentityUser> userManager) : IIdentityService
{
    public async Task<(bool Succeeded, string? UserId, string? Error)> CreateUserAsync(
        string email,
        string password,
        string role,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
            return (false, null, "A user with this email already exists.");

        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return (false, null, errors);
        }

        await userManager.AddToRoleAsync(user, role);
        return (true, user.Id, null);
    }

    public async Task<(bool Succeeded, IdentityUserInfo? UserInfo, string? Error)> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return (false, null, "Invalid email or password.");

        var validPassword = await userManager.CheckPasswordAsync(user, password);
        if (!validPassword)
            return (false, null, "Invalid email or password.");

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Customer";

        return (true, new IdentityUserInfo(user.Id, user.Email!, role), null);
    }

    public async Task<string?> GetRoleByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return null;

        var roles = await userManager.GetRolesAsync(user);
        return roles.FirstOrDefault();
    }
}
