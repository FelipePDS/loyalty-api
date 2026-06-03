using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Interfaces.Repositories;
using Mapster;
using MediatR;

namespace LoyaltyApi.Application.Features.Customers.GetBalance;

public sealed record GetCustomerBalanceQuery(Guid CustomerId) : IQuery<CustomerBalanceDto>;

public sealed class GetCustomerBalanceQueryHandler(
    ICustomerRepository customerRepository,
    IPointTransactionRepository transactionRepository)
    : IRequestHandler<GetCustomerBalanceQuery, Result<CustomerBalanceDto>>
{
    public async Task<Result<CustomerBalanceDto>> Handle(
        GetCustomerBalanceQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            return Error.NotFound("Customer.NotFound", $"Customer {request.CustomerId} not found.");

        var pointsAboutToExpire = await transactionRepository.GetAboutToExpirePointsAsync(
            request.CustomerId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            cancellationToken);

        var dto = customer.Adapt<CustomerBalanceDto>() with
        {
            PointsAboutToExpire = pointsAboutToExpire
        };

        return dto;
    }
}
