using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Interfaces.Repositories;
using Mapster;
using MediatR;

namespace LoyaltyApi.Application.Features.Customers.GetProfile;

public sealed record GetCustomerProfileQuery(Guid CustomerId) : IQuery<CustomerProfileDto>;

public sealed class GetCustomerProfileQueryHandler(
    ICustomerRepository customerRepository)
    : IRequestHandler<GetCustomerProfileQuery, Result<CustomerProfileDto>>
{
    public async Task<Result<CustomerProfileDto>> Handle(
        GetCustomerProfileQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            return Error.NotFound("Customer.NotFound", $"Customer {request.CustomerId} not found.");

        return customer.Adapt<CustomerProfileDto>();
    }
}
