// I01-S08 AC-05/09/14: owned diagnostic only. No hosted workers or HTTP server start.
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using BrokerHub.Application.DTOs.Messaging;
using BrokerHub.Application.DTOs.Notifications;
using BrokerHub.Application.Interfaces.Messaging;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Infrastructure;
using BrokerHub.Infrastructure.Messaging;
using BrokerHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var checks=new List<object>();
void Check(string name,bool passed){checks.Add(new{name,passed});if(!passed)throw new InvalidOperationException("Diagnostic check failed");}
try
{
    if(args.Length!=4||args.Any(x=>!Guid.TryParse(x,out var id)||id==Guid.Empty))throw new ArgumentException("Owned fixture IDs required");
    var customer=Guid.Parse(args[0]);var viewing=Guid.Parse(args[1]);var account=Guid.Parse(args[2]);var notification=Guid.Parse(args[3]);
    var builder=WebApplication.CreateBuilder(new WebApplicationOptions{EnvironmentName="Development",ContentRootPath="/app"});
    builder.Logging.ClearProviders();
    builder.Configuration["LocalNotificationCapture:Enabled"]="true";
    builder.Configuration["LocalNotificationCapture:RootPath"]="/capture";
    builder.Configuration["LocalNotificationCapture:AllowedAccountIds:0"]=account.ToString();
    builder.Services.AddInfrastructure(builder.Configuration,builder.Environment);
    await using var host=builder.Build();
    async Task Tls(string name,bool emptyTrust=false)
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var tcp=new TcpClient();await tcp.ConnectAsync("rabbitmq",5671,deadline.Token);
        using var ssl=new SslStream(tcp.GetStream());
        var options=new SslClientAuthenticationOptions{TargetHost=name};
        if(emptyTrust)options.CertificateChainPolicy=new X509ChainPolicy{TrustMode=X509ChainTrustMode.CustomRootTrust};
        await ssl.AuthenticateAsClientAsync(options,deadline.Token);
    }
    await Tls("rabbitmq");Check("Actual TLS accepts installed CA and matching rabbitmq hostname",true);
    var wrong=false;try{await Tls("wrong.invalid");}catch(AuthenticationException){wrong=true;}Check("Actual TLS rejects mismatched server hostname",wrong);
    var untrusted=false;try{await Tls("rabbitmq",true);}catch(AuthenticationException){untrusted=true;}Check("Actual TLS rejects certificate with empty trusted-root set",untrusted);
    await using var scope=host.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var human=await db.OutboxMessages.AsNoTracking().SingleAsync(e=>e.AggregateId==customer&&e.Name=="Viewing.Requested"&&e.PayloadJson.Contains(viewing.ToString()));
    var system=await db.SystemOutboxMessages.AsNoTracking().SingleAsync(e=>e.AggregateId==customer&&e.ResourceId==viewing&&e.Name=="Viewing.Expired");
    var deliveries=await db.OutboxDeliveries.AsNoTracking().Where(d=>d.HumanOutboxId==human.Id||d.SystemOutboxId==system.Id).ToArrayAsync();
    Check("BOTH original human and actorless System events have stable technical deliveries",deliveries.Length==2&&human.ActorAccountId!=Guid.Empty&&system.Source=="System.ViewingExpiry");
    var deliveryIds=deliveries.Select(x=>x.Id).ToArray();
    async Task<string> Snapshot()=>JsonSerializer.Serialize(new{Inbox=await db.InboxMessages.AsNoTracking().Where(i=>deliveryIds.Contains(i.MessageId)).OrderBy(i=>i.Id).ToArrayAsync(),Notifications=await db.Notifications.AsNoTracking().Where(n=>n.ResourceId==viewing).OrderBy(n=>n.Id).ToArrayAsync(),Viewing=await db.Viewings.AsNoTracking().Where(v=>v.Id==viewing).Select(v=>new{v.Id,v.Version,v.Status}).SingleAsync()});
    var before=await Snapshot();Check("BOTH deliveries already reached real SQL Inbox",await db.InboxMessages.CountAsync(i=>deliveryIds.Contains(i.MessageId))==2);
    var publisher=scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();var consumer=scope.ServiceProvider.GetRequiredService<INotificationMessageConsumer>();
    static DateTime Utc(DateTime t)=>DateTime.SpecifyKind(t,DateTimeKind.Utc);
    foreach(var delivery in deliveries)
    {
        var envelope=delivery.HumanOutboxId.HasValue?new OutboxEnvelope(1,delivery.Id,"OutboxMessages",human.Id,human.Name,human.SchemaVersion,human.AggregateId,human.ActorAccountId,null,null,null,Utc(human.OccurredAtUtc),Utc(human.RecordedAtUtc),human.PayloadJson):
            new OutboxEnvelope(1,delivery.Id,"SystemOutboxMessages",system.Id,system.Name,system.SchemaVersion,system.AggregateId,null,system.Source,system.ResourceId,system.ResourceVersion,Utc(system.OccurredAtUtc),Utc(system.RecordedAtUtc),system.PayloadJson);
        var duplicate=await consumer.ConsumeAsync(OutboxEnvelopeWire.Serialize(envelope),new(envelope.MessageId.ToString("N"),"application/json",envelope.Name,true),default);
        Check(envelope.SourceStore+" exact original envelope is SQL Duplicate",duplicate.Status==NotificationConsumeStatus.Duplicate);
        var sent=await publisher.PublishAsync(envelope,default);Check(envelope.SourceStore+" same original MessageId republished with real broker confirm",sent.Disposition==OutboxPublishDisposition.Confirmed);
    }
    await Task.Delay(2000);Check("Duplicate transport leaves immutable Inbox/notifications and business root unchanged",before==await Snapshot());
    var capture=scope.ServiceProvider.GetRequiredService<ILocalNotificationCapture>();
    var first=await capture.CaptureAsync(account,notification,default);var second=await capture.CaptureAsync(account,notification,default);
    Check("Owned Development dummy-email capture succeeds with explicit account allowlist",first.Status==LocalNotificationCaptureStatus.Success);
    Check("Capture replay retains original receipt/hash/time",second.Status==LocalNotificationCaptureStatus.Success&&first.ContentHash==second.ContentHash&&first.CapturedAtUtc==second.CapturedAtUtc);
    Check("Capture has one durable receipt and one private deterministic artifact",await db.NotificationEmailCaptures.CountAsync(x=>x.NotificationId==notification)==1&&File.Exists(Path.Combine("/capture",notification.ToString("N")+".eml")));
    Console.WriteLine(JsonSerializer.Serialize(new{status="Passed",checks=checks.Count,results=checks}));
    return 0;
}
catch(Exception ex){Console.WriteLine(JsonSerializer.Serialize(new{status="Failed",checks=checks.Count,results=checks,failureType=ex.GetType().Name}));return 1;}
