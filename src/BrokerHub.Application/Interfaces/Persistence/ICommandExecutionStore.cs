using BrokerHub.Application.DTOs.Commands;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface ICommandExecutionStore
{
    Task<CommandResult> ExecuteAsync(CommandExecutionRequest request,
        Func<CancellationToken, Task<CommandPermission>> authorize,
        Func<CommandExecutionContext, CancellationToken, Task<CommandEffect>> effect, CancellationToken ct);
}
