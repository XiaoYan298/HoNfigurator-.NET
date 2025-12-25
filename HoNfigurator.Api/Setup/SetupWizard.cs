using System.Text.Json;
using HoNfigurator.Core.Models;
using HoNfigurator.Core.Services;

namespace HoNfigurator.Api.Setup;

/// <summary>
/// Interactive setup wizard for first-time configuration
/// </summary>
public class SetupWizard
{
    private readonly IConfigurationService _configService;
    private readonly HoNConfiguration _config;

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
    /// Run the interactive setup wizard
    /// </summary>
    public async Task RunAsync()
    {
        SetupConsoleWindow();
        Console.Clear();
        PrintBanner();
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n+==============================================================+");
        Console.WriteLine("|           HoNfigurator .NET - First Time Setup               |");
        Console.WriteLine("+==============================================================+");
        Console.ResetColor();

        Console.WriteLine("\nThis wizard will help you configure your HoN game server.");
        Console.WriteLine("Press Enter to keep the default value shown in [brackets].\n");

        // ===============================================================
        // Step 1: HoN Installation Directories
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("===============================================================");
        Console.WriteLine("  Step 1: HoN Installation Paths");
        Console.WriteLine("===============================================================");
        Console.ResetColor();
        
        Console.Write($"\nHoN Install Directory [{_config.HonData.HonInstallDirectory}]: ");
        var installDir = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(installDir))
        {
            _config.HonData.HonInstallDirectory = installDir;
        }

        // Validate HoN installation
        if (!string.IsNullOrEmpty(_config.HonData?.HonInstallDirectory))
        {
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
                Console.WriteLine("  [X] Warning: hon.exe not found in this directory");
                Console.ResetColor();
            }
        }

        Console.Write($"HoN Home Directory (for logs/replays, leave empty for default) [{_config.HonData.HonHomeDirectory}]: ");
        var homeDir = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(homeDir))
        {
            _config.HonData.HonHomeDirectory = homeDir;
        }

        Console.Write($"HoN Logs Directory (leave empty for default) [{_config.HonData.HonLogsDirectory}]: ");
        var logsDir = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(logsDir))
        {
            _config.HonData.HonLogsDirectory = logsDir;
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
        Console.WriteLine("(This is NOT your player account - it is a dedicated server account)");
        
        Console.Write($"\nServer Login [{_config.HonData.Login}]: ");
        var login = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(login))
        {
            _config.HonData.Login = login;
        }

        Console.Write("Server Password (press Enter to keep existing): ");
        var password = ReadPassword();
        if (!string.IsNullOrEmpty(password))
        {
            _config.HonData.Password = password;
        }
        Console.WriteLine();

        // ===============================================================
        // Step 3: Server Identity
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  Step 3: Server Identity");
        Console.WriteLine("===============================================================");
        Console.ResetColor();

        Console.Write($"Server Name [{_config.HonData.ServerName}]: ");
        var serverName = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(serverName))
        {
            _config.HonData.ServerName = serverName;
        }

        Console.Write($"Server Location (NEWERTH/US/EU/SEA/etc) [{_config.HonData.Location}]: ");
        var location = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(location))
        {
            _config.HonData.Location = location.ToUpper();
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

        Console.Write($"Total Number of Game Servers [{_config.HonData.TotalServers}]: ");
        var totalServersStr = Console.ReadLine()?.Trim();
        if (int.TryParse(totalServersStr, out var totalServers) && totalServers > 0)
        {
            _config.HonData.TotalServers = totalServers;
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

        Console.Write($"Starting Game Port [{_config.HonData.StartingGamePort}]: ");
        var gamePortStr = Console.ReadLine()?.Trim();
        if (int.TryParse(gamePortStr, out var gamePort) && gamePort > 0)
        {
            _config.HonData.StartingGamePort = gamePort;
        }

        Console.Write($"Starting Voice Port [{_config.HonData.StartingVoicePort}]: ");
        var voicePortStr = Console.ReadLine()?.Trim();
        if (int.TryParse(voicePortStr, out var voicePort) && voicePort > 0)
        {
            _config.HonData.StartingVoicePort = voicePort;
        }

        Console.Write($"Manager Port (for game server communication) [{_config.HonData.ManagerPort}]: ");
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
        // Step 6: Master Server Connection
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  Step 6: Master Server Connection");
        Console.WriteLine("===============================================================");
        Console.ResetColor();

        Console.Write($"Master Server Address [{_config.HonData.MasterServer}]: ");
        var masterServer = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(masterServer))
        {
            _config.HonData.MasterServer = masterServer;
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

        Console.Write($"Start servers on launch? (y/n) [{(_config.HonData.StartOnLaunch ? "y" : "n")}]: ");
        var startOnLaunch = Console.ReadLine()?.Trim().ToLower();
        if (startOnLaunch == "y" || startOnLaunch == "yes")
            _config.HonData.StartOnLaunch = true;
        else if (startOnLaunch == "n" || startOnLaunch == "no")
            _config.HonData.StartOnLaunch = false;

        Console.Write($"Enable bot matches? (y/n) [{(_config.HonData.EnableBotMatch ? "y" : "n")}]: ");
        var enableBotMatch = Console.ReadLine()?.Trim().ToLower();
        if (enableBotMatch == "y" || enableBotMatch == "yes")
            _config.HonData.EnableBotMatch = true;
        else if (enableBotMatch == "n" || enableBotMatch == "no")
            _config.HonData.EnableBotMatch = false;

        Console.Write($"Hide server program window? (y/n) [{(_config.HonData.NoConsole ? "y" : "n")}]: ");
        var noConsole = Console.ReadLine()?.Trim().ToLower();
        if (noConsole == "y" || noConsole == "yes")
            _config.HonData.NoConsole = true;
        else if (noConsole == "n" || noConsole == "no")
            _config.HonData.NoConsole = false;

        Console.Write($"Restart servers between games? (y/n) [{(_config.HonData.RestartBetweenGames ? "y" : "n")}]: ");
        var restartBetween = Console.ReadLine()?.Trim().ToLower();
        if (restartBetween == "y" || restartBetween == "yes")
            _config.HonData.RestartBetweenGames = true;
        else if (restartBetween == "n" || restartBetween == "no")
            _config.HonData.RestartBetweenGames = false;

        // ===============================================================
        // Step 8: Proxy Settings
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  Step 8: Proxy Settings");
        Console.WriteLine("===============================================================");
        Console.ResetColor();
        Console.WriteLine("Proxy mode adds 10000 to game ports for external connections.");

        Console.Write($"Enable Proxy Mode? (y/n) [{(_config.HonData.EnableProxy ? "y" : "n")}]: ");
        var enableProxy = Console.ReadLine()?.Trim().ToLower();
        if (enableProxy == "y" || enableProxy == "yes")
            _config.HonData.EnableProxy = true;
        else if (enableProxy == "n" || enableProxy == "no")
            _config.HonData.EnableProxy = false;

        // ===============================================================
        // Step 9: Advanced Settings
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================");
        Console.WriteLine("  Step 9: Advanced Settings");
        Console.WriteLine("===============================================================");
        Console.ResetColor();

        Console.Write($"Use Cowmaster (centralized server management)? (y/n) [{(_config.HonData.UseCowmaster ? "y" : "n")}]: ");
        var useCowmaster = Console.ReadLine()?.Trim().ToLower();
        if (useCowmaster == "y" || useCowmaster == "yes")
            _config.HonData.UseCowmaster = true;
        else if (useCowmaster == "n" || useCowmaster == "no")
            _config.HonData.UseCowmaster = false;

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

        // ===============================================================
        // Summary
        // ===============================================================
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n+==============================================================+");
        Console.WriteLine("|                    Configuration Summary                      |");
        Console.WriteLine("+==============================================================+");
        Console.ResetColor();

        Console.WriteLine("\n+-- Installation --------------------------------------------+");
        Console.WriteLine($"|  HoN Install Dir:   {_config.HonData.HonInstallDirectory}");
        Console.WriteLine($"|  HoN Home Dir:      {_config.HonData.HonHomeDirectory}");
        Console.WriteLine($"|  HoN Logs Dir:      {_config.HonData.HonLogsDirectory}");
        
        Console.WriteLine("+-- Server Identity -----------------------------------------+");
        Console.WriteLine($"|  Login:             {_config.HonData.Login}");
        Console.WriteLine($"|  Server Name:       {_config.HonData.ServerName}");
        Console.WriteLine($"|  Location:          {_config.HonData.Location}");
        Console.WriteLine($"|  Priority:          {_config.HonData.Priority}");
        
        Console.WriteLine("+-- Server Instances ----------------------------------------+");
        Console.WriteLine($"|  Total Servers:     {_config.HonData.TotalServers}");
        Console.WriteLine($"|  Max Start at Once: {_config.HonData.MaxStartAtOnce}");
        Console.WriteLine($"|  Startup Timeout:   {_config.HonData.StartupTimeout}s");
        
        Console.WriteLine("+-- Network Ports -------------------------------------------+");
        Console.WriteLine($"|  Game Port Start:   {_config.HonData.StartingGamePort}");
        Console.WriteLine($"|  Voice Port Start:  {_config.HonData.StartingVoicePort}");
        Console.WriteLine($"|  Manager Port:      {_config.HonData.ManagerPort}");
        Console.WriteLine($"|  API Port:          {_config.HonData.ApiPort}");
        
        Console.WriteLine("+-- Connection ----------------------------------------------+");
        Console.WriteLine($"|  Master Server:     {_config.HonData.MasterServer}");
        Console.WriteLine($"|  Patch Server:      {_config.HonData.PatchServer}");
        
        Console.WriteLine("+-- Options -------------------------------------------------+");
        Console.WriteLine($"|  Start on Launch:   {_config.HonData.StartOnLaunch}");
        Console.WriteLine($"|  Bot Matches:       {_config.HonData.EnableBotMatch}");
        Console.WriteLine($"|  Hide Window:       {_config.HonData.NoConsole}");
        Console.WriteLine($"|  Restart Between:   {_config.HonData.RestartBetweenGames}");
        Console.WriteLine($"|  Enable Proxy:      {_config.HonData.EnableProxy}");
        Console.WriteLine($"|  Use Cowmaster:     {_config.HonData.UseCowmaster}");
        Console.WriteLine("+------------------------------------------------------------+");

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
            // Set console title
            Console.Title = "HoNfigurator .NET - Setup Wizard";
            
            // Windows-specific console settings
            if (OperatingSystem.IsWindows())
            {
                // Set console window size (width x height)
                Console.WindowWidth = Math.Min(120, Console.LargestWindowWidth);
                Console.WindowHeight = Math.Min(40, Console.LargestWindowHeight);
                
                // Set buffer size to match or exceed window size
                Console.BufferWidth = Math.Max(Console.WindowWidth, 120);
                Console.BufferHeight = 1000;
                
                // Set console colors
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
            }
            
            // Enable UTF-8 output
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
        }
        catch
        {
            // Ignore console setup errors (e.g., when running without a console)
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
