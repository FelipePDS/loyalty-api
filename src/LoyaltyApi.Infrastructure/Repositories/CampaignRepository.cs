using LoyaltyApi.Application.Interfaces.Repositories;
using LoyaltyApi.Domain.Entities;
using LoyaltyApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoyaltyApi.Infrastructure.Repositories;

internal sealed class CampaignRepository : ICampaignRepository
{
    private readonly LoyaltyApiDbContext _context;

    public CampaignRepository(LoyaltyApiDbContext context) => _context = context;

    public async Task<IReadOnlyList<Campaign>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await _context.Campaigns
            .AsNoTracking()
            .Where(c => c.IsActive && c.StartDate <= DateTime.UtcNow && c.EndDate >= DateTime.UtcNow)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<Campaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Campaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(Campaign campaign, CancellationToken cancellationToken = default)
        => await _context.Campaigns.AddAsync(campaign, cancellationToken);

    public void Update(Campaign campaign)
        => _context.Campaigns.Update(campaign);
}
