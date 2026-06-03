using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Interfaces;
using LoyaltyApi.Application.Interfaces.Repositories;
using Mapster;
using MediatR;

namespace LoyaltyApi.Application.Features.Points.Redeem;

public sealed record RedeemPointsCommand(
    Guid CustomerId,
    int Points,
    string Description) : ICommand<PointTransactionDto>;

public sealed class RedeemPointsCommandHandler(
    ICustomerRepository customerRepository,
    IPointTransactionRepository transactionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RedeemPointsCommand, Result<PointTransactionDto>>
{
    public async Task<Result<PointTransactionDto>> Handle(
        RedeemPointsCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            return Error.NotFound("Customer.NotFound", $"Customer {request.CustomerId} not found.");

        try
        {
            var transaction = customer.RedeemPoints(request.Points, request.Description);

            customerRepository.Update(customer);
            await transactionRepository.AddAsync(transaction, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return transaction.Adapt<PointTransactionDto>();
        }
        catch (Domain.Exceptions.InsufficientPointsException ex)
        {
            return Error.Validation("Points.InsufficientBalance",
                $"Insufficient points. Current balance: {ex.CurrentBalance}, requested: {ex.RequestedPoints}.");
        }
    }
}
