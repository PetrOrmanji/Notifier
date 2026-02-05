using Notifier.Mail;
using Notifier.Services.Background;
using Notifier.Telegram;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(sp =>
    new MailClient(
        builder.Configuration["Mail:UserName"]!,
        builder.Configuration["Mail:Password"]!,
        builder.Configuration["Mail:Folder"]!
        ));

builder.Services.AddSingleton(sp =>
    new TelegramClient(
        builder.Configuration["Telegram:BotToken"]!,
        builder.Configuration["Telegram:ChatId"]!
    ));

builder.Services.AddHostedService<TasksCheckerService>();

var host = builder.Build();
host.Run();