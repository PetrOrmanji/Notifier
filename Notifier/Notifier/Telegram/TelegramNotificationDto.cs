namespace Notifier.Telegram;

public class TelegramNotificationDto
{
    public string Text { get; set; }
    public string Link { get; set; }

    public DateTime DateTime { get; set; }

    public TelegramNotificationDto(string text, string link, DateTime dateTime)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentNullException(text);

        if (string.IsNullOrWhiteSpace(link))
            throw new ArgumentNullException(link);

        Text = text;
        Link = link;
        DateTime = dateTime;
    }

    public override string ToString()
         => $"📌 Назначили задачу ({DateTime: dd.MM.yyyy HH:mm}):\n<a href=\"{Link}\">{Text}</a>";
}
