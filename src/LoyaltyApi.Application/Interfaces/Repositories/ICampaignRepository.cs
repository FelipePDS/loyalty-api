using LoyaltyApi.Domain.Entities;

namespace LoyaltyApi.Application.Interfaces.Repositories;

public interface ICampaignRepository
{
    Task<IReadOnlyList<Campaign>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Campaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Campaign campaign, CancellationToken cancellationToken = default);
    void Update(Campaign campaign);
}
