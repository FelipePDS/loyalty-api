using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Interfaces;
using LoyaltyApi.Application.Interfaces.Repositories;
using Mapster;
using MediatR;

namespace LoyaltyApi.Application.Features.Points.Reverse;

public sealed record ReversePointsCommand(
    Guid TransactionId,
    string Reason) : ICommand<PointTransactionDto>;

public sealed class ReversePointsCommandHandler(
    ICustomerRepository customerRepository,
    IPointTransactionRepository transactionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReversePointsCommand, Result<PointTransactionDto>>
{
    public async Task<Result<PointTransactionDto>> Handle(
        ReversePointsCommand request,
        CancellationToken cancellationToken)
    {
        var originalTransaction = await transactionRepository.GetByIdAsync(request.TransactionId, cancellationToken);
        if (originalTransaction is null)
            return Error.NotFound("Transaction.NotFound", $"Transaction {request.TransactionId} not found.");

        if (!originalTransaction.CanBeReversed)
            return Error.Validation("Transaction.CannotReverse",
                originalTransaction.IsReversed
                    ? "This transaction has already been reversed."
                    : "This transaction type does not support reversal.");

        var customer = await customerRepository.GetByIdAsync(originalTransaction.CustomerId, cancellationToken);
        if (customer is null)
            return Error.NotFound("Customer.NotFound", $"Customer {originalTransaction.CustomerId} not found.");

        try
        {
            var reversalTransaction = customer.ReverseTransaction(originalTransaction, request.Reason);

            customerRepository.Update(customer);
            transactionRepository.Update(originalTransaction);
            await transactionRepository.AddAsync(reversalTransaction, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return reversalTransaction.Adapt<PointTransactionDto>();
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            return Error.Validation("Transaction.ReversalFailed", ex.Message);
        }
    }
}
