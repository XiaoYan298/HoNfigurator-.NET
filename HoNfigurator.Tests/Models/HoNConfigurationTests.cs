using FluentAssertions;
using HoNfigurator.Core.Models;
using System.Text.Json;

namespace HoNfigurator.Tests.Models;

/// <summary>
/// Tests for HoNConfiguration model and related configuration classes
/// </summary>
public class HoNConfigurationTests
{
    #region HoNConfiguration Tests

    [Fact]
    public void HoNConfiguration_DefaultValues_ShouldBeInitialized()
    {
        var config = new HoNConfiguration();

        config.HonData.Should().NotBeNull();
        config.ApplicationData.Should().NotBeNull();
        config.HealthMonitoring.Should().BeNull();
        config.ServerLifecycle.Should().BeNull();
        config.CpuAffinity.Should().BeNull();
    }

    [Fact]
    public void HoNConfiguration_ConvenienceProperties_ShouldReturnHonDataValues()
    {
        var config = new HoNConfiguration();
        config.HonData.Login = "testuser";
        config.HonData.Password = "testpass";
        config.HonData.ServerName = "MyServer";
        config.HonData.Location = "EU";
        config.HonData.LocalIp = "192.168.1.1";
        config.HonData.ManVersion = "4.10.2";
        config.HonData.AutoPingRespPort = 12345;

        config.SvrLogin.Should().Be("testuser");
        config.SvrPassword.Should().Be("testpass");
        config.SvrName.Should().Be("MyServer");
        config.SvrLocation.Should().Be("EU");
        config.LocalIp.Should().Be("192.168.1.1");
        config.ManVersion.Should().Be("4.10.2");
        config.AutoPingRespPort.Should().Be(12345);
    }

    [Fact]
    public void HoNConfiguration_MasterServerUrl_ShouldIncludeProtocol()
    {
        var config = new HoNConfiguration();
        config.HonData.MasterServer = "api.kongor.net";

        config.MasterServerUrl.Should().Be("http://api.kongor.net");
    }

    #endregion

    #region HoNData Tests

    [Fact]
    public void HoNData_DefaultValues_ShouldBeInitialized()
    {
        var data = new HoNData();

        data.HonInstallDirectory.Should().BeEmpty();
        data.HonHomeDirectory.Should().BeEmpty();
        data.HonLogsDirectory.Should().BeEmpty();
        data.MasterServer.Should().Be("api.kongor.net");
        data.ChatServer.Should().Be("chat.kongor.net");
        data.PatchServer.Should().Be("api.kongor.net/patches");
        data.Login.Should().BeEmpty();
        data.Password.Should().BeEmpty();
        data.ServerName.Should().Be("Unknown");
        data.OverrideSuffix.Should().BeFalse();
        data.Suffix.Should().Be("auto");
        data.OverrideState.Should().BeFalse();
        data.State.Should().Be("auto");
        data.Location.Should().Be("US");
        data.Priority.Should().Be("HIGH");
        data.TotalServers.Should().Be(0);
        data.TotalPerCore.Should().Be(1.0);
        data.EnableProxy.Should().BeTrue();
        data.NoConsole.Should().BeTrue();
        data.EnableBotMatch.Should().BeTrue();
        data.StartOnLaunch.Should().BeFalse();
        data.OverrideAffinity.Should().BeFalse();
        data.MaxStartAtOnce.Should().Be(5);
        data.StartingGamePort.Should().Be(10001);
        data.StartingVoicePort.Should().Be(10061);
        data.ManagerPort.Should().Be(11235);
        data.StartupTimeout.Should().Be(180);
        data.ApiPort.Should().Be(5050);
        data.UseCowmaster.Should().BeFalse();
        data.RestartBetweenGames.Should().BeFalse();
        data.BetaMode.Should().BeFalse();
        data.LocalIp.Should().BeNull();
        data.ManVersion.Should().Be("4.10.1");
        data.AutoPingRespPort.Should().Be(10069);
        data.ServerIp.Should().BeNull();
        data.ProxyLocalAddr.Should().BeNull();
        data.ProxyRemoteAddr.Should().BeNull();
        data.ProxyPort.Should().Be(1135);
    }

    [Fact]
    public void HoNData_Serialization_ShouldUseJsonPropertyNames()
    {
        var data = new HoNData
        {
            Login = "testuser",
            ServerName = "TestServer"
        };

        var json = JsonSerializer.Serialize(data);

        json.Should().Contain("\"svr_login\"");
        json.Should().Contain("\"svr_name\"");
    }

    #endregion

    #region ApplicationData Tests

    [Fact]
    public void ApplicationData_DefaultValues_ShouldBeInitialized()
    {
        var data = new ApplicationData();

        data.Timers.Should().NotBeNull();
        data.Discord.Should().BeNull();
        data.Mqtt.Should().BeNull();
        data.AutoScaling.Should().BeNull();
        data.ReplayUpload.Should().BeNull();
        data.Storage.Should().BeNull();
        data.Filebeat.Should().BeNull();
        data.GitHub.Should().BeNull();
        data.Certificates.Should().BeNull();
        data.DiskMonitoring.Should().BeNull();
    }

    #endregion

    #region StorageConfiguration Tests

    [Fact]
    public void StorageConfiguration_DefaultValues_ShouldBeInitialized()
    {
        var storage = new StorageConfiguration();

        storage.PrimaryPath.Should().Be("replays");
        storage.ArchivePath.Should().BeNull();
        storage.LogsPath.Should().Be("logs");
        storage.ArchiveAfterDays.Should().Be(7);
        storage.RetentionDays.Should().Be(90);
        storage.AutoRelocate.Should().BeFalse();
        storage.AutoCleanup.Should().BeFalse();
    }

    #endregion

    #region FilebeatConfiguration Tests

    [Fact]
    public void FilebeatConfiguration_DefaultValues_ShouldBeInitialized()
    {
        var filebeat = new FilebeatConfiguration();

        filebeat.Enabled.Should().BeFalse();
        filebeat.InstallPath.Should().Be(@"C:\Program Files\Filebeat");
        filebeat.Version.Should().Be("8.11.0");
        filebeat.ElasticsearchHost.Should().Be("localhost:9200");
        filebeat.ElasticsearchUsername.Should().BeNull();
        filebeat.ElasticsearchPassword.Should().BeNull();
        filebeat.LogPaths.Should().Contain("logs/*.log");
        filebeat.IndexPrefix.Should().Be("honfigurator");
        filebeat.AutoStart.Should().BeTrue();
        filebeat.Environment.Should().Be("production");
    }

    #endregion

    #region GitHubConfiguration Tests

    [Fact]
    public void GitHubConfiguration_DefaultValues_ShouldBeInitialized()
    {
        var github = new GitHubConfiguration();

        github.Repository.Should().Be("HoNfigurator/HoNfigurator-dotnet");
        github.Token.Should().BeNull();
        github.AutoUpdate.Should().BeFalse();
        github.CheckUpdatesOnStartup.Should().BeTrue();
        github.PreferredBranch.Should().Be("main");
    }

    #endregion

    #region ReplayUploadSettings Tests

    [Fact]
    public void ReplayUploadSettings_DefaultValues_ShouldBeInitialized()
    {
        var settings = new ReplayUploadSettings();

        settings.Enabled.Should().BeFalse();
        settings.Provider.Should().Be("local");
        settings.ConnectionString.Should().BeEmpty();
        settings.ContainerName.Should().Be("replays");
        settings.BasePath.Should().BeEmpty();
        settings.AutoUploadOnMatchEnd.Should().BeTrue();
        settings.RetryCount.Should().Be(3);
        settings.RetryDelaySeconds.Should().Be(5);
        settings.BaseUrl.Should().BeEmpty();
    }

    #endregion

    #region AutoScalingSettings Tests

    [Fact]
    public void AutoScalingSettings_DefaultValues_ShouldBeInitialized()
    {
        var settings = new AutoScalingSettings();

        settings.Enabled.Should().BeFalse();
        settings.MinServers.Should().Be(1);
        settings.MaxServers.Should().Be(10);
        settings.ScaleUpThreshold.Should().Be(80);
        settings.ScaleDownThreshold.Should().Be(20);
        settings.CooldownSeconds.Should().Be(300);
        settings.CheckIntervalSeconds.Should().Be(60);
        settings.MinReadyServers.Should().Be(1);
    }

    #endregion

    #region MqttSettings Tests

    [Fact]
    public void MqttSettings_DefaultValues_ShouldBeInitialized()
    {
        var settings = new MqttSettings();

        settings.Enabled.Should().BeFalse();
        settings.Host.Should().Be("localhost");
        settings.Port.Should().Be(1883);
        settings.Username.Should().BeNull();
        settings.Password.Should().BeNull();
        settings.TopicPrefix.Should().Be("honfigurator");
        settings.UseTls.Should().BeFalse();
    }

    #endregion

    #region DiscordSettings Tests

    [Fact]
    public void DiscordSettings_DefaultValues_ShouldBeInitialized()
    {
        var settings = new DiscordSettings();

        settings.OwnerId.Should().BeEmpty();
        settings.BotToken.Should().BeEmpty();
        settings.NotificationChannelId.Should().BeEmpty();
        settings.EnableNotifications.Should().BeTrue();
        settings.NotifyMatchStart.Should().BeTrue();
        settings.NotifyMatchEnd.Should().BeTrue();
        settings.NotifyPlayerJoinLeave.Should().BeFalse();
    }

    #endregion

    #region TimerSettings Tests

    [Fact]
    public void TimerSettings_DefaultValues_ShouldBeInitialized()
    {
        var settings = new TimerSettings();

        settings.Manager.Should().NotBeNull();
        settings.ReplayCleaner.Should().NotBeNull();
    }

    [Fact]
    public void ManagerTimers_DefaultValues_ShouldBeInitialized()
    {
        var timers = new ManagerTimers();

        timers.PublicIpHealthcheck.Should().Be(1800);
        timers.GeneralHealthcheck.Should().Be(60);
        timers.LagHealthcheck.Should().Be(120);
        timers.CheckForHonUpdate.Should().Be(120);
    }

    [Fact]
    public void ReplayCleanerSettings_DefaultValues_ShouldBeInitialized()
    {
        var settings = new ReplayCleanerSettings();

        settings.Active.Should().BeFalse();
        settings.MaxReplayAgeDays.Should().Be(0);
        settings.MaxTempFilesAgeDays.Should().Be(1);
    }

    #endregion

    #region CertificatesConfiguration Tests

    [Fact]
    public void CertificatesConfiguration_DefaultValues_ShouldBeInitialized()
    {
        var config = new CertificatesConfiguration();

        config.BasePath.Should().Be("certs");
        config.StepCliPath.Should().BeNull();
        config.DefaultName.Should().Be("server");
        config.CaUrl.Should().BeNull();
        config.CaFingerprint.Should().BeNull();
        config.AutoRenew.Should().BeTrue();
        config.RenewThresholdDays.Should().Be(30);
    }

    #endregion

    #region DiskMonitoringConfiguration Tests

    [Fact]
    public void DiskMonitoringConfiguration_DefaultValues_ShouldBeInitialized()
    {
        var config = new DiskMonitoringConfiguration();

        config.Enabled.Should().BeTrue();
        config.WarningThreshold.Should().Be(80);
        config.CriticalThreshold.Should().Be(95);
        config.CheckIntervalMinutes.Should().Be(15);
        config.MonitoredPaths.Should().BeEmpty();
        config.AlertCooldownMinutes.Should().Be(60);
    }

    [Fact]
    public void MonitoredPath_DefaultValues_ShouldBeInitialized()
    {
        var path = new MonitoredPath();

        path.Path.Should().BeEmpty();
        path.Name.Should().BeNull();
        path.MaxSizeGb.Should().Be(0);
    }

    #endregion

    #region HealthMonitoringConfiguration Tests

    [Fact]
    public void HealthMonitoringConfiguration_DefaultValues_ShouldBeInitialized()
    {
        var config = new HealthMonitoringConfiguration();

        config.AutoPingEnabled.Should().BeTrue();
        config.AutoPingIntervalMs.Should().Be(30000);
        config.MaxConsecutiveFailures.Should().Be(3);
        config.AutoRestartOnUnhealthy.Should().BeTrue();
        config.PingTimeoutMs.Should().Be(5000);
    }

    #endregion

    #region ServerLifecycleConfiguration Tests

    [Fact]
    public void ServerLifecycleConfiguration_DefaultValues_ShouldBeInitialized()
    {
        var config = new ServerLifecycleConfiguration();

        config.PeriodicRestartEnabled.Should().BeTrue();
        config.MinUptimeHours.Should().Be(24);
        config.MaxUptimeHours.Should().Be(48);
        config.CheckIntervalMinutes.Should().Be(5);
        config.MaxWaitForGameMinutes.Should().Be(60);
        config.RestartWindowStartHour.Should().BeNull();
        config.RestartWindowEndHour.Should().BeNull();
    }

    #endregion

    #region CpuAffinityConfiguration Tests

    [Fact]
    public void CpuAffinityConfiguration_DefaultValues_ShouldBeInitialized()
    {
        var config = new CpuAffinityConfiguration();

        config.Enabled.Should().BeFalse();
        config.CoresPerServer.Should().Be(1);
        config.StartCore.Should().Be(0);
        config.ReservedCores.Should().Be(2);
        config.AutoAssignOnStart.Should().BeTrue();
        config.SetPriority.Should().BeFalse();
        config.PriorityLevel.Should().Be("Normal");
    }

    #endregion

    #region Serialization Round-Trip Tests

    [Fact]
    public void HoNConfiguration_SerializationRoundTrip_ShouldPreserveValues()
    {
        var original = new HoNConfiguration
        {
            HonData = new HoNData
            {
                Login = "testuser",
                Password = "testpass",
                ServerName = "TestServer",
                TotalServers = 5
            },
            HealthMonitoring = new HealthMonitoringConfiguration
            {
                AutoPingEnabled = true,
                MaxConsecutiveFailures = 5
            }
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<HoNConfiguration>(json);

        deserialized.Should().NotBeNull();
        deserialized!.HonData.Login.Should().Be("testuser");
        deserialized.HonData.ServerName.Should().Be("TestServer");
        deserialized.HonData.TotalServers.Should().Be(5);
        deserialized.HealthMonitoring.Should().NotBeNull();
        deserialized.HealthMonitoring!.MaxConsecutiveFailures.Should().Be(5);
    }

    [Fact]
    public void HoNData_SerializationRoundTrip_ShouldPreserveValues()
    {
        var original = new HoNData
        {
            Login = "user1",
            Password = "pass1",
            ServerName = "Server1",
            Location = "EU",
            TotalServers = 3,
            EnableProxy = false,
            MaxStartAtOnce = 2
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<HoNData>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Login.Should().Be("user1");
        deserialized.Location.Should().Be("EU");
        deserialized.TotalServers.Should().Be(3);
        deserialized.EnableProxy.Should().BeFalse();
        deserialized.MaxStartAtOnce.Should().Be(2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void HoNConfiguration_NullOptionalConfigs_ShouldSerializeCorrectly()
    {
        var config = new HoNConfiguration();

        var json = JsonSerializer.Serialize(config);

        json.Should().Contain("\"health_monitoring\":null");
        json.Should().Contain("\"server_lifecycle\":null");
        json.Should().Contain("\"cpu_affinity\":null");
    }

    [Fact]
    public void DiskMonitoringConfiguration_WithMonitoredPaths_ShouldStoreCorrectly()
    {
        var config = new DiskMonitoringConfiguration();
        config.MonitoredPaths.Add(new MonitoredPath
        {
            Path = @"C:\Replays",
            Name = "Replay Storage",
            MaxSizeGb = 100
        });
        config.MonitoredPaths.Add(new MonitoredPath
        {
            Path = @"C:\Logs",
            Name = "Log Storage",
            MaxSizeGb = 50
        });

        config.MonitoredPaths.Should().HaveCount(2);
        config.MonitoredPaths[0].Path.Should().Be(@"C:\Replays");
        config.MonitoredPaths[0].MaxSizeGb.Should().Be(100);
        config.MonitoredPaths[1].Name.Should().Be("Log Storage");
    }

    #endregion
}
