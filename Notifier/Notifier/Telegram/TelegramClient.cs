namespace Notifier.Telegram;

public class TelegramClient
{
    private readonly string _botToken;
    private readonly string _chatId;
    private readonly string _apiUrl;
    private readonly HttpClient _httpClient;

    public TelegramClient(string botToken, string chatId)
    {
        if (string.IsNullOrWhiteSpace(botToken))
            throw new ArgumentNullException(nameof(botToken));
        if (string.IsNullOrWhiteSpace(chatId))
            throw new ArgumentNullException(nameof(chatId));

        _botToken = botToken;
        _chatId = chatId;
        _apiUrl = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        _httpClient = new HttpClient();
    }

    public async Task SendToTelegramAsync(TelegramNotificationDto telegramNotificationDto)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("chat_id", _chatId),
            new KeyValuePair<string, string>("text", telegramNotificationDto.ToString()),
            new KeyValuePair<string, string>("parse_mode", "HTML")
        });

        var response = await _httpClient.PostAsync(_apiUrl, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Telegram API error: {response.StatusCode}, {responseBody}");
        }
    }
}