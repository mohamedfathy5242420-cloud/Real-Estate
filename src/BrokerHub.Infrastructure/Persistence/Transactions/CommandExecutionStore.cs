using System.Data;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Serialization;
using BrokerHub.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BrokerHub.Infrastructure.Persistence.Transactions;

internal sealed class CommandExecutionStore(ApplicationDbContext db, TimeProvider clock) : ICommandExecutionStore
{
    public async Task<CommandResult> ExecuteAsync(CommandExecutionRequest request,
        Func<CancellationToken, Task<CommandPermission>> authorize,
        Func<CommandExecutionContext, CancellationToken, Task<CommandEffect>> effect, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null || db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Command boundary requires no nested transaction or pending outside writes.");
        List<(EntityEntry Entry, PropertyValues Expected)> pending = [];
        CommandReceipt? receipt = null;
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(db, async (context, token) =>
            {
                // Retry callbacks reload their state; never reuse a rolled-back tracked aggregate.
                context.ChangeTracker.Clear(); pending = []; receipt = null;
                var active = await context.Database.SqlQuery<int>($"""
                    SELECT CONVERT(int, CASE WHEN [IsActive]=1 AND [EmailConfirmed]=1 THEN 1 ELSE 0 END) AS [Value]
                    FROM [Identity].[Accounts] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={request.ActorAccountId}
                    """).SingleOrDefaultAsync(token);
                if (active != 1) return new CommandResult(CommandStatus.Forbidden);
                var permission = await authorize(token);
                if (context.ChangeTracker.HasChanges()) throw new InvalidOperationException("Permission callback must be read-only.");
                if (permission != CommandPermission.Allowed)
                    return new CommandResult(permission == CommandPermission.NotFound ? CommandStatus.NotFound : CommandStatus.Forbidden);

                // Permission is checked first, even when this key was previously committed.
                // Match the indexed ANSI columns rather than converting them for Unicode raw parameters.
                var operation = new SqlParameter("operation", SqlDbType.VarChar, 64) { Value = request.Operation };
                var key = new SqlParameter("key", SqlDbType.VarChar, 128) { Value = request.Key };
                receipt = await context.CommandReceipts.FromSqlInterpolated($"""
                    SELECT * FROM [Reliability].[CommandReceipts] WITH (UPDLOCK,HOLDLOCK,INDEX([UX_CommandReceipts_ActorOperationKey]))
                    WHERE [ActorAccountId]={request.ActorAccountId} AND [Operation]={operation} AND [IdempotencyKey]={key}
                    """).AsNoTracking().SingleOrDefaultAsync(token);
                if (receipt is not null)
                    return receipt.FingerprintVersion == 1 && receipt.RequestHash == request.RequestHash
                        ? new CommandResult(CommandStatus.Success, Outcome(receipt), true) : new CommandResult(CommandStatus.Conflict);

                var command = new CommandExecutionContext(request.ActorAccountId, request.CommandId, clock);
                var result = await effect(command, token);
                if (result.Status != CommandStatus.Success)
                {
                    if (result.Status is not (CommandStatus.Invalid or CommandStatus.Forbidden or CommandStatus.NotFound or CommandStatus.Conflict) || result.Outcome is not null)
                        throw new InvalidOperationException("Invalid rejected command result.");
                    return new CommandResult(result.Status);
                }
                var outcome = result.Outcome ?? throw new InvalidOperationException("Successful command requires an outcome reference.");
                if (!CanonicalCommandJson.IsIdentifier(outcome.ResourceType, 64, true)) throw new ArgumentException("Invalid resource type.");
                var completedAt = clock.GetUtcNow().UtcDateTime;
                receipt = CommandReceipt.Create(request.CommandId, request.ActorAccountId, request.Operation, request.Key,
                    request.RequestHash, outcome.ResourceType, outcome.ResourceId, outcome.ResourceVersion, command.Events.Count, completedAt);
                context.CommandReceipts.Add(receipt);
                foreach (var e in command.Events)
                    context.OutboxMessages.Add(OutboxMessage.Record(e.Id, receipt.Id, request.ActorAccountId, e.Name, e.SchemaVersion,
                        e.AggregateId, e.PayloadJson, e.OccurredAtUtc, completedAt));
                context.ChangeTracker.DetectChanges();
                pending = context.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified)
                    .Select(e => (Entry: e, Expected: e.CurrentValues.Clone())).ToList();
                await context.SaveChangesAsync(acceptAllChangesOnSuccess: false, token);
                return new CommandResult(CommandStatus.Success, outcome);
            }, async (context, token) =>
            {
                // Resolve a possibly lost commit acknowledgment using every exact staged scalar.
                foreach (var (entry, expected) in pending)
                {
                    var persisted = await entry.GetDatabaseValuesAsync(token);
                    if (persisted is null || expected.Properties.Any(p => !Equals(expected[p], persisted[p]))) return false;
                }
                if (receipt is null) return true; // denied/rejected read-only attempt has no write to resolve
                var actual = await context.CommandReceipts.AsNoTracking().SingleOrDefaultAsync(r => r.Id == receipt.Id, token);
                return actual is not null && actual.ActorAccountId == receipt.ActorAccountId && actual.Operation == receipt.Operation &&
                    actual.IdempotencyKey == receipt.IdempotencyKey && actual.RequestHash == receipt.RequestHash &&
                    actual.FingerprintVersion == receipt.FingerprintVersion && actual.ResourceType == receipt.ResourceType &&
                    actual.ResourceId == receipt.ResourceId && actual.ResourceVersion == receipt.ResourceVersion &&
                    actual.CompletedAtUtc == receipt.CompletedAtUtc && actual.EventCount == receipt.EventCount &&
                    await context.OutboxMessages.CountAsync(e => e.CommandReceiptId == receipt.Id, token) == receipt.EventCount;
            }, ct);
        }
        catch (DbUpdateConcurrencyException) { return new(CommandStatus.Conflict); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 }) { return new(CommandStatus.Conflict); }
        finally { db.ChangeTracker.Clear(); }
    }
    private static CommandOutcome Outcome(CommandReceipt r) => new(r.ResourceType, r.ResourceId, r.ResourceVersion);
}
