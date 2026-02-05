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
            var msgDate = msgsDates.FirstOrDefault(x => x.UniqueId == msgId)?.Date.LocalDateTime;

            if (msgDate == null || msgDate <= dateTime)
            {
                continue;
            }

            var message = specificFolder.GetMessage(msgId);
            resultList.Add(new ImapMessageDto(message.TextBody, msgDate.Value));
        }

        resultList.Add(new ImapMessageDto("Создана новая задача #10718 <http://redmine.payture.com/issues/10718>\r\n(Антон Елисеев).\r\n------------------------------\r\nTask #10718: Перенос пароля метода getbininfo в Vault\r\n<http://redmine.payture.com/issues/10718> открыто\r\n\r\n   - *Автор: *Антон Елисеев\r\n   - *Статус: *Projected\r\n   - *Приоритет: *Normal\r\n   - *Назначена: *Петр Орманжи\r\n   - *Дата начала: *2026-02-02\r\n   - *Срок завершения: *2026-02-26\r\n   - *Сервис: *GetBinInfo\r\n   - *Часы Dev: *0\r\n   - *Часы QA: *0\r\n\r\nПереносим пароль в {env}/admin/getbininfo\r\n\r\nЗатронет ручки:\r\ngetbininfo/updateBinBase/(.*)\r\ngetbininfo/Reload/(.*)\r\ngetbininfo/save/(.*)\r\ngetbininfo/remove/(.*)\r\n------------------------------\r\n\r\nYou have received this notification because you have either subscribed to\r\nit, or are involved in it...", DateTime.Now));

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
