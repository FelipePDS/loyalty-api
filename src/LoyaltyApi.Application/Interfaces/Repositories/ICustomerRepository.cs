using LoyaltyApi.Domain.Entities;

namespace LoyaltyApi.Application.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Customer?> GetByEmailAsync(string encryptedEmail, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    void Update(Customer customer);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
