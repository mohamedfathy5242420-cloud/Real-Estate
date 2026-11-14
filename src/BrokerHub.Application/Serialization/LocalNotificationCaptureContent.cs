using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BrokerHub.Application.DTOs.Notifications;

namespace BrokerHub.Application.Serialization;

public static class LocalNotificationCaptureContent
{
    public const int MaxBytes=16384;
    public static LocalNotificationArtifact Create(UserNotificationDto notification,Guid recipientAccountId)
    {
        if(notification.Id==Guid.Empty||recipientAccountId==Guid.Empty)throw new ArgumentException("Owned capture identities required.");
        var subject=Convert.ToBase64String(Encoding.UTF8.GetBytes(notification.Title));var resource=notification.Resource;
        var text=notification.Message+"\r\nOccurredAtUtc: "+notification.OccurredAtUtc.UtcDateTime.ToString("O",CultureInfo.InvariantCulture)+
            "\r\nResource: "+resource.Kind+"/"+resource.Id.ToString("D")+"\r\nCustomerId: "+resource.CustomerId?.ToString("D")+
            "\r\nParentId: "+resource.ParentId?.ToString("D")+"\r\nRelatedId: "+resource.RelatedId?.ToString("D")+"\r\n";
        var encoded=Convert.ToBase64String(Encoding.UTF8.GetBytes(text),Base64FormattingOptions.InsertLineBreaks);
        var content="From: BrokerHub Local Capture <noreply@brokerhub.invalid>\r\nTo: <capture-"+recipientAccountId.ToString("N")+"@brokerhub.invalid>\r\n"+
            "Message-ID: <notification-"+notification.Id.ToString("N")+"@brokerhub.invalid>\r\nDate: "+notification.CreatedAtUtc.UtcDateTime.ToString("r",CultureInfo.InvariantCulture)+
            "\r\nSubject: =?utf-8?B?"+subject+"?=\r\nMIME-Version: 1.0\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Transfer-Encoding: base64\r\nX-BrokerHub-Local-Capture: true\r\n\r\n"+encoded+"\r\n";
        var bytes=Encoding.UTF8.GetBytes(content);if(bytes.Length>MaxBytes)throw new ArgumentException("Bounded safe capture content required.");
        return new(notification.Id,bytes,Convert.ToHexString(SHA256.HashData(bytes)));
    }
}
