using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Interfaces;
using LoyaltyApi.Application.Interfaces.Repositories;
using Mapster;
using MediatR;

namespace LoyaltyApi.Application.Features.Customers.AdjustPoints;

public sealed record AdjustPointsCommand(
    Guid CustomerId,
    int Points,
    string Description) : ICommand<PointTransactionDto>;

public sealed class AdjustPointsCommandHandler(
    ICustomerRepository customerRepository,
    IPointTransactionRepository transactionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AdjustPointsCommand, Result<PointTransactionDto>>
{
    public async Task<Result<PointTransactionDto>> Handle(
        AdjustPointsCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            return Error.NotFound("Customer.NotFound", $"Customer {request.CustomerId} not found.");

        var transaction = customer.AdjustPoints(request.Points, request.Description);

        customerRepository.Update(customer);
        await transactionRepository.AddAsync(transaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction.Adapt<PointTransactionDto>();
    }
}
