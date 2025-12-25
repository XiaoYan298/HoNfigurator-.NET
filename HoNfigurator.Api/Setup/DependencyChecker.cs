using System.Diagnostics;
using System.Net.Http;
using HoNfigurator.Core.Models;

namespace HoNfigurator.Api.Setup;

/// <summary>
/// Checks for required dependencies
/// </summary>
public class DependencyChecker
{
    private readonly HoNConfiguration _config;
    
    // GitHub repo for proxy
    private const string ProxyGitHubRepo = "https://github.com/wasserver/wasserver";
    
    public DependencyChecker(HoNConfiguration config)
    {
        _config = config;
    }
    
    /// <summary>
    /// Run all dependency checks
    /// </summary>
    public async Task<bool> CheckAllDependenciesAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n");
        Console.WriteLine("  Checking Dependencies...");
        Console.WriteLine("");
        Console.ResetColor();
        
        var allPassed = true;
        
        // Check HoN installation
        var honCheck = CheckHonInstallation();
        allPassed &= honCheck;
        
        // Check proxy.exe if proxy is enabled
        if (_config.HonData.EnableProxy)
        {
            var proxyCheck = CheckProxy();
            allPassed &= proxyCheck;
        }
        
        // Check write permissions
        var writeCheck = CheckWritePermissions();
        allPassed &= writeCheck;
        
        Console.WriteLine();
        
        if (allPassed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" All dependencies are satisfied!");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" Some dependencies are missing. The application may not work correctly.");
            Console.ResetColor();
        }
        
        Console.WriteLine();
        await Task.CompletedTask;
        return allPassed;
    }
    
    /// <summary>
    /// Check if HoN is installed
    /// </summary>
    private bool CheckHonInstallation()
    {
        Console.Write("\n  [1/3] HoN Installation... ");
        
        var installDir = _config.HonData?.HonInstallDirectory;
        
        if (string.IsNullOrEmpty(installDir))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" Not configured");
            Console.ResetColor();
            Console.WriteLine("        Please set HoN Install Directory in the Setup Wizard or Dashboard.");
            return false;
        }
        
        if (!Directory.Exists(installDir))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Directory not found: {installDir}");
            Console.ResetColor();
            return false;
        }
        
        // Look for HoN executable
        var possibleExes = new[] { "hon_x64.exe", "hon.exe", "k2_x64.exe", "k2.exe" };
        string? foundExe = null;
        
        foreach (var exe in possibleExes)
        {
            var exePath = Path.Combine(installDir, exe);
            if (File.Exists(exePath))
            {
                foundExe = exe;
                break;
            }
        }
        
        if (foundExe != null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" Found {foundExe}");
            Console.ResetColor();
            return true;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" HoN executable not found");
            Console.ResetColor();
            Console.WriteLine($"        Searched in: {installDir}");
            Console.WriteLine("        Expected: hon_x64.exe, hon.exe, k2_x64.exe, or k2.exe");
            return false;
        }
    }
    
    /// <summary>
    /// Check if proxy.exe exists
    /// </summary>
    private bool CheckProxy()
    {
        Console.Write("  [2/3] Proxy (proxy.exe)... ");
        
        var installDir = _config.HonData?.HonInstallDirectory;
        if (string.IsNullOrEmpty(installDir))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" Skipped (no install directory)");
            Console.ResetColor();
            return true;
        }
        
        var proxyPath = Path.Combine(installDir, "proxy.exe");
        
        if (File.Exists(proxyPath))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" Found");
            Console.ResetColor();
            return true;
        }
        
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(" Not found");
        Console.ResetColor();
        
        Console.WriteLine($"        Proxy mode is enabled but proxy.exe is missing.");
        Console.WriteLine($"        Please download proxy.exe from: {ProxyGitHubRepo}");
        Console.WriteLine($"        Copy proxy.exe to: {installDir}");
        
        return false;
    }
    
    /// <summary>
    /// Check write permissions to necessary directories
    /// </summary>
    private bool CheckWritePermissions()
    {
        Console.Write("  [3/3] Write Permissions... ");
        
        var dirsToCheck = new List<string>();
        
        // Config directory
        var configDir = Path.Combine(AppContext.BaseDirectory, "config");
        dirsToCheck.Add(configDir);
        
        // HoN Home directory (for logs, replays, proxy config)
        if (!string.IsNullOrEmpty(_config.HonData?.HonHomeDirectory))
        {
            dirsToCheck.Add(_config.HonData.HonHomeDirectory);
        }
        else if (!string.IsNullOrEmpty(_config.HonData?.HonInstallDirectory))
        {
            dirsToCheck.Add(_config.HonData.HonInstallDirectory);
        }
        
        var failedDirs = new List<string>();
        
        foreach (var dir in dirsToCheck)
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                // Try to write a test file
                var testFile = Path.Combine(dir, ".write_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            catch
            {
                failedDirs.Add(dir);
            }
        }
        
        if (failedDirs.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" OK");
            Console.ResetColor();
            return true;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" Failed");
            Console.ResetColor();
            foreach (var dir in failedDirs)
            {
                Console.WriteLine($"        Cannot write to: {dir}");
            }
            Console.WriteLine("        Please run as administrator or check folder permissions.");
            return false;
        }
    }
    
    /// <summary>
    /// Get dependency status for API
    /// </summary>
    public DependencyStatus GetDependencyStatus()
    {
        var status = new DependencyStatus();
        
        // Check HoN
        var installDir = _config.HonData?.HonInstallDirectory;
        if (!string.IsNullOrEmpty(installDir) && Directory.Exists(installDir))
        {
            var possibleExes = new[] { "hon_x64.exe", "hon.exe", "k2_x64.exe", "k2.exe" };
            status.HonInstalled = possibleExes.Any(exe => File.Exists(Path.Combine(installDir, exe)));
            status.HonPath = installDir;
        }
        
        // Check Proxy
        if (!string.IsNullOrEmpty(installDir))
        {
            var proxyPath = Path.Combine(installDir, "proxy.exe");
            status.ProxyInstalled = File.Exists(proxyPath);
            status.ProxyPath = proxyPath;
        }
        
        status.ProxyEnabled = _config.HonData?.EnableProxy ?? false;
        status.ProxyDownloadUrl = ProxyGitHubRepo;
        
        return status;
    }
}

/// <summary>
/// Status of all dependencies
/// </summary>
public class DependencyStatus
{
    public bool HonInstalled { get; set; }
    public string? HonPath { get; set; }
    public bool ProxyInstalled { get; set; }
    public string? ProxyPath { get; set; }
    public bool ProxyEnabled { get; set; }
    public string? ProxyDownloadUrl { get; set; }
    
    public bool AllSatisfied => HonInstalled && (!ProxyEnabled || ProxyInstalled);
}
