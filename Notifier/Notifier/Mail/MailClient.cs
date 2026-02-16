using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;

namespace Notifier.Mail;

public class MailClient
{
    private const string ImapHost = "imap.gmail.com";
    private const short ImapPort = 993;

    private readonly string _userName;
    private readonly string _password;
    private readonly string _folder;

    public MailClient(string userName, string password, string folder)
    {
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentNullException(nameof(userName));
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentNullException(nameof(password));
        if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentNullException(nameof(folder));

        _userName = userName;
        _password = password;
        _folder = folder;
    }

    public async Task<ImapMessageDto[]> GetMessagesDeliveredAfter(DateTime dateTime)
    {
        using var client = new ImapClient();

        await client.ConnectAsync(ImapHost, ImapPort, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(_userName, _password);

        var specificFolder = await client.GetFolderAsync(_folder);
        await specificFolder.OpenAsync(FolderAccess.ReadOnly);

        var msgsIds = await specificFolder.SearchAsync(SearchQuery.DeliveredAfter(dateTime));
        var msgsDates = await specificFolder.FetchAsync(msgsIds, MessageSummaryItems.InternalDate);

        var resultList = new List<ImapMessageDto>();
        
        foreach (var msgId in msgsIds)
        {
            var msgDate = msgsDates.FirstOrDefault(x => x.UniqueId == msgId)?.Date.UtcDateTime;

            if (msgDate == null || msgDate <= dateTime)
            {
                continue;
            }

            var msgMoscowDate = msgDate.Value.AddHours(3);

            var message = await specificFolder.GetMessageAsync(msgId);
            resultList.Add(new ImapMessageDto(message.TextBody, msgMoscowDate));
        }

        await client.DisconnectAsync(true);

        return resultList.ToArray();
    }
}
