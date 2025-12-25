using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace HoNfigurator.Core.Services;

/// <summary>
/// CowMaster service for Linux fork-based server spawning
/// Preloads resources and enables instant server startup via process forking
/// </summary>
public class CowMasterService
{
    private readonly ILogger<CowMasterService> _logger;
    private Process? _process;
    private int _port = 11235;
    private bool _enabled;
    private string _name = "cowmaster";

    public bool IsEnabled => _enabled;
    public bool IsRunning => _process != null && !_process.HasExited;
    public int Port => _port;
    public string Name => _name;
    public int? ProcessId => _process?.Id;

    public CowMasterService(ILogger<CowMasterService> logger)
    {
        _logger = logger;
    }

    public void Configure(int port, string serverName)
    {
        _port = port;
        _name = $"{serverName}-cowmaster";
    }

    /// <summary>
    /// Start the CowMaster process (Linux only)
    /// The CowMaster preloads game resources and can fork new servers instantly
    /// </summary>
    public async Task<bool> StartAsync(string honPath, Dictionary<string, string> cmdLineArgs)
    {
        if (!OperatingSystem.IsLinux())
        {
            _logger.LogWarning("CowMaster is only supported on Linux");
            return false;
        }

        if (IsRunning)
        {
            _logger.LogWarning("CowMaster is already running");
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(honPath, "hon_server-x86_64"),
                WorkingDirectory = honPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var args = new List<string>
            {
                "-dedicated",
                "-cowmaster",
                $"-svr_ip 0.0.0.0",
                $"-svr_port {_port}",
                $"-svr_name {_name}"
            };

            foreach (var arg in cmdLineArgs)
            {
                args.Add($"-{arg.Key} {arg.Value}");
            }

            startInfo.Arguments = string.Join(" ", args);

            _logger.LogInformation("Starting CowMaster: {FileName} {Arguments}", startInfo.FileName, startInfo.Arguments);

            _process = new Process { StartInfo = startInfo };
            _process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    _logger.LogDebug("[CowMaster] {Data}", e.Data);
            };
            _process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    _logger.LogWarning("[CowMaster Error] {Data}", e.Data);
            };

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            // Wait a bit for process to start
            await Task.Delay(1000);

            if (_process.HasExited)
            {
                _logger.LogError("CowMaster exited immediately with code {ExitCode}", _process.ExitCode);
                return false;
            }

            _enabled = true;
            _logger.LogInformation("CowMaster started with PID {PID}", _process.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start CowMaster");
            return false;
        }
    }

    /// <summary>
    /// Fork a new server from the CowMaster
    /// </summary>
    public async Task<int?> ForkServerAsync(int serverId, int gamePort, int voicePort, int proxyPort)
    {
        if (!IsRunning)
        {
            _logger.LogWarning("Cannot fork: CowMaster is not running");
            return null;
        }

        // Send fork command via TCP to CowMaster port
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            await client.ConnectAsync("127.0.0.1", _port);
            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            using var reader = new StreamReader(stream);

            var forkCmd = $"fork {serverId} {gamePort} {voicePort} {proxyPort}";
            await writer.WriteLineAsync(forkCmd);

            var response = await reader.ReadLineAsync();
            if (response?.StartsWith("OK ") == true)
            {
                if (int.TryParse(response[3..], out var pid))
                {
                    _logger.LogInformation("Forked server {ServerId} with PID {PID}", serverId, pid);
                    return pid;
                }
            }

            _logger.LogWarning("Fork failed: {Response}", response);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fork server {ServerId}", serverId);
            return null;
        }
    }

    /// <summary>
    /// Stop the CowMaster process
    /// </summary>
    public void Stop()
    {
        if (_process == null) return;

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
            _logger.LogInformation("CowMaster stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping CowMaster");
        }
        finally
        {
            _process.Dispose();
            _process = null;
            _enabled = false;
        }
    }

    public CowMasterStatus GetStatus()
    {
        return new CowMasterStatus
        {
            Name = _name,
            Port = _port,
            IsEnabled = _enabled,
            IsRunning = IsRunning,
            ProcessId = ProcessId
        };
    }
}

public class CowMasterStatus
{
    public string Name { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsRunning { get; set; }
    public int? ProcessId { get; set; }
}
