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
    private readonly ImapClient _imapClient;

    public MailClient(string userName, string password, string folder)
    {
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentNullException(nameof(userName));
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentNullException(nameof(password));
        if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentNullException(nameof(folder));

        _userName = userName;
        _password = password;
        _folder = folder;

        _imapClient = new ImapClient();
    }

    public async Task<ImapMessageDto[]> GetMessagesDeliveredAfter(DateTime dateTime)
    {
        await EnsureConnectedAsync();

        var specificFolder = _imapClient.GetFolder(_folder);
        specificFolder.Open(FolderAccess.ReadOnly);

        var msgsIds = specificFolder.Search(SearchQuery.DeliveredAfter(dateTime));
        var msgsDates = specificFolder.Fetch(msgsIds, MessageSummaryItems.InternalDate);

        var resultList = new List<ImapMessageDto>();
        
        foreach (var msgId in msgsIds)
        {
            var msgDate = msgsDates.FirstOrDefault(x => x.UniqueId == msgId)?.Date.UtcDateTime;

            if (msgDate == null || msgDate <= dateTime)
            {
                continue;
            }

            var msgMoscowDate = msgDate.Value.AddHours(3);

            var message = specificFolder.GetMessage(msgId);
            resultList.Add(new ImapMessageDto(message.TextBody, msgMoscowDate));
        }

        return resultList.ToArray();
    }

    private async Task EnsureConnectedAsync()
    {
        if (!_imapClient.IsConnected)
        {
            await _imapClient.ConnectAsync(ImapHost, ImapPort, SecureSocketOptions.SslOnConnect);
            await _imapClient.AuthenticateAsync(_userName, _password);
        }
    }
}
