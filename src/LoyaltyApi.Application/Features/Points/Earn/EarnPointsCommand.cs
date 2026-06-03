using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Interfaces;
using LoyaltyApi.Application.Interfaces.Repositories;
using Mapster;
using MediatR;

namespace LoyaltyApi.Application.Features.Points.Earn;

public sealed record EarnPointsCommand(
    Guid CustomerId,
    int Points,
    string Description,
    string? ReferenceId,
    DateTime? ExpiresAt) : ICommand<PointTransactionDto>;

public sealed class EarnPointsCommandHandler(
    ICustomerRepository customerRepository,
    IPointTransactionRepository transactionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<EarnPointsCommand, Result<PointTransactionDto>>
{
    public async Task<Result<PointTransactionDto>> Handle(
        EarnPointsCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            return Error.NotFound("Customer.NotFound", $"Customer {request.CustomerId} not found.");

        var transaction = customer.EarnPoints(
            request.Points,
            request.Description,
            request.ReferenceId,
            request.ExpiresAt);

        customerRepository.Update(customer);
        await transactionRepository.AddAsync(transaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction.Adapt<PointTransactionDto>();
    }
}
