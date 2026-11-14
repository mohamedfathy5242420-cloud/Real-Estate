namespace BrokerHub.Application.DTOs.Commands;

public enum CommandStatus { Invalid, Success, Forbidden, NotFound, Conflict }
public enum CommandPermission { Forbidden, Allowed, NotFound }
public sealed record CommandOutcome(string ResourceType, Guid ResourceId, long ResourceVersion);
public sealed record CommandResult(CommandStatus Status, CommandOutcome? Outcome = null, bool IsReplay = false);
public sealed record CommandEffect(CommandStatus Status, CommandOutcome? Outcome = null);
public sealed record CommandExecutionRequest(Guid CommandId, Guid ActorAccountId, string Operation, string Key, string RequestHash);
public sealed record OutboxEventIntent(Guid Id, string Name, int SchemaVersion, Guid AggregateId, string PayloadJson, DateTime OccurredAtUtc);
