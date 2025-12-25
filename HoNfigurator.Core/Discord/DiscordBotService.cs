using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using HoNfigurator.Core.Models;
using HoNfigurator.Core.Connectors;

namespace HoNfigurator.Core.Discord;

/// <summary>
/// Discord bot service for HoNfigurator notifications and commands
/// </summary>
public interface IDiscordBotService : IDisposable
{
    bool IsConnected { get; }
    bool IsEnabled { get; }
    Task StartAsync();
    Task StopAsync();
    
    // Notification methods
    Task SendServerNotificationAsync(string title, string message, NotificationLevel level = NotificationLevel.Info);
    Task SendMatchStartedAsync(int serverId, string serverName, List<string> players);
    Task SendMatchEndedAsync(int serverId, string serverName, int duration, string? winner = null);
    Task SendPlayerJoinedAsync(int serverId, string serverName, string playerName);
    Task SendPlayerLeftAsync(int serverId, string serverName, string playerName);
    Task SendAlertAsync(string title, string message);
}

public enum NotificationLevel
{
    Info,
    Warning,
    Error,
    Success
}

/// <summary>
/// Discord bot implementation using Discord.Net
/// </summary>
public class DiscordBotService : IDiscordBotService
{
    private readonly ILogger<DiscordBotService> _logger;
    private readonly HoNConfiguration _config;
    private readonly DiscordSocketClient _client;
    private readonly IMqttHandler? _mqttHandler;
    private ulong _notificationChannelId;
    private bool _disposed;

    public bool IsConnected => _client?.ConnectionState == ConnectionState.Connected;
    public bool IsEnabled => !string.IsNullOrEmpty(_config.ApplicationData?.Discord?.BotToken);

    public DiscordBotService(
        ILogger<DiscordBotService> logger, 
        HoNConfiguration config,
        IMqttHandler? mqttHandler = null)
    {
        _logger = logger;
        _config = config;
        _mqttHandler = mqttHandler;

        var socketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | 
                            GatewayIntents.GuildMessages | 
                            GatewayIntents.MessageContent |
                            GatewayIntents.DirectMessages,
            LogLevel = LogSeverity.Info
        };

        _client = new DiscordSocketClient(socketConfig);
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += MessageReceivedAsync;
    }

    public async Task StartAsync()
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Discord bot is disabled (no bot token configured)");
            return;
        }

        try
        {
            var token = _config.ApplicationData?.Discord?.BotToken;
            _logger.LogInformation("Starting Discord bot...");
            
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            
            // Parse notification channel ID if configured
            var channelIdStr = _config.ApplicationData?.Discord?.NotificationChannelId;
            if (!string.IsNullOrEmpty(channelIdStr) && ulong.TryParse(channelIdStr, out var channelId))
            {
                _notificationChannelId = channelId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Discord bot");
        }
    }

    public async Task StopAsync()
    {
        if (_client.ConnectionState == ConnectionState.Disconnected)
            return;

        try
        {
            _logger.LogInformation("Stopping Discord bot...");
            await _client.StopAsync();
            await _client.LogoutAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Discord bot");
        }
    }

    private Task LogAsync(LogMessage log)
    {
        var logLevel = log.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, log.Exception, "[Discord] {Message}", log.Message);
        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        _logger.LogInformation("Discord bot connected as {User}", _client.CurrentUser?.Username);
        
        // Set custom status
        await _client.SetGameAsync("HoN Servers", type: ActivityType.Watching);
        
        // Send startup notification
        await SendServerNotificationAsync(
            "🟢 HoNfigurator Online",
            $"Server manager is now online.\nServer: **{_config.HonData.ServerName}**",
            NotificationLevel.Success
        );
    }

    private async Task MessageReceivedAsync(SocketMessage message)
    {
        // Ignore bot messages
        if (message.Author.IsBot) return;

        // Check for commands (prefix: !)
        if (!message.Content.StartsWith("!")) return;

        var command = message.Content.ToLower().Split(' ')[0];
        var args = message.Content.Split(' ').Skip(1).ToArray();

        try
        {
            switch (command)
            {
                case "!status":
                    await HandleStatusCommandAsync(message);
                    break;
                case "!players":
                    await HandlePlayersCommandAsync(message);
                    break;
                case "!servers":
                    await HandleServersCommandAsync(message);
                    break;
                case "!help":
                    await HandleHelpCommandAsync(message);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Discord command: {Command}", command);
            await message.Channel.SendMessageAsync($"❌ Error processing command: {ex.Message}");
        }
    }

    private async Task HandleStatusCommandAsync(SocketMessage message)
    {
        var embed = new EmbedBuilder()
            .WithTitle("📊 Server Status")
            .WithColor(Color.Blue)
            .WithDescription($"**{_config.HonData.ServerName}**")
            .AddField("Location", _config.HonData.Location, true)
            .AddField("Status", IsConnected ? "🟢 Online" : "🔴 Offline", true)
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter("HoNfigurator .NET");

        await message.Channel.SendMessageAsync(embed: embed.Build());
    }

    private async Task HandlePlayersCommandAsync(SocketMessage message)
    {
        // This would need access to GameServerManager - for now show placeholder
        var embed = new EmbedBuilder()
            .WithTitle("👥 Online Players")
            .WithColor(Color.Green)
            .WithDescription("Use the web dashboard for detailed player information.")
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter("HoNfigurator .NET");

        await message.Channel.SendMessageAsync(embed: embed.Build());
    }

    private async Task HandleServersCommandAsync(SocketMessage message)
    {
        var embed = new EmbedBuilder()
            .WithTitle("🎮 Game Servers")
            .WithColor(Color.Purple)
            .WithDescription($"**{_config.HonData.ServerName}**\nTotal Servers: {_config.HonData.TotalServers}")
            .AddField("Game Port Range", $"{_config.HonData.StartingGamePort}+", true)
            .AddField("API Port", _config.HonData.ApiPort.ToString(), true)
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter("HoNfigurator .NET");

        await message.Channel.SendMessageAsync(embed: embed.Build());
    }

    private async Task HandleHelpCommandAsync(SocketMessage message)
    {
        var embed = new EmbedBuilder()
            .WithTitle("📖 HoNfigurator Commands")
            .WithColor(Color.Gold)
            .WithDescription("Available commands:")
            .AddField("!status", "Show server status", false)
            .AddField("!players", "Show online players", false)
            .AddField("!servers", "Show game server info", false)
            .AddField("!help", "Show this help message", false)
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter("HoNfigurator .NET");

        await message.Channel.SendMessageAsync(embed: embed.Build());
    }

    // Notification methods

    public async Task SendServerNotificationAsync(string title, string message, NotificationLevel level = NotificationLevel.Info)
    {
        if (!IsConnected || _notificationChannelId == 0) return;

        var color = level switch
        {
            NotificationLevel.Info => Color.Blue,
            NotificationLevel.Warning => Color.Orange,
            NotificationLevel.Error => Color.Red,
            NotificationLevel.Success => Color.Green,
            _ => Color.Default
        };

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(message)
            .WithColor(color)
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter("HoNfigurator .NET")
            .Build();

        try
        {
            var channel = _client.GetChannel(_notificationChannelId) as IMessageChannel;
            if (channel != null)
            {
                await channel.SendMessageAsync(embed: embed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Discord notification");
        }
    }

    public async Task SendMatchStartedAsync(int serverId, string serverName, List<string> players)
    {
        if (!IsConnected || _notificationChannelId == 0) return;

        var playerList = players.Count > 0 
            ? string.Join("\n", players.Select(p => $"• {p}"))
            : "No players";

        var embed = new EmbedBuilder()
            .WithTitle("🎮 Match Started")
            .WithColor(Color.Green)
            .WithDescription($"A new match has started on **Server #{serverId}**")
            .AddField("Server", serverName, true)
            .AddField("Players", players.Count.ToString(), true)
            .AddField("Player List", playerList.Length > 1024 ? playerList[..1020] + "..." : playerList)
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter("HoNfigurator .NET")
            .Build();

        try
        {
            var channel = _client.GetChannel(_notificationChannelId) as IMessageChannel;
            if (channel != null)
            {
                await channel.SendMessageAsync(embed: embed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send match started notification");
        }
    }

    public async Task SendMatchEndedAsync(int serverId, string serverName, int duration, string? winner = null)
    {
        if (!IsConnected || _notificationChannelId == 0) return;

        var durationStr = TimeSpan.FromSeconds(duration).ToString(@"hh\:mm\:ss");

        var embedBuilder = new EmbedBuilder()
            .WithTitle("🏁 Match Ended")
            .WithColor(Color.Orange)
            .WithDescription($"Match ended on **Server #{serverId}**")
            .AddField("Server", serverName, true)
            .AddField("Duration", durationStr, true)
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter("HoNfigurator .NET");

        if (!string.IsNullOrEmpty(winner))
        {
            embedBuilder.AddField("Winner", winner, true);
        }

        try
        {
            var channel = _client.GetChannel(_notificationChannelId) as IMessageChannel;
            if (channel != null)
            {
                await channel.SendMessageAsync(embed: embedBuilder.Build());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send match ended notification");
        }
    }

    public async Task SendPlayerJoinedAsync(int serverId, string serverName, string playerName)
    {
        if (!IsConnected || _notificationChannelId == 0) return;
        if (!(_config.ApplicationData?.Discord?.NotifyPlayerJoinLeave ?? false)) return;

        var embed = new EmbedBuilder()
            .WithTitle("👤 Player Joined")
            .WithColor(Color.Teal)
            .WithDescription($"**{playerName}** joined Server #{serverId}")
            .AddField("Server", serverName, true)
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter("HoNfigurator .NET")
            .Build();

        try
        {
            var channel = _client.GetChannel(_notificationChannelId) as IMessageChannel;
            if (channel != null)
            {
                await channel.SendMessageAsync(embed: embed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send player joined notification");
        }
    }

    public async Task SendPlayerLeftAsync(int serverId, string serverName, string playerName)
    {
        if (!IsConnected || _notificationChannelId == 0) return;
        if (!(_config.ApplicationData?.Discord?.NotifyPlayerJoinLeave ?? false)) return;

        var embed = new EmbedBuilder()
            .WithTitle("👤 Player Left")
            .WithColor(Color.LightGrey)
            .WithDescription($"**{playerName}** left Server #{serverId}")
            .AddField("Server", serverName, true)
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter("HoNfigurator .NET")
            .Build();

        try
        {
            var channel = _client.GetChannel(_notificationChannelId) as IMessageChannel;
            if (channel != null)
            {
                await channel.SendMessageAsync(embed: embed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send player left notification");
        }
    }

    public async Task SendAlertAsync(string title, string message)
    {
        await SendServerNotificationAsync($"⚠️ {title}", message, NotificationLevel.Warning);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        StopAsync().GetAwaiter().GetResult();
        _client?.Dispose();
    }
}
