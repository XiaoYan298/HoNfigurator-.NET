using System.Text.Json;
using System.Text.RegularExpressions;
using HoNfigurator.Core.Models;
using HoNfigurator.Core.Services;

namespace HoNfigurator.Api.Setup;

/// <summary>
/// Interactive setup wizard for first-time configuration
/// Based on Python HoNfigurator-Central setup flow
/// </summary>
public class SetupWizard
{
    private readonly IConfigurationService _configService;
    private readonly HoNConfiguration _config;
    
    // Allowed regions (same as Python version)
    private static readonly string[] ALLOWED_REGIONS = { "AU", "BR", "EU", "RU", "SEA", "TH", "USE", "USW", "NEWERTH", "TEST" };

    public SetupWizard(IConfigurationService configService, HoNConfiguration config)
    {
        _configService = configService;
        _config = config;
    }

    /// <summary>
    /// Check if setup is needed
    /// </summary>
    public bool IsSetupRequired()
    {
        // Setup required if login or password is empty
        return string.IsNullOrEmpty(_config.HonData?.Login) ||
               string.IsNullOrEmpty(_config.HonData?.Password) ||
               string.IsNullOrEmpty(_config.HonData?.HonInstallDirectory);
    }

    /// <summary>
    /// Run the interactive setup wizard (based on Python HoNfigurator-Central)
    /// </summary>
    public async Task RunAsync()
    {
        SetupConsoleWindow();
        Console.Clear();
        PrintBanner();

        // ===============================================================
        // Terms and Conditions (like Python version)
        // ===============================================================
        if (!await ShowTermsAndConditionsAsync())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nYou must agree to the terms and conditions to use HoNfigurator.");
            Console.WriteLine("If there are any questions, you may reach out on Discord.");
            Console.ResetColor();
            Console.WriteLine("\nPress ENTER to exit.");
            Console.ReadLine();
            Environment.Exit(0);
        }

        // ===============================================================
        // Discord Owner ID (like Python version)
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  Discord Authentication");
        Console.WriteLine("===============================================================");
        Console.ResetColor();

        string discordId = await GetDiscordIdAsync();
        _config.ApplicationData ??= new ApplicationData();
        _config.ApplicationData.Discord ??= new DiscordSettings();
        _config.ApplicationData.Discord.OwnerId = discordId;

        // ===============================================================
        // Basic or Advanced Setup (like Python version)
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  Setup Mode");
        Console.WriteLine("===============================================================");
        Console.ResetColor();

        bool useDefaults = await AskUseDefaultsAsync();

        if (useDefaults)
        {
            Console.WriteLine("\nUsing default settings. Only required fields will be asked.");
            await RunBasicSetupAsync(discordId);
        }
        else
        {
            Console.WriteLine("\nAdvanced setup selected. Please provide values for each setting.");
            Console.WriteLine("Press Enter to keep the default value shown in [brackets].\n");
            await RunAdvancedSetupAsync(discordId);
        }

        // ===============================================================
        // Configuration Summary
        // ===============================================================
        PrintConfigurationSummary();

        // ===============================================================
        // Save Configuration
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("\nSave configuration? (y/n): ");
        Console.ResetColor();
        var save = Console.ReadLine()?.Trim().ToLower();

        if (save == "y" || save == "yes")
        {
            await _configService.SaveConfigurationAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[OK] Configuration saved successfully!");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n[!] Configuration not saved. You can configure later via the dashboard.");
            Console.ResetColor();
        }

        Console.WriteLine("\nStarting HoNfigurator...\n");
        await Task.Delay(1500);
    }

    /// <summary>
    /// Show Terms and Conditions (like Python version)
    /// </summary>
    private async Task<bool> ShowTermsAndConditionsAsync()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n+==============================================================+");
        Console.WriteLine("|           HoNfigurator .NET - Terms and Conditions           |");
        Console.WriteLine("+==============================================================+");
        Console.ResetColor();

        Console.WriteLine(@"
Welcome to HoNfigurator. By using our software, you agree to these terms:

1. To ensure the legitimacy and effective administration of game servers,
   server administrators are required to authenticate using their Discord account.

2. You may receive alerts or notifications via Discord from the HoNfigurator bot
   regarding the status of your game servers.

3. The hosting of dedicated servers through HoNfigurator requires the use of 
   HoN server binaries. Users acknowledge that these binaries are not owned 
   or maintained by the author of HoNfigurator.

4. In order to monitor server performance and maintain game integrity, 
   the following diagnostic data will be collected:
   - This server's public IP address.
   - Server administrator's Discord ID.
   - Game server logs, including in-game events and chat logs.
   - Player account names and public IP addresses.
   This data is essential for the effective operation of the server.

5. Game replays will be stored on the server and can be requested by players 
   in-game. Server administrators may manage these replays using the provided 
   HoNfigurator settings. We recommend retaining replays for a minimum of 
   30-60 days for player review and quality assurance purposes.

In summary, by using HoNfigurator, users agree to:
   - Properly manage and administer their game server.
   - Ensure the privacy and security of collected data.
   - Retain game replays for a minimum of 30 days (if practical).
   - Not tamper with, or modify the game state in any way that may 
     negatively affect the outcome of a match in progress.
");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Do you agree to these terms and conditions? (y/n): ");
        Console.ResetColor();

        var response = Console.ReadLine()?.Trim().ToLower();
        return response == "y" || response == "yes";
    }

    /// <summary>
    /// Get Discord ID from user (like Python version)
    /// </summary>
    private async Task<string> GetDiscordIdAsync()
    {
        Console.WriteLine(@"
To ensure server legitimacy, please provide your Discord User ID.
This is used for authentication and receiving server notifications.

How to find your Discord ID:
  1. Enable Developer Mode in Discord Settings > Advanced
  2. Right-click your username and select 'Copy User ID'
  
43 second guide: https://www.youtube.com/watch?v=ZPROrf4Fe3Q
");

        while (true)
        {
            Console.Write("Please provide your Discord User ID: ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Discord ID is required.");
                Console.ResetColor();
                continue;
            }

            // Validate: must be numeric and at least 10 digits
            if (long.TryParse(input, out var discordIdNum) && input.Length >= 10)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  [OK] Discord ID: {input}");
                Console.ResetColor();
                return input;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid Discord ID. Must be a number with at least 10 digits.");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Ask if user wants to use defaults (like Python version)
    /// </summary>
    private async Task<bool> AskUseDefaultsAsync()
    {
        Console.WriteLine(@"
Would you like to use mostly defaults or complete advanced setup?

  [y] - Use default settings (recommended for most users)
        Only required fields (login, password, install path) will be asked.
        
  [n] - Advanced setup
        Configure all settings manually.
");

        while (true)
        {
            Console.Write("Use default settings? (y/n): ");
            var input = Console.ReadLine()?.Trim().ToLower();

            if (input == "y" || input == "yes")
                return true;
            if (input == "n" || input == "no")
                return false;

            Console.WriteLine("Please enter 'y' for defaults or 'n' for advanced.");
        }
    }

    /// <summary>
    /// Run basic setup with defaults (like Python version - basic mode)
    /// </summary>
    private async Task RunBasicSetupAsync(string discordId)
    {
        // ===============================================================
        // Required: HoN Installation Path
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  HoN Installation Path (Required)");
        Console.WriteLine("===============================================================");
        Console.ResetColor();

        Console.Write($"HoN Install Directory [{_config.HonData.HonInstallDirectory}]: ");
        var installDir = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(installDir))
        {
            _config.HonData.HonInstallDirectory = installDir;
        }
        ValidateHonInstallation();

        // ===============================================================
        // Required: Server Account Credentials
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  Server Account Credentials (Required)");
        Console.WriteLine("===============================================================");
        Console.ResetColor();
        Console.WriteLine("Enter your Kongor/HoN server account credentials.");
        Console.WriteLine("(This is NOT your player account - it is a dedicated server account)\n");

        // Login
        while (string.IsNullOrEmpty(_config.HonData.Login))
        {
            Console.Write("Server Login (HINT: HoN Username): ");
            var login = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(login))
            {
                _config.HonData.Login = login;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Login is required.");
                Console.ResetColor();
            }
        }

        // Password
        while (string.IsNullOrEmpty(_config.HonData.Password))
        {
            Console.Write("Server Password (HINT: HoN Password): ");
            var password = ReadPassword();
            Console.WriteLine();
            if (!string.IsNullOrEmpty(password))
            {
                _config.HonData.Password = password;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Password is required.");
                Console.ResetColor();
            }
        }

        // ===============================================================
        // Required: Server Location
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  Server Location (Required)");
        Console.WriteLine("===============================================================");
        Console.ResetColor();
        Console.WriteLine($"Allowed regions: {string.Join(", ", ALLOWED_REGIONS)}\n");

        while (string.IsNullOrEmpty(_config.HonData.Location) || !ALLOWED_REGIONS.Contains(_config.HonData.Location.ToUpper()))
        {
            Console.Write($"Server Location [{_config.HonData.Location}]: ");
            var location = Console.ReadLine()?.Trim().ToUpper();
            if (!string.IsNullOrEmpty(location))
            {
                if (ALLOWED_REGIONS.Contains(location))
                {
                    _config.HonData.Location = location;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Invalid region. Must be one of: {string.Join(", ", ALLOWED_REGIONS)}");
                    Console.ResetColor();
                }
            }
            else if (!string.IsNullOrEmpty(_config.HonData.Location) && ALLOWED_REGIONS.Contains(_config.HonData.Location.ToUpper()))
            {
                break; // Use existing valid value
            }
        }

        // Auto-generate server name (like Python version)
        _config.HonData.ServerName = await GenerateServerNameAsync(discordId);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n  [Auto] Server Name: {_config.HonData.ServerName}");
        Console.ResetColor();

        // Apply defaults
        ApplyDefaultValues();
    }

    /// <summary>
    /// Run advanced setup (like Python version - advanced mode)
    /// </summary>
    private async Task RunAdvancedSetupAsync(string discordId)
    {
        // ===============================================================
        // Step 1: HoN Installation Directories
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  Step 1: HoN Installation Paths");
        Console.WriteLine("===============================================================");
        Console.ResetColor();

        Console.Write($"\nHoN Install Directory [{_config.HonData.HonInstallDirectory}]: ");
        var installDir = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(installDir))
        {
            _config.HonData.HonInstallDirectory = installDir;
        }
        ValidateHonInstallation();

        Console.Write($"HoN Home Directory (for logs/replays) [{_config.HonData.HonHomeDirectory}]: ");
        var homeDir = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(homeDir))
        {
            _config.HonData.HonHomeDirectory = homeDir;
        }

        Console.Write($"HoN Logs Directory [{_config.HonData.HonLogsDirectory}]: ");
        var logsDir = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(logsDir))
        {
            _config.HonData.HonLogsDirectory = logsDir;
        }
        else if (string.IsNullOrEmpty(_config.HonData.HonLogsDirectory) && !string.IsNullOrEmpty(_config.HonData.HonHomeDirectory))
        {
            // Default: use HoN Home + logs
            _config.HonData.HonLogsDirectory = Path.Combine(_config.HonData.HonHomeDirectory, "logs");
        }

        // ===============================================================
        // Step 2: Server Account Credentials
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  Step 2: Server Account Credentials");
        Console.WriteLine("===============================================================");
        Console.ResetColor();
        Console.WriteLine("Enter your Kongor/HoN server account credentials.");
        Console.WriteLine("(This is NOT your player account - it is a dedicated server account)\n");

        Console.Write($"Server Login (HINT: HoN Username) [{_config.HonData.Login}]: ");
        var login = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(login))
        {
            _config.HonData.Login = login;
        }

        Console.Write("Server Password (HINT: HoN Password, press Enter to keep existing): ");
        var password = ReadPassword();
        Console.WriteLine();
        if (!string.IsNullOrEmpty(password))
        {
            _config.HonData.Password = password;
        }

        // ===============================================================
        // Step 3: Server Identity
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  Step 3: Server Identity");
        Console.WriteLine("===============================================================");
        Console.ResetColor();
        Console.WriteLine($"Allowed regions: {string.Join(", ", ALLOWED_REGIONS)}\n");

        // Location first (needed for server name)
        while (true)
        {
            Console.Write($"Server Location [{_config.HonData.Location}]: ");
            var location = Console.ReadLine()?.Trim().ToUpper();
            if (string.IsNullOrEmpty(location) && !string.IsNullOrEmpty(_config.HonData.Location))
            {
                break; // Keep existing
            }
            if (!string.IsNullOrEmpty(location) && ALLOWED_REGIONS.Contains(location))
            {
                _config.HonData.Location = location;
                break;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Invalid region. Must be one of: {string.Join(", ", ALLOWED_REGIONS)}");
            Console.ResetColor();
        }

        // Auto-generate server name suggestion (like Python version)
        var suggestedName = await GenerateServerNameAsync(discordId);
        Console.Write($"Server Name [{suggestedName}]: ");
        var serverName = Console.ReadLine()?.Trim();
        _config.HonData.ServerName = string.IsNullOrEmpty(serverName) ? suggestedName : serverName;
        // Limit to 20 chars (like Python version)
        if (_config.HonData.ServerName.Length > 20)
        {
            _config.HonData.ServerName = _config.HonData.ServerName[..20];
        }

        Console.Write($"Server Priority (HIGH/NORMAL/LOW) [{_config.HonData.Priority}]: ");
        var priority = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(priority))
        {
            _config.HonData.Priority = priority.ToUpper();
        }

        // ===============================================================
        // Step 4: Server Instances
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  Step 4: Server Instances");
        Console.WriteLine("===============================================================");
        Console.ResetColor();

        var maxAllowed = Environment.ProcessorCount / 2;
        Console.WriteLine($"(Recommended max based on CPU: {maxAllowed} servers)\n");

        Console.Write($"Total Number of Game Servers [{_config.HonData.TotalServers}]: ");
        var totalServersStr = Console.ReadLine()?.Trim();
        if (int.TryParse(totalServersStr, out var totalServers) && totalServers > 0)
        {
            // Validate like Python version
            if (totalServers > maxAllowed)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  [!] Reduced to {maxAllowed} (based on CPU cores)");
                Console.ResetColor();
                totalServers = maxAllowed;
            }
            _config.HonData.TotalServers = totalServers;
        }

        Console.Write($"Servers per CPU Core [{_config.HonData.TotalPerCore}]: ");
        var perCoreStr = Console.ReadLine()?.Trim();
        if (float.TryParse(perCoreStr, out var perCore) && perCore > 0)
        {
            _config.HonData.TotalPerCore = perCore;
        }

        Console.Write($"Max Servers to Start at Once [{_config.HonData.MaxStartAtOnce}]: ");
        var maxStartStr = Console.ReadLine()?.Trim();
        if (int.TryParse(maxStartStr, out var maxStart) && maxStart > 0)
        {
            _config.HonData.MaxStartAtOnce = maxStart;
        }

        Console.Write($"Startup Timeout (seconds) [{_config.HonData.StartupTimeout}]: ");
        var timeoutStr = Console.ReadLine()?.Trim();
        if (int.TryParse(timeoutStr, out var timeout) && timeout > 0)
        {
            _config.HonData.StartupTimeout = timeout;
        }

        // ===============================================================
        // Step 5: Network Ports
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  Step 5: Network Ports");
        Console.WriteLine("===============================================================");
        Console.ResetColor();
        Console.WriteLine("(Game ports must start from 10001 onwards)\n");

        Console.Write($"Starting Game Port [{_config.HonData.StartingGamePort}]: ");
        var gamePortStr = Console.ReadLine()?.Trim();
        if (int.TryParse(gamePortStr, out var gamePort))
        {
            // Validate like Python version
            if (gamePort < 10001)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  [!] Game port must be >= 10001, using 10001");
                Console.ResetColor();
                gamePort = 10001;
            }
            _config.HonData.StartingGamePort = gamePort;
        }

        Console.Write($"Starting Voice Port [{_config.HonData.StartingVoicePort}]: ");
        var voicePortStr = Console.ReadLine()?.Trim();
        if (int.TryParse(voicePortStr, out var voicePort))
        {
            // Validate like Python version
            if (voicePort < 10061)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  [!] Voice port must be >= 10061, using 10061");
                Console.ResetColor();
                voicePort = 10061;
            }
            _config.HonData.StartingVoicePort = voicePort;
        }

        Console.Write($"Manager Port [{_config.HonData.ManagerPort}]: ");
        var managerPortStr = Console.ReadLine()?.Trim();
        if (int.TryParse(managerPortStr, out var managerPort) && managerPort > 0)
        {
            _config.HonData.ManagerPort = managerPort;
        }

        Console.Write($"API Dashboard Port [{_config.HonData.ApiPort}]: ");
        var apiPortStr = Console.ReadLine()?.Trim();
        if (int.TryParse(apiPortStr, out var apiPort) && apiPort > 0)
        {
            _config.HonData.ApiPort = apiPort;
        }

        // ===============================================================
        // Step 6: Server Connection
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  Step 6: Server Connection");
        Console.WriteLine("===============================================================");
        Console.ResetColor();
        Console.WriteLine("Configure the master server, chat server, and patch server addresses.");
        Console.WriteLine("Default: kongor.net servers\n");

        Console.Write($"Master Server Address [{_config.HonData.MasterServer}]: ");
        var masterServer = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(masterServer))
        {
            _config.HonData.MasterServer = masterServer;
        }

        Console.Write($"Chat Server Address [{_config.HonData.ChatServer}]: ");
        var chatServer = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(chatServer))
        {
            _config.HonData.ChatServer = chatServer;
        }

        Console.Write($"Patch Server Address [{_config.HonData.PatchServer}]: ");
        var patchServer = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(patchServer))
        {
            _config.HonData.PatchServer = patchServer;
        }

        // ===============================================================
        // Step 7: Server Options
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  Step 7: Server Options");
        Console.WriteLine("===============================================================");
        Console.ResetColor();

        _config.HonData.StartOnLaunch = AskYesNo("Start servers on launch?", _config.HonData.StartOnLaunch);
        _config.HonData.EnableBotMatch = AskYesNo("Enable bot matches?", _config.HonData.EnableBotMatch);
        _config.HonData.NoConsole = AskYesNo("Hide server program window?", _config.HonData.NoConsole);
        _config.HonData.RestartBetweenGames = AskYesNo("Restart servers between games?", _config.HonData.RestartBetweenGames);

        // ===============================================================
        // Step 8: Proxy Settings (Windows only, like Python version)
        // ===============================================================
        if (OperatingSystem.IsWindows())
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n===============================================================");
            Console.WriteLine("  Step 8: Proxy Settings (Windows)");
            Console.WriteLine("===============================================================");
            Console.ResetColor();
            Console.WriteLine("Proxy mode adds 10000 to game ports for external connections.\n");

            _config.HonData.EnableProxy = AskYesNo("Enable Proxy Mode?", _config.HonData.EnableProxy);
        }

        // ===============================================================
        // Step 9: Advanced Settings
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  Step 9: Advanced Settings");
        Console.WriteLine("===============================================================");
        Console.ResetColor();

        // Cowmaster is Linux only (like Python version)
        if (!OperatingSystem.IsWindows())
        {
            _config.HonData.UseCowmaster = AskYesNo("Use Cowmaster (Linux only)?", _config.HonData.UseCowmaster);
        }
        else
        {
            _config.HonData.UseCowmaster = false; // Forced off on Windows
        }

        Console.Write($"Server IP (leave empty for auto-detect) [{_config.HonData.ServerIp}]: ");
        var serverIp = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(serverIp))
        {
            _config.HonData.ServerIp = serverIp;
        }

        Console.Write($"Local IP (leave empty for auto-detect) [{_config.HonData.LocalIp}]: ");
        var localIp = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(localIp))
        {
            _config.HonData.LocalIp = localIp;
        }

        _config.HonData.BetaMode = AskYesNo("Enable Beta Mode?", _config.HonData.BetaMode);
    }

    /// <summary>
    /// Generate server name from Discord username + location (like Python version)
    /// </summary>
    private async Task<string> GenerateServerNameAsync(string discordId)
    {
        // Try to get Discord username (simplified - in Python it calls API)
        // For now, use format: "Server_{location}_{random}"
        var location = _config.HonData.Location ?? "NEWERTH";
        var random = new Random().Next(100, 999);
        var name = $"Server_{location}_{random}";
        
        // Limit to 20 characters (like Python version)
        if (name.Length > 20)
        {
            name = name[..20];
        }
        
        return name;
    }

    /// <summary>
    /// Apply default values (like Python version defaults)
    /// </summary>
    private void ApplyDefaultValues()
    {
        // Default values from Python version
        _config.HonData.MasterServer ??= "api.kongor.net";
        _config.HonData.PatchServer ??= "api.kongor.net";
        _config.HonData.ChatServer ??= "chat.kongor.net";
        _config.HonData.Priority ??= "HIGH";
        _config.HonData.TotalServers = _config.HonData.TotalServers > 0 ? _config.HonData.TotalServers : Environment.ProcessorCount / 2;
        _config.HonData.TotalPerCore = _config.HonData.TotalPerCore > 0 ? _config.HonData.TotalPerCore : 1.0f;
        _config.HonData.MaxStartAtOnce = _config.HonData.MaxStartAtOnce > 0 ? _config.HonData.MaxStartAtOnce : 5;
        _config.HonData.StartingGamePort = _config.HonData.StartingGamePort > 0 ? _config.HonData.StartingGamePort : 10001;
        _config.HonData.StartingVoicePort = _config.HonData.StartingVoicePort > 0 ? _config.HonData.StartingVoicePort : 10061;
        _config.HonData.ManagerPort = _config.HonData.ManagerPort > 0 ? _config.HonData.ManagerPort : 1134;
        _config.HonData.StartupTimeout = _config.HonData.StartupTimeout > 0 ? _config.HonData.StartupTimeout : 180;
        _config.HonData.ApiPort = _config.HonData.ApiPort > 0 ? _config.HonData.ApiPort : 5000;
        _config.HonData.EnableProxy = OperatingSystem.IsWindows(); // Default true on Windows
        _config.HonData.EnableBotMatch = true;
        _config.HonData.StartOnLaunch = true;
    }

    /// <summary>
    /// Validate HoN installation directory
    /// </summary>
    private void ValidateHonInstallation()
    {
        if (string.IsNullOrEmpty(_config.HonData?.HonInstallDirectory))
            return;

        var honExe = Path.Combine(_config.HonData.HonInstallDirectory, "hon_x64.exe");
        if (!File.Exists(honExe))
        {
            honExe = Path.Combine(_config.HonData.HonInstallDirectory, "hon.exe");
        }

        if (File.Exists(honExe))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [OK] Found: {honExe}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  [X] Warning: hon.exe / hon_x64.exe not found in this directory");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Ask a yes/no question
    /// </summary>
    private bool AskYesNo(string question, bool defaultValue)
    {
        Console.Write($"{question} (y/n) [{(defaultValue ? "y" : "n")}]: ");
        var input = Console.ReadLine()?.Trim().ToLower();
        
        if (input == "y" || input == "yes")
            return true;
        if (input == "n" || input == "no")
            return false;
        
        return defaultValue;
    }

    /// <summary>
    /// Print configuration summary
    /// </summary>
    private void PrintConfigurationSummary()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n+==============================================================+");
        Console.WriteLine("|                    Configuration Summary                      |");
        Console.WriteLine("+==============================================================+");
        Console.ResetColor();

        // Like Python version: print all config items
        Console.WriteLine("\n Configuration Overview:");
        Console.WriteLine($"  hon_install_directory: {_config.HonData.HonInstallDirectory}");
        Console.WriteLine($"  hon_home_directory: {_config.HonData.HonHomeDirectory}");
        Console.WriteLine($"  svr_masterServer: {_config.HonData.MasterServer}");
        Console.WriteLine($"  svr_chatServer: {_config.HonData.ChatServer}");
        Console.WriteLine($"  svr_patchServer: {_config.HonData.PatchServer}");
        Console.WriteLine($"  svr_login: {_config.HonData.Login}");
        Console.WriteLine($"  svr_password: ***********");
        Console.WriteLine($"  svr_name: {_config.HonData.ServerName}");
        Console.WriteLine($"  svr_location: {_config.HonData.Location}");
        Console.WriteLine($"  svr_priority: {_config.HonData.Priority}");
        Console.WriteLine($"  svr_total: {_config.HonData.TotalServers}");
        Console.WriteLine($"  svr_total_per_core: {_config.HonData.TotalPerCore}");
        Console.WriteLine($"  svr_max_start_at_once: {_config.HonData.MaxStartAtOnce}");
        Console.WriteLine($"  svr_starting_gamePort: {_config.HonData.StartingGamePort}");
        Console.WriteLine($"  svr_starting_voicePort: {_config.HonData.StartingVoicePort}");
        Console.WriteLine($"  svr_managerPort: {_config.HonData.ManagerPort}");
        Console.WriteLine($"  svr_api_port: {_config.HonData.ApiPort}");
        Console.WriteLine($"  svr_startup_timeout: {_config.HonData.StartupTimeout}");
        Console.WriteLine($"  man_enableProxy: {_config.HonData.EnableProxy}");
        Console.WriteLine($"  svr_noConsole: {_config.HonData.NoConsole}");
        Console.WriteLine($"  svr_enableBotMatch: {_config.HonData.EnableBotMatch}");
        Console.WriteLine($"  svr_start_on_launch: {_config.HonData.StartOnLaunch}");
        Console.WriteLine($"  svr_restart_between_games: {_config.HonData.RestartBetweenGames}");
        Console.WriteLine($"  man_use_cowmaster: {_config.HonData.UseCowmaster}");
        Console.WriteLine($"  svr_beta_mode: {_config.HonData.BetaMode}");
        
        if (_config.ApplicationData?.Discord != null)
        {
            Console.WriteLine($"  discord_owner_id: {_config.ApplicationData.Discord.OwnerId}");
        }
    }

    private void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(@"
  _   _       _   _ __ _                       _             
 | | | | ___ | \ | |  _(_) __ _ _   _ _ __ __ _| |_ ___  _ __ 
 | |_| |/ _ \|  \| | |_| / _` | | | | '__/ _` | __/ _ \| '__|
 |  _  | (_) | |\  |  _| | (_| | |_| | | | (_| | || (_) | |   
 |_| |_|\___/|_| \_|_| |_|\__, |\__,_|_|  \__,_|\__\___/|_|   
                         |___/                .NET Edition
");
        Console.ResetColor();
    }

    private void SetupConsoleWindow()
    {
        try
        {
            Console.Title = "HoNfigurator .NET - Setup Wizard";

            if (OperatingSystem.IsWindows())
            {
                Console.WindowWidth = Math.Min(120, Console.LargestWindowWidth);
                Console.WindowHeight = Math.Min(40, Console.LargestWindowHeight);
                Console.BufferWidth = Math.Max(Console.WindowWidth, 120);
                Console.BufferHeight = 1000;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
            }

            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
        }
        catch
        {
            // Ignore console setup errors
        }
    }

    private string ReadPassword()
    {
        var password = string.Empty;
        ConsoleKey key;

        do
        {
            var keyInfo = Console.ReadKey(intercept: true);
            key = keyInfo.Key;

            if (key == ConsoleKey.Backspace && password.Length > 0)
            {
                Console.Write("\b \b");
                password = password[..^1];
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                Console.Write("*");
                password += keyInfo.KeyChar;
            }
        } while (key != ConsoleKey.Enter);

        return password;
    }
}
