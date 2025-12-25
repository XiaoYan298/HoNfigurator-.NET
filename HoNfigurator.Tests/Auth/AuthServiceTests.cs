using FluentAssertions;
using HoNfigurator.Core.Auth;
using HoNfigurator.Core.Models;

namespace HoNfigurator.Tests.Auth;

public class AuthServiceTests
{
    private static HoNConfiguration CreateTestConfig(string? login = null, string? password = null)
    {
        return new HoNConfiguration
        {
            HonData = new HoNData
            {
                Login = login ?? "testuser",
                Password = password ?? "testpass"
            }
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithoutConfig_ShouldCreateDefaultAdmin()
    {
        // Act
        var service = new AuthService();

        // Assert
        var result = service.Authenticate("admin", "admin");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithConfig_ShouldUseConfigCredentials()
    {
        // Arrange
        var config = CreateTestConfig("myuser", "mypassword");

        // Act
        var service = new AuthService(config);

        // Assert
        var result = service.Authenticate("myuser", "mypassword");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithEmptyConfigCredentials_ShouldFallbackToDefaults()
    {
        // Arrange
        var config = new HoNConfiguration { HonData = new HoNData() };

        // Act
        var service = new AuthService(config);

        // Assert
        var result = service.Authenticate("admin", "admin");
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Authenticate Tests

    [Fact]
    public void Authenticate_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var result = service.Authenticate("admin", "admin");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.Username.Should().Be("admin");
        result.Role.Should().Be("Admin");
    }

    [Fact]
    public void Authenticate_WithInvalidUsername_ShouldReturnFailure()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var result = service.Authenticate("wronguser", "admin");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid");
    }

    [Fact]
    public void Authenticate_WithInvalidPassword_ShouldReturnFailure()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var result = service.Authenticate("admin", "wrongpass");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Authenticate_ShouldBeCaseInsensitiveForUsername()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var result = service.Authenticate("ADMIN", "admin");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Token Validation Tests

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var service = new AuthService();
        var authResult = service.Authenticate("admin", "admin");

        // Act
        var isValid = service.ValidateToken(authResult.Token!, out var principal);

        // Assert
        isValid.Should().BeTrue();
        principal.Should().NotBeNull();
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var isValid = service.ValidateToken("invalid.token.here", out var principal);

        // Assert
        isValid.Should().BeFalse();
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithEmptyToken_ShouldReturnFalse()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var isValid = service.ValidateToken("", out var principal);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region CreateUser Tests

    [Fact]
    public void CreateUser_WithNewUsername_ShouldSucceed()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var result = service.CreateUser("newuser", "newpassword", "User");

        // Assert
        result.Should().BeTrue();
        service.Authenticate("newuser", "newpassword").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CreateUser_WithExistingUsername_ShouldFail()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var result = service.CreateUser("admin", "newpassword");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CreateUser_ShouldBeCaseInsensitive()
    {
        // Arrange
        var service = new AuthService();
        service.CreateUser("testuser", "pass1");

        // Act
        var result = service.CreateUser("TESTUSER", "pass2");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public void ChangePassword_WithCorrectOldPassword_ShouldSucceed()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var result = service.ChangePassword("admin", "admin", "newpassword");

        // Assert
        result.Should().BeTrue();
        service.Authenticate("admin", "newpassword").IsSuccess.Should().BeTrue();
        service.Authenticate("admin", "admin").IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ChangePassword_WithIncorrectOldPassword_ShouldFail()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var result = service.ChangePassword("admin", "wrongold", "newpassword");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ChangePassword_ForNonExistentUser_ShouldFail()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var result = service.ChangePassword("nonexistent", "oldpass", "newpass");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region DeleteUser Tests

    [Fact]
    public void DeleteUser_WithExistingUser_ShouldSucceed()
    {
        // Arrange
        var service = new AuthService();
        service.CreateUser("tempuser", "temppass");

        // Act
        var result = service.DeleteUser("tempuser");

        // Assert
        result.Should().BeTrue();
        service.Authenticate("tempuser", "temppass").IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void DeleteUser_ForAdminUser_ShouldFail()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var result = service.DeleteUser("admin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DeleteUser_ForNonExistentUser_ShouldFail()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var result = service.DeleteUser("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetUsers Tests

    [Fact]
    public void GetUsers_ShouldReturnAllUsers()
    {
        // Arrange
        var service = new AuthService();
        service.CreateUser("user1", "pass1");
        service.CreateUser("user2", "pass2");

        // Act
        var users = service.GetUsers().ToList();

        // Assert
        users.Should().HaveCount(3); // admin + 2 new users
        users.Should().Contain(u => u.Username == "admin");
        users.Should().Contain(u => u.Username == "user1");
        users.Should().Contain(u => u.Username == "user2");
    }

    [Fact]
    public void GetUsers_ShouldReturnUserSummaryWithoutPasswords()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var users = service.GetUsers().ToList();

        // Assert
        users.First().Should().BeOfType<UserSummary>();
        users.First().Role.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region RefreshCredentials Tests

    [Fact]
    public void RefreshCredentials_ShouldUpdateUserFromConfig()
    {
        // Arrange
        var config = CreateTestConfig("initialuser", "initialpass");
        var service = new AuthService(config);
        
        config.HonData.Login = "newuser";
        config.HonData.Password = "newpassword";

        // Act
        service.RefreshCredentials();

        // Assert
        service.Authenticate("newuser", "newpassword").IsSuccess.Should().BeTrue();
        service.Authenticate("initialuser", "initialpass").IsSuccess.Should().BeFalse();
    }

    #endregion

    #region GetAdminUsername Tests

    [Fact]
    public void GetAdminUsername_WithConfig_ShouldReturnConfigUsername()
    {
        // Arrange
        var config = CreateTestConfig("configadmin", "pass");
        var service = new AuthService(config);

        // Act
        var username = service.GetAdminUsername();

        // Assert
        username.Should().Be("configadmin");
    }

    [Fact]
    public void GetAdminUsername_WithoutConfig_ShouldReturnDefault()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var username = service.GetAdminUsername();

        // Assert
        username.Should().Be("admin");
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task ConcurrentAuthentication_ShouldBeThreadSafe()
    {
        // Arrange
        var service = new AuthService();
        var tasks = new List<Task<AuthResult>>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => service.Authenticate("admin", "admin")));
        }
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().OnlyContain(r => r.IsSuccess);
    }

    [Fact]
    public async Task ConcurrentUserOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var service = new AuthService();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 50; i++)
        {
            var idx = i;
            tasks.Add(Task.Run(() => service.CreateUser($"user{idx}", $"pass{idx}")));
        }
        await Task.WhenAll(tasks);

        // Assert
        var users = service.GetUsers().ToList();
        users.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    #endregion
}
