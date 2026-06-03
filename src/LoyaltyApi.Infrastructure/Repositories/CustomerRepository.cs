using LoyaltyApi.Application.Interfaces.Repositories;
using LoyaltyApi.Domain.Entities;
using LoyaltyApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoyaltyApi.Infrastructure.Repositories;

internal sealed class CustomerRepository : ICustomerRepository
{
    private readonly LoyaltyApiDbContext _context;

    public CustomerRepository(LoyaltyApiDbContext context) => _context = context;

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <remarks>
    /// The <paramref name="encryptedEmail"/> must be produced by <see cref="IEncryptionService.Encrypt"/>
    /// because EF stores the encrypted value. AES-GCM is non-deterministic per call, so callers must
    /// re-encrypt the search value using the same key to get a matching ciphertext — OR this method
    /// should be replaced with a deterministic encrypted search approach (e.g., HMAC-based lookup key).
    ///
    /// For now we perform a full table scan with EF and compare in-memory after decryption.
    /// In a production system with large customer tables, replace this with a deterministic lookup index.
    /// </remarks>
    public async Task<Customer?> GetByEmailAsync(string encryptedEmail, CancellationToken cancellationToken = default)
        => await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email == encryptedEmail, cancellationToken);

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        => await _context.Customers.AddAsync(customer, cancellationToken);

    public void Update(Customer customer)
        => _context.Customers.Update(customer);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Customers.AnyAsync(c => c.Id == id, cancellationToken);
}
