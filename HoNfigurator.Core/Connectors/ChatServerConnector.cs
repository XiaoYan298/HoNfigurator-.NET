using System.Net.Sockets;
using System.Text;
using HoNfigurator.Core.Protocol;
using Microsoft.Extensions.Logging;

namespace HoNfigurator.Core.Connectors;

/// <summary>
/// Replay upload status codes (matches Python's enum)
/// </summary>
public enum ReplayUploadStatus : byte
{
    NotFound = 0x01,
    AlreadyUploaded = 0x02,
    InQueue = 0x03,
    Uploading = 0x04,
    HaveReplay = 0x05,
    UploadingNow = 0x06,
    UploadComplete = 0x07
}

/// <summary>
/// Manages connection to the HoN Chat Server for server registration and replay handling
/// </summary>
public interface IChatServerConnector
{
    bool IsConnected { get; }
    int? ServerId { get; }
    Task<bool> ConnectAsync(string host, int port, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task<bool> SendHandshakeAsync(int serverId, string sessionId);
    Task SendServerInfoAsync(int serverId, string username, string region, string serverName, string version, string ipAddress, int udpPingPort);
    Task SendHeartbeatAsync();
    Task SendReplayStatusUpdateAsync(int matchId, int accountId, ReplayUploadStatus status, string? downloadLink = null);
}

public class ChatServerConnector : IChatServerConnector, IDisposable
{
    private readonly ILogger<ChatServerConnector> _logger;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly ManagerChatPacketParser _parser;
    private CancellationTokenSource? _readCts;
    private CancellationTokenSource? _keepaliveCts;
    private Task? _readTask;
    private Task? _keepaliveTask;
    private bool _disposed;
    private bool _handshakeAccepted;

    public bool IsConnected => _client?.Connected ?? false;
    public int? ServerId { get; private set; }

    public event Action? OnConnected;
    public event Action? OnDisconnected;
    public event Action? OnHandshakeAccepted;
    public event Action<ReplayRequestData>? OnReplayRequest;
    public event Action? OnShutdownNotice;

    // Server info for auto-reconnect
    private string? _username;
    private string? _region;
    private string? _serverName;
    private string? _version;
    private string? _ipAddress;
    private int _udpPingPort;

    public ChatServerConnector(ILogger<ChatServerConnector> logger)
    {
        _logger = logger;
        _parser = new ManagerChatPacketParser((level, msg) =>
        {
            switch (level)
            {
                case "debug": _logger.LogDebug("{Message}", msg); break;
                case "warn": _logger.LogWarning("{Message}", msg); break;
                case "error": _logger.LogError("{Message}", msg); break;
                default: _logger.LogInformation("{Message}", msg); break;
            }
        });

        _parser.OnHandshakeAccepted += HandleHandshakeAccepted;
        _parser.OnReplayRequest += data => OnReplayRequest?.Invoke(data);
        _parser.OnShutdownNotice += HandleShutdownNotice;
        _parser.OnHeartbeatReceived += () => _logger.LogDebug("Chat server heartbeat response received");
    }

    private void HandleHandshakeAccepted()
    {
        _handshakeAccepted = true;
        _logger.LogInformation("Chat server handshake accepted, sending server info...");
        OnHandshakeAccepted?.Invoke();
    }

    private async void HandleShutdownNotice()
    {
        _logger.LogWarning("Chat server sent shutdown notice");
        OnShutdownNotice?.Invoke();
        await DisconnectAsync();
    }

    public async Task<bool> ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            _logger.LogWarning("Already connected to chat server");
            return true;
        }

        try
        {
            _client = new TcpClient();
            _client.ReceiveTimeout = 30000;
            _client.SendTimeout = 30000;
            _client.NoDelay = true;

            _logger.LogInformation("Connecting to chat server at {Host}:{Port}", host, port);
            await _client.ConnectAsync(host, port, cancellationToken);

            _stream = _client.GetStream();
            _handshakeAccepted = false;

            // Start reading packets
            _readCts = new CancellationTokenSource();
            _readTask = ReadPacketsAsync(_readCts.Token);

            _logger.LogInformation("Connected to chat server");
            OnConnected?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to chat server");
            await DisconnectAsync();
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        // Stop keepalive
        if (_keepaliveCts != null)
        {
            await _keepaliveCts.CancelAsync();
            if (_keepaliveTask != null)
            {
                try { await _keepaliveTask; } catch { }
            }
        }

        // Stop reading
        if (_readCts != null)
        {
            await _readCts.CancelAsync();
            if (_readTask != null)
            {
                try { await _readTask; } catch { }
            }
        }

        // Send termination if connected
        if (_stream != null && IsConnected)
        {
            try
            {
                await _stream.WriteAsync(new byte[] { 0x03, 0x00 });
            }
            catch { }
        }

        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
        _handshakeAccepted = false;
        ServerId = null;

        _logger.LogInformation("Disconnected from chat server");
        OnDisconnected?.Invoke();
    }

    public async Task<bool> SendHandshakeAsync(int serverId, string sessionId)
    {
        if (_stream == null || !IsConnected)
        {
            _logger.LogError("Cannot send handshake: not connected");
            return false;
        }

        try
        {
            ServerId = serverId;

            // Python: struct.pack('<H', msg_type) + struct.pack('<I', server_id) + session_id.encode('utf-8') + b'\x00' + struct.pack('<I', 70)
            // 0x1600 = handshake request
            var packetData = new List<byte>();
            
            // Packet type
            packetData.AddRange(BitConverter.GetBytes((ushort)0x1600));
            // Server ID (4 bytes)
            packetData.AddRange(BitConverter.GetBytes(serverId));
            // Session ID (null-terminated string)
            packetData.AddRange(Encoding.UTF8.GetBytes(sessionId));
            packetData.Add(0x00);
            // Protocol version (4 bytes, value 70 = 0x46)
            packetData.AddRange(BitConverter.GetBytes(70));

            // Prepend length
            var packet = new byte[2 + packetData.Count];
            BitConverter.GetBytes((ushort)packetData.Count).CopyTo(packet, 0);
            packetData.ToArray().CopyTo(packet, 2);

            await _stream.WriteAsync(packet);
            await _stream.FlushAsync();

            _logger.LogDebug(">>> [MGR|CHAT] [0x1600] Sent handshake to ChatServer - ServerID: {ServerId}", serverId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send handshake");
            return false;
        }
    }

    public async Task SendServerInfoAsync(int serverId, string username, string region, string serverName, string version, string ipAddress, int udpPingPort)
    {
        if (_stream == null || !IsConnected)
        {
            throw new InvalidOperationException("Not connected to chat server");
        }

        // Store for potential reconnect
        _username = username;
        _region = region;
        _serverName = serverName;
        _version = version;
        _ipAddress = ipAddress;
        _udpPingPort = udpPingPort;

        // Python format:
        // msg_type = 0x1602
        // packet_data += server_id (4 bytes)
        // packet_data += username + '\x00'
        // packet_data += region + '\x00'
        // packet_data += server_name + '\x00'  (format: "name 0")
        // packet_data += version + '\x00'
        // packet_data += ip_addr + '\x00'
        // packet_data += udp_ping_port (2 bytes)
        // packet_data += '\x00' (running status: 0 = running, 1 = shutting down)

        var packetData = new List<byte>();
        
        // Packet type 0x1602
        packetData.AddRange(BitConverter.GetBytes((ushort)0x1602));
        // Server ID
        packetData.AddRange(BitConverter.GetBytes(serverId));
        // Username (ensure colon suffix like Python)
        var formattedUsername = username.EndsWith(":") ? username : username + ":";
        packetData.AddRange(Encoding.UTF8.GetBytes(formattedUsername));
        packetData.Add(0x00);
        // Region
        packetData.AddRange(Encoding.UTF8.GetBytes(region));
        packetData.Add(0x00);
        // Server name (format: "name 0")
        var formattedServerName = serverName.EndsWith(" 0") ? serverName : serverName + " 0";
        packetData.AddRange(Encoding.UTF8.GetBytes(formattedServerName));
        packetData.Add(0x00);
        // Version
        packetData.AddRange(Encoding.UTF8.GetBytes(version));
        packetData.Add(0x00);
        // IP Address
        packetData.AddRange(Encoding.UTF8.GetBytes(ipAddress));
        packetData.Add(0x00);
        // UDP Ping Port (2 bytes)
        packetData.AddRange(BitConverter.GetBytes((ushort)udpPingPort));
        // Running status (0 = running)
        packetData.Add(0x00);

        // Prepend length
        var packet = new byte[2 + packetData.Count];
        BitConverter.GetBytes((ushort)packetData.Count).CopyTo(packet, 0);
        packetData.ToArray().CopyTo(packet, 2);

        await _stream.WriteAsync(packet);
        await _stream.FlushAsync();

        _logger.LogDebug(">>> [MGR|CHAT] [0x1602] Sent server info - User: {Username}, Region: {Region}, Server: {ServerName}, IP: {IpAddress}:{Port}",
            formattedUsername, region, formattedServerName, ipAddress, udpPingPort);

        // Start keepalive task after sending server info (like Python does after 0x1700 response)
        StartKeepalive();
    }

    private void StartKeepalive()
    {
        _keepaliveCts?.Cancel();
        _keepaliveCts = new CancellationTokenSource();
        _keepaliveTask = KeepaliveLoopAsync(_keepaliveCts.Token);
    }

    private async Task KeepaliveLoopAsync(CancellationToken cancellationToken)
    {
        // Python sends keepalive every 15 seconds:
        // self.writer.write(b'\x02\x00')  # length
        // self.writer.write(b'\x00*')     # packet type 0x2A00
        
        while (!cancellationToken.IsCancellationRequested && IsConnected)
        {
            try
            {
                // Wait 15 seconds (checking cancellation every second)
                for (int i = 0; i < 15; i++)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    await Task.Delay(1000, cancellationToken);
                }

                if (_stream != null && IsConnected)
                {
                    // Send keepalive: length (2 bytes) + packet type 0x2A00
                    // Python: b'\x02\x00' + b'\x00*' = [0x02, 0x00, 0x00, 0x2A]
                    // But \x00* in Python means 0x00 + ord('*') = 0x00 + 0x2A
                    // So full packet is: 0x02 0x00 0x00 0x2A
                    await _stream.WriteAsync(new byte[] { 0x02, 0x00 }, cancellationToken);
                    await _stream.FlushAsync(cancellationToken);
                    await _stream.WriteAsync(new byte[] { 0x00, 0x2A }, cancellationToken);
                    await _stream.FlushAsync(cancellationToken);

                    _logger.LogDebug(">>> [MGR|CHAT] Sent keepalive");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Keepalive failed");
                break;
            }
        }
    }

    public async Task SendHeartbeatAsync()
    {
        if (_stream == null || !IsConnected)
        {
            throw new InvalidOperationException("Not connected to chat server");
        }

        // Heartbeat is same as keepalive
        await _stream.WriteAsync(new byte[] { 0x02, 0x00, 0x00, 0x2A });
        await _stream.FlushAsync();
        _logger.LogDebug(">>> [MGR|CHAT] Sent heartbeat");
    }

    public async Task SendReplayStatusUpdateAsync(int matchId, int accountId, ReplayUploadStatus status, string? downloadLink = null)
    {
        if (_stream == null || !IsConnected)
        {
            _logger.LogError("Cannot send replay status: not connected");
            return;
        }

        try
        {
            // Python format for 0x1603:
            // packet_data = struct.pack('<H', msg_type)  # 0x1603
            // packet_data += struct.pack('<I', match_id)  # 4 bytes
            // packet_data += struct.pack('<I', account_id)  # 4 bytes
            // packet_data += struct.pack('<B', status)  # 1 byte
            // if status == UPLOAD_COMPLETE: packet_data += b'\x00'

            var packetData = new List<byte>();
            
            // Packet type 0x1603
            packetData.AddRange(BitConverter.GetBytes((ushort)0x1603));
            // Match ID (4 bytes)
            packetData.AddRange(BitConverter.GetBytes(matchId));
            // Account ID (4 bytes)
            packetData.AddRange(BitConverter.GetBytes(accountId));
            // Status (1 byte)
            packetData.Add((byte)status);
            
            // Add download link for completed/already uploaded statuses
            if (status == ReplayUploadStatus.UploadComplete || status == ReplayUploadStatus.AlreadyUploaded)
            {
                if (!string.IsNullOrEmpty(downloadLink))
                {
                    packetData.AddRange(Encoding.UTF8.GetBytes(downloadLink));
                }
                packetData.Add(0x00);
            }

            // Prepend length
            var packet = new byte[2 + packetData.Count];
            BitConverter.GetBytes((ushort)packetData.Count).CopyTo(packet, 0);
            packetData.ToArray().CopyTo(packet, 2);

            await _stream.WriteAsync(packet);
            await _stream.FlushAsync();

            _logger.LogDebug(">>> [MGR|CHAT] [0x1603] Sent replay status - Match: {MatchId}, Account: {AccountId}, Status: {Status}",
                matchId, accountId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send replay status update");
        }
    }

    private async Task ReadPacketsAsync(CancellationToken cancellationToken)
    {
        var lengthBuffer = new byte[2];

        while (!cancellationToken.IsCancellationRequested && IsConnected)
        {
            try
            {
                // Read packet length (2 bytes, little endian)
                var bytesRead = await _stream!.ReadAsync(lengthBuffer, 0, 2, cancellationToken);
                if (bytesRead < 2)
                {
                    _logger.LogWarning("Chat server connection closed (incomplete length read)");
                    break;
                }

                var packetLen = BitConverter.ToUInt16(lengthBuffer, 0);

                // Read full packet data
                var packetData = new byte[packetLen];
                var totalRead = 0;
                while (totalRead < packetLen)
                {
                    var chunk = await _stream.ReadAsync(packetData, totalRead, packetLen - totalRead, cancellationToken);
                    if (chunk == 0)
                    {
                        _logger.LogWarning("Chat server connection closed during packet read");
                        break;
                    }
                    totalRead += chunk;
                }

                if (totalRead < packetLen) break;

                // Extract packet type (first 2 bytes of data)
                var packetType = BitConverter.ToUInt16(packetData, 0);

                // Process packet (pass data after packet type)
                await _parser.HandlePacketAsync(packetType, packetData.AsMemory(2), "receiving");

                // If handshake accepted (0x1700), we need to send server info
                if (packetType == 0x1700 && _handshakeAccepted && ServerId.HasValue && _username != null)
                {
                    await SendServerInfoAsync(ServerId.Value, _username, _region ?? "US", _serverName ?? "HoNfigurator", 
                        _version ?? "4.10.1", _ipAddress ?? "0.0.0.0", _udpPingPort);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException ex) when (ex.InnerException is SocketException)
            {
                _logger.LogWarning("Chat server connection reset");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading packet from chat server");
                break;
            }
        }

        // Connection lost - notify
        if (!cancellationToken.IsCancellationRequested)
        {
            _ = Task.Run(() => OnDisconnected?.Invoke());
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _keepaliveCts?.Cancel();
        _keepaliveCts?.Dispose();
        _readCts?.Cancel();
        _readCts?.Dispose();
        _stream?.Dispose();
        _client?.Dispose();
    }
}
