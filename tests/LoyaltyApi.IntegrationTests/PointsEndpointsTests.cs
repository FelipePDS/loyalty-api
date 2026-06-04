using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Features.Auth.Register;
using LoyaltyApi.Application.Features.Points.Earn;
using LoyaltyApi.Domain.Entities;
using LoyaltyApi.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace LoyaltyApi.IntegrationTests;

public sealed class PointsEndpointsTests : IClassFixture<LoyaltyApiFactory>
{
    private readonly LoyaltyApiFactory _factory;

    public PointsEndpointsTests(LoyaltyApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Earn_AsPartner_Returns201()
    {
        // Arrange — create a customer in the database
        Guid customerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LoyaltyApiDbContext>();
            var customer = Customer.Create(
                "Points Test User",
                Domain.ValueObjects.Email.Create("points-test@example.com"),
                Domain.ValueObjects.Document.Create("52998224725"),
                "partner-identity-123");
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            customerId = customer.Id;
        }

        var client = _factory.CreateAuthenticatedClient("Partner", customerId);
        var command = new EarnPointsCommand(customerId, 200, "Integration test earn", null, null);

        // Act
        var response = await client.PostAsJsonAsync("/api/points/earn", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var dto = await response.Content.ReadFromJsonAsync<PointTransactionDto>();
        dto.Should().NotBeNull();
        dto!.Points.Should().Be(200);
        dto.Type.Should().Be(Domain.Enums.TransactionType.Earned);
    }

    [Fact]
    public async Task Earn_AsCustomerRole_Returns403()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient("Customer");
        var command = new EarnPointsCommand(Guid.NewGuid(), 100, "Should fail", null, null);

        // Act
        var response = await client.PostAsJsonAsync("/api/points/earn", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Earn_Unauthenticated_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new EarnPointsCommand(Guid.NewGuid(), 100, "Should fail", null, null);

        // Act
        var response = await client.PostAsJsonAsync("/api/points/earn", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Redeem_AuthenticatedCustomer_ReturnsOk()
    {
        // Arrange — register and earn points first
        var anonymousClient = _factory.CreateClient();
        var email = $"redeem-{Guid.NewGuid():N}@test.com";

        var registerResponse = await anonymousClient.PostAsJsonAsync("/api/auth/register",
            new RegisterCustomerCommand("Redeem User", email, "StrongPass123!", "52998224725"));
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var customerId = authResponse!.CustomerId;

        // Earn points as partner
        var partnerClient = _factory.CreateAuthenticatedClient("Partner", customerId);
        await partnerClient.PostAsJsonAsync("/api/points/earn",
            new EarnPointsCommand(customerId, 500, "Earn for redeem test", null, null));

        // Redeem as customer
        var customerClient = _factory.CreateAuthenticatedClient("Customer", customerId);
        var redeemPayload = new { Points = 100, Description = "Test redemption" };

        // Act
        var response = await customerClient.PostAsJsonAsync("/api/points/redeem", redeemPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
