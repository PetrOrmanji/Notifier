using Notifier.Mail;
using Notifier.Telegram;

namespace Notifier.Services.Background;

public class TasksCheckerService : BackgroundService
{
    private const string NewLineSeparator = "\r\n";
    private const string LongLineSeparatorForCreate = "------------------------------";
    private const string LongLineSeparatorForUpdate = "----------------------------------------";

    private readonly MailClient _mailClient;
    private readonly TelegramClient _telegramClient;
    private readonly PeriodicTimer _timer;
    private readonly ILogger<TasksCheckerService> _logger;

    private DateTime _lastCheckDateTime;

    public TasksCheckerService(
        MailClient mailClient,
        TelegramClient telegramClient,
        ILogger<TasksCheckerService> logger)
    {
        _mailClient = mailClient ?? throw new ArgumentNullException(nameof(mailClient));
        _telegramClient = telegramClient ?? throw new ArgumentNullException(nameof(telegramClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _timer = new(TimeSpan.FromMinutes(5));
        _lastCheckDateTime = DateTime.Now.AddDays(-15);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await DoWorkAsync(cancellationToken);

        while (await _timer.WaitForNextTickAsync(cancellationToken) && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                await DoWorkAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{nameof(TasksCheckerService)}.{nameof(ExecuteAsync)}].");
            }
        }
    }

    private async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        const string baseLog = $"[{nameof(TasksCheckerService)}.{nameof(DoWorkAsync)}]";

        var msgs = await _mailClient.GetMessagesDeliveredAfter(_lastCheckDateTime);
        if (msgs.Length == 0)
        {
            _logger.LogInformation($"{baseLog}. New messages not found.");
            return;
        }

        _logger.LogInformation($"{baseLog}. Found {msgs.Length} new message(s)");

        var maxMsgsDateTime = msgs.Max(x => x.DateTime);
        _lastCheckDateTime = maxMsgsDateTime > _lastCheckDateTime ? maxMsgsDateTime : _lastCheckDateTime;

        var telegramNotifications = HandleMessages(msgs);
        if (telegramNotifications.Length == 0)
        {
            _logger.LogInformation($"{baseLog}. No notifications to send.");
            return;
        }

        foreach (var notification in telegramNotifications)
        {
            try
            {
                await _telegramClient.SendToTelegramAsync(notification);
                _logger.LogInformation("Notification sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Telegram notification");
            }
        }
    }

    private TelegramNotificationDto[] HandleMessages(ImapMessageDto[] msgs)
    {
        const string baseLog = $"[{nameof(TasksCheckerService)}.{nameof(HandleMessages)}";

        var resultList = new List<TelegramNotificationDto>();

        foreach (var msg in msgs)
        {
            if (string.IsNullOrWhiteSpace(msg.Text))
            {
                _logger.LogWarning("Message text is null or empty");
                continue;
            }

            var msgLines = msg.Text.Split(NewLineSeparator);

            if (msgLines.Length == 0)
            {
                _logger.LogWarning("Message has no lines after split");
                continue;
            }

            var whatsHappenedLine = msgLines[0];

            if (whatsHappenedLine.StartsWith("Создана новая задача"))
            {
                var firstSeparatorIndex = Array.IndexOf(msgLines, LongLineSeparatorForCreate);
                var telegramNotificationDto = new TelegramNotificationDto(
                    msgLines[firstSeparatorIndex + 1],
                    msgLines[firstSeparatorIndex + 2].Replace(" открыто", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty),
                    msg.DateTime);

                resultList.Add(telegramNotificationDto);
            }
            else if (whatsHappenedLine.Contains("была обновлена"))
            {
                var firstSeparatorIndex = Array.IndexOf(msgLines, LongLineSeparatorForUpdate);
                if (firstSeparatorIndex == -1 || firstSeparatorIndex + 2 >= msgLines.Length)
                {
                    _logger.LogWarning("Invalid message format: separator not found or insufficient lines");
                    continue;
                }

                var assignmentChangedLine =
                    msgLines.FirstOrDefault(x => x.StartsWith("Параметр Назначена изменился с") && x.EndsWith("Петр Орманжи"));

                if (assignmentChangedLine == null)
                {
                    continue;
                }

                var telegramNotificationDto = new TelegramNotificationDto(
                    msgLines[firstSeparatorIndex + 1],
                    msgLines[firstSeparatorIndex + 2],
                    msg.DateTime);

                resultList.Add(telegramNotificationDto);
            }
        }

        return resultList.ToArray();
    }
}
