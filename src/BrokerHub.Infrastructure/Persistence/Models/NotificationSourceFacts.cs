using BrokerHub.Application.DTOs.Messaging;

namespace BrokerHub.Infrastructure.Persistence.Models;

internal sealed record NotificationSourceFacts(OutboxEnvelope Envelope,NotificationEventDefinition Definition,
    string BodyHash,Guid? CustomerId,Guid ResourceId,Guid? ParentResourceId,Guid? RelatedResourceId);
