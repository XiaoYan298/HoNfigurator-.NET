using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using HoNfigurator.Core.Models;

namespace HoNfigurator.Core.Auth;

public class AuthService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly HoNConfiguration? _config;
    private readonly Dictionary<string, UserInfo> _users = new();
    private readonly object _lock = new();

    public AuthService(HoNConfiguration? config = null, string? secretKey = null)
    {
        _config = config;
        _secretKey = secretKey ?? GenerateSecretKey();
        _issuer = "HoNfigurator";
        _audience = "HoNfigurator-Dashboard";
        
        // Use HoN credentials from config if available, otherwise fallback to admin/admin
        var username = !string.IsNullOrEmpty(_config?.HonData?.Login) ? _config.HonData.Login : "admin";
        var password = !string.IsNullOrEmpty(_config?.HonData?.Password) ? _config.HonData.Password : "admin";
        
        Console.WriteLine($"[AuthService] Initialized with username: {username}");
        
        _users[username.ToLower()] = new UserInfo
        {
            Username = username,
            PasswordHash = HashPassword(password),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };
    }

    public void RefreshCredentials()
    {
        if (_config == null) return;
        
        lock (_lock)
        {
            var username = !string.IsNullOrEmpty(_config.HonData?.Login) ? _config.HonData.Login : "admin";
            var password = !string.IsNullOrEmpty(_config.HonData?.Password) ? _config.HonData.Password : "admin";
            
            _users.Clear();
            _users[username.ToLower()] = new UserInfo
            {
                Username = username,
                PasswordHash = HashPassword(password),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };
        }
    }

    public AuthResult Authenticate(string username, string password)
    {
        Console.WriteLine($"[AuthService] Attempting login for: {username}");
        Console.WriteLine($"[AuthService] Available users: {string.Join(", ", _users.Keys)}");
        
        lock (_lock)
        {
            if (!_users.TryGetValue(username.ToLower(), out var user))
            {
                Console.WriteLine($"[AuthService] User not found: {username.ToLower()}");
                return AuthResult.Failed("Invalid username or password");
            }
            if (!VerifyPassword(password, user.PasswordHash))
            {
                Console.WriteLine($"[AuthService] Password mismatch for: {username}");
                return AuthResult.Failed("Invalid username or password");
            }
            var token = GenerateToken(user);
            user.LastLogin = DateTime.UtcNow;
            Console.WriteLine($"[AuthService] Login successful for: {username}");
            return AuthResult.Success(token, user.Username, user.Role);
        }
    }

    public bool ValidateToken(string token, out ClaimsPrincipal? principal)
    {
        principal = null;
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);
            principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            }, out _);
            return true;
        }
        catch { return false; }
    }

    public bool CreateUser(string username, string password, string role = "User")
    {
        lock (_lock)
        {
            var key = username.ToLower();
            if (_users.ContainsKey(key)) return false;
            _users[key] = new UserInfo { Username = username, PasswordHash = HashPassword(password), Role = role, CreatedAt = DateTime.UtcNow };
            return true;
        }
    }

    public bool ChangePassword(string username, string oldPassword, string newPassword)
    {
        lock (_lock)
        {
            if (!_users.TryGetValue(username.ToLower(), out var user)) return false;
            if (!VerifyPassword(oldPassword, user.PasswordHash)) return false;
            user.PasswordHash = HashPassword(newPassword);
            return true;
        }
    }

    public bool DeleteUser(string username)
    {
        lock (_lock)
        {
            var adminUsername = !string.IsNullOrEmpty(_config?.HonData?.Login) ? _config.HonData.Login.ToLower() : "admin";
            if (username.ToLower() == adminUsername) return false;
            return _users.Remove(username.ToLower());
        }
    }

    public IEnumerable<UserSummary> GetUsers()
    {
        lock (_lock)
        {
            return _users.Values.Select(u => new UserSummary { Username = u.Username, Role = u.Role, CreatedAt = u.CreatedAt, LastLogin = u.LastLogin }).ToList();
        }
    }
    
    public string GetAdminUsername()
    {
        return !string.IsNullOrEmpty(_config?.HonData?.Login) ? _config.HonData.Login : "admin";
    }

    private string GenerateToken(UserInfo user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);
        var claims = new List<Claim> { new(ClaimTypes.Name, user.Username), new(ClaimTypes.Role, user.Role), new("sub", user.Username) };
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    private static bool VerifyPassword(string password, string hash) => HashPassword(password) == hash;

    private static string GenerateSecretKey()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

public class UserInfo
{
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}

public class UserSummary
{
    public required string Username { get; set; }
    public required string Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}

public class AuthResult
{
    public bool IsSuccess { get; set; }
    public string? Token { get; set; }
    public string? Username { get; set; }
    public string? Role { get; set; }
    public string? Error { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public static AuthResult Success(string token, string username, string role) => new() { IsSuccess = true, Token = token, Username = username, Role = role, ExpiresAt = DateTime.UtcNow.AddHours(24) };
    public static AuthResult Failed(string error) => new() { IsSuccess = false, Error = error };
}
