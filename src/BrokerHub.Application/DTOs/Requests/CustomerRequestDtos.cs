using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.DTOs.Requests;
public sealed record CustomerRequestLock(CommandPermission Permission, Customer? Customer = null);
public sealed record RequestUnitIdentity(Guid UnitId, Guid ProjectId, Guid DeveloperId);
public sealed record RequestUnitContext(Guid UnitId, Guid ProjectId, Guid DeveloperId, bool Published,
    bool DeveloperActive, bool ProjectActive, string TimeZoneId);
