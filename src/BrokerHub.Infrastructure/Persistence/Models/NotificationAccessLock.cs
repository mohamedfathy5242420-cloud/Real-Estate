using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Infrastructure.Persistence.Models;

internal sealed record NotificationAccessLock(CommandStatus Status,Notification? Notification=null);
