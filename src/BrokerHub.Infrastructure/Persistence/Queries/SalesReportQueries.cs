using System.Data;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Deals;
using BrokerHub.Application.DTOs.Reports;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BrokerHub.Infrastructure.Persistence.Queries;

internal sealed class SalesReportQueries(ApplicationDbContext db):ISalesReportQueries
{
    private IQueryable<Guid> Roles(string role)=>from ar in db.UserRoles join r in db.Roles on ar.RoleId equals r.Id
        where r.NormalizedName==role select ar.UserId;
    private Task<bool> ManagerAllowed(Guid actor,CancellationToken ct)=>db.Users.AnyAsync(u=>u.Id==actor&&u.IsActive&&u.EmailConfirmed&&Roles("SALESMANAGER").Contains(u.Id),ct);
    public async Task<DealResult<CrmActivityReportDto>> ActivityAsync(Guid actor,ActivityReportFilterDto filter,DateTime observedUtc,CancellationToken ct)
    {
        var managers=Roles("SALESMANAGER");var employees=Roles("SALESEMPLOYEE");
        var active=db.Users.Where(u=>u.Id==actor&&u.IsActive&&u.EmailConfirmed);
        if(!await active.AnyAsync(u=>managers.Contains(actor)||employees.Contains(actor),ct))return new(CommandStatus.Forbidden);
        var manager=await managers.ContainsAsync(actor,ct);
        var scoped=db.Customers.AsNoTracking().Where(c=>active.Any()&&(manager?managers.Contains(actor):
            employees.Contains(actor)&&db.Employees.Any(e=>e.Id==c.AssignedEmployeeId&&e.AccountId==actor)));
        var customers=scoped;
        var leads=db.Leads.AsNoTracking().Where(l=>scoped.Any(c=>c.Id==l.CustomerId));
        var viewings=db.Viewings.AsNoTracking().Where(v=>scoped.Any(c=>c.Id==v.CustomerId));
        var bookings=db.Bookings.AsNoTracking().Where(b=>scoped.Any(c=>c.Id==b.CustomerId));
        var deals=db.Deals.AsNoTracking().Where(d=>scoped.Any(c=>c.Id==d.CustomerId));
        var related=viewings.Select(v=>v.UnitId).Union(bookings.Select(b=>b.UnitId)).Union(deals.Select(d=>d.UnitId));
        var units=db.Units.AsNoTracking().Where(u=>active.Any()&&(manager?managers.Contains(actor):employees.Contains(actor)&&related.Contains(u.Id)));
        if(filter.FromUtcDate is { } from)
        {
            var day=from.ToDateTime(TimeOnly.MinValue);customers=customers.Where(c=>c.CreatedAtUtc.Date>=day);leads=leads.Where(l=>l.OpenedAtUtc.Date>=day);
            units=units.Where(u=>u.CreatedAtUtc.Date>=day);viewings=viewings.Where(v=>v.RequestedAtUtc.Date>=day);
            bookings=bookings.Where(b=>b.RequestedAtUtc.Date>=day);deals=deals.Where(d=>d.SaleOccurredAtUtc.Date>=day);
        }
        if(filter.ThroughUtcDate is { } through)
        {
            var day=through.ToDateTime(TimeOnly.MinValue);customers=customers.Where(c=>c.CreatedAtUtc.Date<=day);leads=leads.Where(l=>l.OpenedAtUtc.Date<=day);
            units=units.Where(u=>u.CreatedAtUtc.Date<=day);viewings=viewings.Where(v=>v.RequestedAtUtc.Date<=day);
            bookings=bookings.Where(b=>b.RequestedAtUtc.Date<=day);deals=deals.Where(d=>d.SaleOccurredAtUtc.Date<=day);
        }
        var result=new CrmActivityReportDto(observedUtc,manager?"AllCustomersAndCatalog":"AssignedCustomers",
            await customers.LongCountAsync(ct),await customers.LongCountAsync(c=>c.AssignedEmployeeId==null,ct),
            await leads.LongCountAsync(ct),await leads.LongCountAsync(l=>l.Status==Lead.Open,ct),await units.LongCountAsync(ct),
            await viewings.LongCountAsync(ct),await viewings.LongCountAsync(v=>
                (v.Status==Viewing.PendingConfirmation&&v.CurrentProposedStartUtc>observedUtc&&v.PendingCycleStartedAtUtc.AddDays(7)>observedUtc)||
                (v.Status==Viewing.AwaitingCustomerAcceptance&&v.CurrentProposedStartUtc>observedUtc),ct),
            await viewings.LongCountAsync(v=>v.Status==Viewing.Confirmed,ct),await bookings.LongCountAsync(ct),
            await bookings.LongCountAsync(b=>b.Status==Booking.PendingReview&&b.ResponseDueAtUtc>observedUtc,ct),
            await bookings.LongCountAsync(b=>b.Status==Booking.Confirmed&&b.ReservationExpiresAtUtc>observedUtc,ct),await deals.LongCountAsync(ct));
        // A role change during the live report cannot return a result with an obsolete scope label.
        if(!await active.AnyAsync(u=>manager?managers.Contains(actor):employees.Contains(actor)&&!managers.Contains(actor),ct))return new(CommandStatus.Forbidden);
        return new(CommandStatus.Success,result);
    }
    public async Task<DealResult<DealPage<CommissionCurrencyTotalDto>>> CommissionsAsync(Guid actor,CommissionReportFilterDto filter,CancellationToken ct)
    {
        if(!await ManagerAllowed(actor,ct))return new(CommandStatus.Forbidden);
        var connection=(SqlConnection)db.Database.GetDbConnection();var opened=connection.State==ConnectionState.Closed;
        if(opened)await db.Database.OpenConnectionAsync(ct);
        try
        {
            async Task<long> Scalar(string suffix)
            {
                using var command=Command(connection,actor,filter,FinancialCte+suffix);
                return Convert.ToInt64(await command.ExecuteScalarAsync(ct));
            }
            if(await Scalar("SELECT COUNT_BIG(*) FROM PerDeal WHERE Invalid=1")>0)
                return new(await ManagerAllowed(actor,ct)?CommandStatus.Conflict:CommandStatus.Forbidden);
            var total=checked((int)await Scalar("SELECT COUNT_BIG(*) FROM (SELECT CurrencyCode FROM PerDeal GROUP BY CurrencyCode) currencies"));
            var items=new List<CommissionCurrencyTotalDto>();var invalid=false;
            using(var command=Command(connection,actor,filter,FinancialCte+"""
                SELECT CurrencyCode,COUNT_BIG(*) AS DealCount,SUM(CollectionCount) AS CollectionCount,
                       CONVERT(varchar(50),SUM(EntitledAmount)) AS EntitledAmount,
                       CONVERT(varchar(50),SUM(CollectedAmount)) AS CollectedAmount,
                       CONVERT(varchar(50),SUM(EntitledAmount-CollectedAmount)) AS RemainingAmount,
                       MAX(Invalid) AS Invalid
                FROM PerDeal GROUP BY CurrencyCode
                ORDER BY CurrencyCode COLLATE Latin1_General_100_BIN2
                OFFSET @offset ROWS FETCH NEXT @size ROWS ONLY
                """))
            await using(var reader=await command.ExecuteReaderAsync(ct))
            {
                while(await reader.ReadAsync(ct))
                {
                    invalid|=reader.GetInt32(6)!=0;
                    items.Add(new(reader.GetString(0),reader.GetInt64(1),reader.GetInt64(2),reader.GetString(3),reader.GetString(4),reader.GetString(5)));
                }
            }
            if(!await ManagerAllowed(actor,ct))return new(CommandStatus.Forbidden);
            return invalid?new(CommandStatus.Conflict):new(CommandStatus.Success,new(filter.Page,filter.PageSize,total,items));
        }
        finally{if(opened)await db.Database.CloseConnectionAsync();}
    }
    private SqlCommand Command(SqlConnection connection,Guid actor,CommissionReportFilterDto filter,string sql)
    {
        var command=connection.CreateCommand();command.CommandText=sql;command.CommandTimeout=db.Database.GetCommandTimeout()??30;
        command.Transaction=db.Database.CurrentTransaction?.GetDbTransaction() as SqlTransaction;
        command.Parameters.Add(new SqlParameter("@actor",SqlDbType.UniqueIdentifier){Value=actor});
        command.Parameters.Add(new SqlParameter("@customer",SqlDbType.UniqueIdentifier){Value=(object?)filter.CustomerId??DBNull.Value});
        command.Parameters.Add(new SqlParameter("@project",SqlDbType.UniqueIdentifier){Value=(object?)filter.ProjectId??DBNull.Value});
        command.Parameters.Add(new SqlParameter("@developer",SqlDbType.UniqueIdentifier){Value=(object?)filter.DeveloperId??DBNull.Value});
        command.Parameters.Add(new SqlParameter("@currency",SqlDbType.VarChar,3){Value=(object?)filter.CurrencyCode??DBNull.Value});
        command.Parameters.Add(new SqlParameter("@from",SqlDbType.Date){Value=filter.SaleLocalFrom is { } from?from.ToDateTime(TimeOnly.MinValue):DBNull.Value});
        command.Parameters.Add(new SqlParameter("@through",SqlDbType.Date){Value=filter.SaleLocalThrough is { } through?through.ToDateTime(TimeOnly.MinValue):DBNull.Value});
        command.Parameters.Add(new SqlParameter("@offset",SqlDbType.Int){Value=(filter.Page-1)*filter.PageSize});
        command.Parameters.Add(new SqlParameter("@size",SqlDbType.Int){Value=filter.PageSize});return command;
    }
    private const string FinancialCte="""
        WITH Selected AS (
            SELECT d.Id,d.CurrencyCode,d.Amount
            FROM [Sales].[Deals] d
            WHERE EXISTS(SELECT 1 FROM [Identity].[Accounts] u JOIN [Identity].[AccountRoles] ar ON ar.UserId=u.Id
                         JOIN [Identity].[Roles] r ON r.Id=ar.RoleId
                         WHERE u.Id=@actor AND u.IsActive=1 AND u.EmailConfirmed=1 AND r.NormalizedName=N'SALESMANAGER')
              AND (@customer IS NULL OR d.CustomerId=@customer) AND (@project IS NULL OR d.ProjectId=@project)
              AND (@developer IS NULL OR d.DeveloperId=@developer) AND (@currency IS NULL OR d.CurrencyCode=@currency)
              AND (@from IS NULL OR d.SaleLocalDate>=@from) AND (@through IS NULL OR d.SaleLocalDate<=@through)
        ), Ledger AS (
            SELECT c.DealId,SUM(c.Amount) AS Collected,COUNT_BIG(*) AS CollectionCount
            FROM [Sales].[CommissionCollections] c JOIN Selected s ON s.Id=c.DealId GROUP BY c.DealId
        ), PerDeal AS (
            SELECT s.Id,s.CurrencyCode,s.Amount AS EntitledAmount,COALESCE(l.CollectionCount,CONVERT(bigint,0)) AS CollectionCount,
                   CONVERT(decimal(18,2),CASE WHEN COALESCE(l.Collected,0)>=0 AND COALESCE(l.Collected,0)<=s.Amount
                           THEN COALESCE(l.Collected,0) ELSE 0 END) AS CollectedAmount,
                   CASE WHEN COALESCE(l.Collected,0)<0 OR COALESCE(l.Collected,0)>s.Amount THEN 1 ELSE 0 END AS Invalid
            FROM Selected s LEFT JOIN Ledger l ON l.DealId=s.Id
        )

        """;
}
