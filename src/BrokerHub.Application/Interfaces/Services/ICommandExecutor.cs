using System.Text.Json;
using BrokerHub.Application.DTOs.Commands;

namespace BrokerHub.Application.Interfaces.Services;

public interface ICommandExecutor
{
    Task<CommandResult> ExecuteAsync(string operation, string idempotencyKey, JsonElement validatedIntent,
        Func<CancellationToken, Task<CommandPermission>> authorize,
        Func<CommandExecutionContext, CancellationToken, Task<CommandEffect>> effect, CancellationToken ct);
}
