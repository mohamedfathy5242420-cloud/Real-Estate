using System.Text.Json;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Serialization;

namespace BrokerHub.Application.Services;

public sealed class CommandExecutor(ICommandExecutionStore store, ICurrentUser currentUser) : ICommandExecutor
{
    public Task<CommandResult> ExecuteAsync(string operation, string idempotencyKey, JsonElement validatedIntent,
        Func<CancellationToken, Task<CommandPermission>> authorize,
        Func<CommandExecutionContext, CancellationToken, Task<CommandEffect>> effect, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.AccountId is not { } actor || actor == Guid.Empty)
            return Task.FromResult(new CommandResult(CommandStatus.Forbidden));
        var key = idempotencyKey?.Trim();
        if (!CanonicalCommandJson.IsIdentifier(operation, 64, true) || !CanonicalCommandJson.IsIdentifier(key, 128))
            return Task.FromResult(new CommandResult(CommandStatus.Invalid));
        string hash;
        try { hash = CanonicalCommandJson.Hash(validatedIntent); }
        catch (ArgumentException) { return Task.FromResult(new CommandResult(CommandStatus.Invalid)); }
        return store.ExecuteAsync(new(Guid.NewGuid(), actor, operation, key!, hash), authorize, effect, ct);
    }
}
