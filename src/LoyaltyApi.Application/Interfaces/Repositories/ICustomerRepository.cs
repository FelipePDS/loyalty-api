using LoyaltyApi.Domain.Entities;

namespace LoyaltyApi.Application.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Customer?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    void Update(Customer customer);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
