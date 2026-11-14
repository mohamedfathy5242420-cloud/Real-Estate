namespace BrokerHub.Application.DTOs.Catalog;

public enum PublicImageWarmupOfferStatus { Accepted, Busy, Disabled, Stopped, Invalid }
public sealed record PublicImageWarmupOffer(PublicImageWarmupOfferStatus Status,int AcceptedCount,int SkippedCount);
