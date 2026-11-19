using CoC_API.Models;

namespace CoC_API.Services;

public sealed class CampaignStore
{
    private readonly object _gate = new();
    private readonly List<Campaign> _campaigns = [];

    public Campaign[] GetAll()
    {
        lock (_gate)
            return _campaigns.ToArray();
    }

    public Campaign? Get(Guid id)
    {
        lock (_gate)
            return _campaigns.FirstOrDefault(campaign => campaign.Id == id);
    }

    public void Add(Campaign campaign)
    {
        lock (_gate)
            _campaigns.Add(campaign);
    }

    public void Update(Guid id, Action<Campaign> update)
    {
        lock (_gate)
        {
            var campaign = _campaigns.FirstOrDefault(candidate => candidate.Id == id)
                ?? throw new KeyNotFoundException($"Campaign '{id}' was not found.");

            update(campaign);
        }
    }

    public Campaign[] ClaimDueInstagramCampaigns(DateTimeOffset now)
    {
        lock (_gate)
        {
            return _campaigns
                .Where(campaign => campaign.TryBeginInstagramPublication(now))
                .ToArray();
        }
    }

    public void CompleteInstagramPublication(Campaign campaign, string mediaId)
    {
        lock (_gate)
            campaign.CompleteInstagramPublication(mediaId);
    }

    public void FailInstagramPublication(Campaign campaign)
    {
        lock (_gate)
            campaign.FailInstagramPublication();
    }
}
