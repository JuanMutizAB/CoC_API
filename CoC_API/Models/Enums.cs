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
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CampaignModel
    {
        Email,
        Messaging,
        Instagram,
        Facebook,
        LinkedIn
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserCaracteristics
    {
        Admin,
        isAprovando,
        isAprovado,
        isActive
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Motivo
    {
        InadequateContent,
        InadequateLanguage,
        InadequateImage,
        InadequateTargetAudience,
        Other
    }
}