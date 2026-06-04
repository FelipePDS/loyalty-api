using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Features.Auth.Register;

namespace LoyaltyApi.IntegrationTests;

public sealed class AuthEndpointsTests : IClassFixture<LoyaltyApiFactory>
{
    private readonly HttpClient _client;
    private readonly LoyaltyApiFactory _factory;

    public AuthEndpointsTests(LoyaltyApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidData_Returns201WithTokens()
    {
        // Arrange
        var command = new RegisterCustomerCommand(
            "Integration Test User",
            $"integration-{Guid.NewGuid():N}@test.com",
            "StrongPass123!",
            "52998224725");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
        authResponse.Email.Should().Contain("@test.com");
        authResponse.Role.Should().Be("Customer");
        authResponse.CustomerId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        // Arrange — first register a user
        var email = $"login-test-{Guid.NewGuid():N}@test.com";
        var password = "StrongPass123!";

        var registerCommand = new RegisterCustomerCommand(
            "Login Test User", email, password, "52998224725");
        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        var loginPayload = new { Email = email, Password = password };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.Email.Should().Be(email.ToLowerInvariant());
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange — first register a user
        var email = $"wrong-pass-{Guid.NewGuid():N}@test.com";

        var registerCommand = new RegisterCustomerCommand(
            "Wrong Pass User", email, "StrongPass123!", "52998224725");
        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        var loginPayload = new { Email = email, Password = "WrongPassword!" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        // Arrange
        var loginPayload = new { Email = "nonexistent@test.com", Password = "Whatever123!" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
