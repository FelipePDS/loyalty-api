using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Interfaces.Repositories;
using Mapster;
using MediatR;

namespace LoyaltyApi.Application.Features.Customers.GetTransactions;

public sealed record GetTransactionHistoryQuery(
    Guid CustomerId,
    int Page,
    int PageSize) : IQuery<PagedResult<PointTransactionDto>>;

public sealed class GetTransactionHistoryQueryHandler(
    ICustomerRepository customerRepository,
    IPointTransactionRepository transactionRepository)
    : IRequestHandler<GetTransactionHistoryQuery, Result<PagedResult<PointTransactionDto>>>
{
    public async Task<Result<PagedResult<PointTransactionDto>>> Handle(
        GetTransactionHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (!await customerRepository.ExistsAsync(request.CustomerId, cancellationToken))
            return Error.NotFound("Customer.NotFound", $"Customer {request.CustomerId} not found.");

        var (items, totalCount) = await transactionRepository.GetByCustomerIdAsync(
            request.CustomerId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items.Adapt<IReadOnlyList<PointTransactionDto>>();

        return new PagedResult<PointTransactionDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
