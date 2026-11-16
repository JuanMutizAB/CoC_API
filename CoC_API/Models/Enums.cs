using System.Text.Json.Serialization;

namespace CoC_API.Models
{

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CampaignStatus
    {
        New,
        Scheduled,
        Sending,
        Closed,
        InReview,
        Approved,
        Canceled
    }

    public enum CampaignModel
    {
        Email,
        Messaging,
        Instagram,
        Facebook,
        LinkedIn
    }

    public enum UserCaracteristics
    {
        Admin,
        isAprovando,
        isAprovado,
        isActive
    }
}