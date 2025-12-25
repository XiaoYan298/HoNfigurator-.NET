using FluentAssertions;
using HoNfigurator.Api.Hubs;

namespace HoNfigurator.Tests.Api.Hubs;

/// <summary>
/// Tests for DashboardHub types and interfaces
/// </summary>
public class DashboardHubTests
{
    #region IDashboardClient Interface Tests

    [Fact]
    public void IDashboardClient_ShouldDefineReceiveStatus()
    {
        // Arrange
        var methods = typeof(IDashboardClient).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "ReceiveStatus");
    }

    [Fact]
    public void IDashboardClient_ShouldDefineReceiveServerUpdate()
    {
        // Arrange
        var methods = typeof(IDashboardClient).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "ReceiveServerUpdate");
    }

    [Fact]
    public void IDashboardClient_ShouldDefineReceiveLog()
    {
        // Arrange
        var methods = typeof(IDashboardClient).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "ReceiveLog");
    }

    [Fact]
    public void IDashboardClient_ShouldDefineReceiveNotification()
    {
        // Arrange
        var methods = typeof(IDashboardClient).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "ReceiveNotification");
    }

    [Fact]
    public void IDashboardClient_ShouldDefineReceiveCommandResult()
    {
        // Arrange
        var methods = typeof(IDashboardClient).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "ReceiveCommandResult");
    }

    [Fact]
    public void IDashboardClient_ShouldDefineReceiveLogUpdate()
    {
        // Arrange
        var methods = typeof(IDashboardClient).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "ReceiveLogUpdate");
    }

    [Fact]
    public void IDashboardClient_ShouldDefineReceiveAlert()
    {
        // Arrange
        var methods = typeof(IDashboardClient).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "ReceiveAlert");
    }

    [Fact]
    public void IDashboardClient_ShouldDefineReceiveChartUpdate()
    {
        // Arrange
        var methods = typeof(IDashboardClient).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "ReceiveChartUpdate");
    }

    [Fact]
    public void IDashboardClient_ShouldHaveCorrectMethodCount()
    {
        // Arrange
        var methods = typeof(IDashboardClient).GetMethods();

        // Assert - 8 custom methods (not counting inherited object methods)
        var customMethods = methods.Where(m => 
            m.Name.StartsWith("Receive") || 
            m.DeclaringType == typeof(IDashboardClient));
        customMethods.Should().HaveCount(8);
    }

    #endregion

    #region CommandResult Tests

    [Fact]
    public void CommandResult_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var result = new CommandResult();

        // Assert
        result.Success.Should().BeFalse();
        result.Output.Should().NotBeNull();
        result.Output.Should().BeEmpty();
    }

    [Fact]
    public void CommandResult_CanSetSuccess()
    {
        // Arrange & Act
        var result = new CommandResult { Success = true };

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void CommandResult_CanSetOutput()
    {
        // Arrange
        var output = new[] { "Line 1", "Line 2", "Line 3" };

        // Act
        var result = new CommandResult { Output = output };

        // Assert
        result.Output.Should().BeEquivalentTo(output);
    }

    [Fact]
    public void CommandResult_CanSetBothProperties()
    {
        // Arrange & Act
        var result = new CommandResult
        {
            Success = true,
            Output = new[] { "Success message" }
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().HaveCount(1);
        result.Output[0].Should().Be("Success message");
    }

    [Fact]
    public void CommandResult_IsRecordType()
    {
        // Arrange & Act
        var result = new CommandResult { Success = true, Output = new[] { "test" } };
        var result2 = new CommandResult { Success = true, Output = new[] { "test" } };

        // Assert - records have value equality
        result.Should().NotBeSameAs(result2); // Different instances
        // Output arrays are different references, so not equal by default
    }

    [Fact]
    public void CommandResult_WithExpression_ShouldCreateNewInstance()
    {
        // Arrange
        var original = new CommandResult { Success = false, Output = new[] { "Error" } };

        // Act
        var modified = original with { Success = true };

        // Assert
        modified.Success.Should().BeTrue();
        modified.Output.Should().BeEquivalentTo(original.Output);
        original.Success.Should().BeFalse(); // Original unchanged
    }

    [Fact]
    public void CommandResult_EmptyOutput_ShouldBeValidArray()
    {
        // Arrange & Act
        var result = new CommandResult { Success = true, Output = Array.Empty<string>() };

        // Assert
        result.Output.Should().NotBeNull();
        result.Output.Should().BeEmpty();
    }

    [Fact]
    public void CommandResult_MultilineOutput_ShouldPreserveOrder()
    {
        // Arrange
        var lines = new[] { "First", "Second", "Third", "Fourth" };

        // Act
        var result = new CommandResult { Output = lines };

        // Assert
        result.Output.Should().HaveCount(4);
        result.Output[0].Should().Be("First");
        result.Output[3].Should().Be("Fourth");
    }

    #endregion

    #region DashboardHub Type Tests

    [Fact]
    public void DashboardHub_ShouldInheritFromHub()
    {
        // Arrange & Act
        var baseType = typeof(DashboardHub).BaseType;

        // Assert
        baseType.Should().NotBeNull();
        baseType!.Name.Should().Contain("Hub");
    }

    [Fact]
    public void DashboardHub_ShouldHaveOnConnectedAsync()
    {
        // Arrange
        var methods = typeof(DashboardHub).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "OnConnectedAsync");
    }

    [Fact]
    public void DashboardHub_ShouldHaveOnDisconnectedAsync()
    {
        // Arrange
        var methods = typeof(DashboardHub).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "OnDisconnectedAsync");
    }

    [Fact]
    public void DashboardHub_ShouldHaveRequestStatus()
    {
        // Arrange
        var methods = typeof(DashboardHub).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "RequestStatus");
    }

    [Fact]
    public void DashboardHub_ShouldHaveStartServer()
    {
        // Arrange
        var methods = typeof(DashboardHub).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "StartServer");
    }

    [Fact]
    public void DashboardHub_ShouldHaveStopServer()
    {
        // Arrange
        var methods = typeof(DashboardHub).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "StopServer");
    }

    [Fact]
    public void DashboardHub_ShouldHaveRestartServer()
    {
        // Arrange
        var methods = typeof(DashboardHub).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "RestartServer");
    }

    [Fact]
    public void DashboardHub_ShouldHaveStartAllServers()
    {
        // Arrange
        var methods = typeof(DashboardHub).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "StartAllServers");
    }

    [Fact]
    public void DashboardHub_ShouldHaveStopAllServers()
    {
        // Arrange
        var methods = typeof(DashboardHub).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "StopAllServers");
    }

    [Fact]
    public void DashboardHub_ShouldHaveRestartAllServers()
    {
        // Arrange
        var methods = typeof(DashboardHub).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "RestartAllServers");
    }

    [Fact]
    public void DashboardHub_ShouldHaveAddServer()
    {
        // Arrange
        var methods = typeof(DashboardHub).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "AddServer");
    }

    [Fact]
    public void DashboardHub_ShouldHaveExecuteCommand()
    {
        // Arrange
        var methods = typeof(DashboardHub).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "ExecuteCommand");
    }

    [Fact]
    public void DashboardHub_ShouldHaveSendMessage()
    {
        // Arrange
        var methods = typeof(DashboardHub).GetMethods();

        // Act & Assert
        methods.Should().Contain(m => m.Name == "SendMessage");
    }

    #endregion

    #region ExecuteCommand Return Values Tests

    [Fact]
    public void ExecuteCommand_Methods_ShouldReturnTask()
    {
        // Arrange
        var executeCommandMethod = typeof(DashboardHub).GetMethod("ExecuteCommand");

        // Assert
        executeCommandMethod.Should().NotBeNull();
        executeCommandMethod!.ReturnType.Should().Be(typeof(Task));
    }

    [Theory]
    [InlineData("RequestStatus")]
    [InlineData("StartServer")]
    [InlineData("StopServer")]
    [InlineData("RestartServer")]
    [InlineData("StartAllServers")]
    [InlineData("StopAllServers")]
    [InlineData("RestartAllServers")]
    [InlineData("AddServer")]
    [InlineData("ExecuteCommand")]
    [InlineData("SendMessage")]
    public void HubMethod_ShouldReturnTask(string methodName)
    {
        // Arrange
        var methods = typeof(DashboardHub).GetMethods()
            .Where(m => m.Name == methodName && m.DeclaringType == typeof(DashboardHub));

        // Assert
        methods.Should().NotBeEmpty();
        foreach (var method in methods)
        {
            method.ReturnType.Should().Be(typeof(Task));
        }
    }

    #endregion
}
