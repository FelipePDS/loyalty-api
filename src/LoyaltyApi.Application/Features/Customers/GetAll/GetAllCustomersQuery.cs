using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Interfaces.Repositories;
using Mapster;
using MediatR;

namespace LoyaltyApi.Application.Features.Customers.GetAll;

public sealed record GetAllCustomersQuery(int Page, int PageSize) : IQuery<PagedResult<CustomerSummaryDto>>;

public sealed class GetAllCustomersQueryHandler(
    ICustomerRepository customerRepository)
    : IRequestHandler<GetAllCustomersQuery, Result<PagedResult<CustomerSummaryDto>>>
{
    public async Task<Result<PagedResult<CustomerSummaryDto>>> Handle(
        GetAllCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await customerRepository.GetAllAsync(
            request.Page, request.PageSize, cancellationToken);

        var dtos = items.Adapt<IReadOnlyList<CustomerSummaryDto>>();

        return new PagedResult<CustomerSummaryDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
