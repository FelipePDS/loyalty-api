using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.Interfaces.Services;
using LoyaltyApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace LoyaltyApi.IntegrationTests;

public sealed class LoyaltyApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    public async Task InitializeAsync()
    {
        // Seed Identity roles
        using var scope = Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Customer", "Partner", "Admin" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove all EF Core SQL Server related services and DbContext
            var sqlServerDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("SqlServer") == true
                    || d.ImplementationType?.FullName?.Contains("SqlServer") == true
                    || d.ServiceType == typeof(DbContextOptions<LoyaltyApiDbContext>)
                    || d.ServiceType == typeof(DbContextOptions))
                .ToList();
            foreach (var d in sqlServerDescriptors)
                services.Remove(d);

            services.RemoveAll(typeof(IEncryptionService));

            // Add a no-op encryption service for testing
            services.AddSingleton<IEncryptionService, FakeEncryptionService>();

            services.AddDbContext<LoyaltyApiDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_dbName)
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
        });
    }

    /// <summary>
    /// Creates an HTTP client with a valid JWT for the specified role.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string role, Guid? customerId = null)
    {
        var client = CreateClient();
        var token = GenerateTestJwt(role, customerId ?? Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string GenerateTestJwt(string role, Guid customerId)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("CHANGE_ME_USE_A_SECURE_256_BIT_KEY_IN_PRODUCTION_1234567890"));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(AppClaimTypes.CustomerId, customerId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: "LoyaltyApi",
            audience: "LoyaltyApiClients",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// Fake encryption service that returns plaintext (no actual encryption) for testing.
/// </summary>
internal sealed class FakeEncryptionService : IEncryptionService
{
    public string Encrypt(string plaintext) => plaintext;
    public string Decrypt(string ciphertext) => ciphertext;
}
