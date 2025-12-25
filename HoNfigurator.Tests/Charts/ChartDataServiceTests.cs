using FluentAssertions;
using HoNfigurator.Core.Charts;

namespace HoNfigurator.Tests.Charts;

public class ChartDataServiceTests
{
    private ChartDataService CreateService()
    {
        return new ChartDataService();
    }

    #region Server Uptime Tests

    [Fact]
    public void RecordServerStart_ShouldTrackServerUptime()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.RecordServerStart(1, "Server #1");

        // Assert
        var uptime = service.GetUptimePercentage(1, hours: 1);
        uptime.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RecordServerStop_ShouldEndUptimeTracking()
    {
        // Arrange
        var service = CreateService();
        service.RecordServerStart(1, "Server #1");

        // Act
        service.RecordServerStop(1, "Stopped");

        // Assert
        var history = service.GetUptimeHistory(serverId: 1);
        history.Should().HaveCount(1);
        history[0].EndReason.Should().Be("Stopped");
    }

    [Fact]
    public void GetUptimeHistory_WithMultipleServers_ShouldFilterByServerId()
    {
        // Arrange
        var service = CreateService();
        service.RecordServerStart(1, "Server #1");
        service.RecordServerStart(2, "Server #2");
        service.RecordServerStop(1, "Stopped");
        service.RecordServerStop(2, "Crashed");

        // Act
        var history1 = service.GetUptimeHistory(serverId: 1);
        var history2 = service.GetUptimeHistory(serverId: 2);
        var historyAll = service.GetUptimeHistory();

        // Assert
        history1.Should().HaveCount(1);
        history2.Should().HaveCount(1);
        historyAll.Should().HaveCount(2);
    }

    [Fact]
    public void GetAllServersUptime_ShouldReturnUptimeForAllServers()
    {
        // Arrange
        var service = CreateService();
        service.RecordServerStart(1, "Server #1");
        service.RecordServerStart(2, "Server #2");

        // Act
        var uptimes = service.GetAllServersUptime(hours: 1);

        // Assert
        uptimes.Should().ContainKey(1);
        uptimes.Should().ContainKey(2);
    }

    [Fact]
    public void GetUptimePercentage_WithNoData_ShouldReturnZero()
    {
        // Arrange
        var service = CreateService();

        // Act
        var uptime = service.GetUptimePercentage(999, hours: 1);

        // Assert
        uptime.Should().Be(0);
    }

    #endregion

    #region Player Count Tests

    [Fact]
    public void RecordPlayerCount_ShouldAddSnapshot()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.RecordPlayerCount(1, 10);

        // Assert
        var history = service.GetPlayerCountHistory(hours: 1);
        history.Should().NotBeEmpty();
    }

    [Fact]
    public void GetPlayerCountHistory_ShouldReturnRecentSnapshots()
    {
        // Arrange
        var service = CreateService();
        service.RecordPlayerCount(1, 5);
        service.RecordPlayerCount(1, 10);
        service.RecordPlayerCount(2, 8);

        // Act
        var history = service.GetPlayerCountHistory(hours: 1);

        // Assert
        history.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void GetPlayerCountSeries_ShouldReturnChartDataPoints()
    {
        // Arrange
        var service = CreateService();
        service.RecordPlayerCount(1, 5);
        service.RecordPlayerCount(1, 10);

        // Act
        var series = service.GetPlayerCountSeries(hours: 1);

        // Assert
        series.Should().NotBeEmpty();
        series.Should().AllBeOfType<ChartDataPoint>();
    }

    #endregion

    #region Match Statistics Tests

    [Fact]
    public void RecordMatchStart_ShouldTrackActiveMatch()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.RecordMatchStart(1001, serverId: 1, playerCount: 10, gameMode: "Normal", map: "caldavar");

        // Assert - match should be active (not in history yet)
        var history = service.GetMatchHistory();
        // Active matches don't appear in history until ended
    }

    [Fact]
    public void RecordMatchEnd_ShouldCompleteMatchAndAddToHistory()
    {
        // Arrange
        var service = CreateService();
        service.RecordMatchStart(1001, serverId: 1, playerCount: 10, gameMode: "Normal", map: "caldavar");

        // Act
        service.RecordMatchEnd(1001, winner: "Legion");

        // Assert
        var history = service.GetMatchHistory();
        history.Should().HaveCount(1);
        history[0].Winner.Should().Be("Legion");
    }

    [Fact]
    public void GetMatchHistory_ShouldFilterByServerId()
    {
        // Arrange
        var service = CreateService();
        service.RecordMatchStart(1001, serverId: 1, playerCount: 10, gameMode: "Normal", map: "caldavar");
        service.RecordMatchEnd(1001, "Legion");
        service.RecordMatchStart(1002, serverId: 2, playerCount: 8, gameMode: "AP", map: "caldavar");
        service.RecordMatchEnd(1002, "Hellbourne");

        // Act
        var server1History = service.GetMatchHistory(serverId: 1);
        var server2History = service.GetMatchHistory(serverId: 2);

        // Assert
        server1History.Should().HaveCount(1);
        server2History.Should().HaveCount(1);
    }

    [Fact]
    public void GetMatchStatsSummary_ShouldCalculateCorrectStats()
    {
        // Arrange
        var service = CreateService();
        
        service.RecordMatchStart(1001, 1, 10, "Normal", "caldavar");
        service.RecordMatchEnd(1001, "Legion");
        
        service.RecordMatchStart(1002, 1, 10, "Normal", "caldavar");
        service.RecordMatchEnd(1002, "Hellbourne");
        
        service.RecordMatchStart(1003, 1, 10, "AP", "caldavar");
        service.RecordMatchEnd(1003, "Legion");

        // Act
        var summary = service.GetMatchStatsSummary();

        // Assert
        summary.TotalMatches.Should().Be(3);
        summary.LegionWins.Should().Be(2);
        summary.HellbourneWins.Should().Be(1);
        summary.MatchesByGameMode.Should().ContainKey("Normal");
        summary.MatchesByGameMode.Should().ContainKey("AP");
    }

    [Fact]
    public void GetMatchStatsSummary_WithNoMatches_ShouldReturnEmptySummary()
    {
        // Arrange
        var service = CreateService();

        // Act
        var summary = service.GetMatchStatsSummary();

        // Assert
        summary.TotalMatches.Should().Be(0);
        summary.LegionWins.Should().Be(0);
        summary.HellbourneWins.Should().Be(0);
    }

    #endregion

    #region Resource Metrics Tests

    [Fact]
    public void RecordResourceMetrics_ShouldStoreMetrics()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.RecordResourceMetrics(50.0, 60.0, 70.0);

        // Assert
        var cpuSeries = service.GetCpuSeries(hours: 1);
        var memorySeries = service.GetMemorySeries(hours: 1);
        var diskSeries = service.GetDiskSeries(hours: 1);

        cpuSeries.Should().NotBeEmpty();
        memorySeries.Should().NotBeEmpty();
        diskSeries.Should().NotBeEmpty();
    }

    [Fact]
    public void GetCpuSeries_ShouldReturnCorrectValues()
    {
        // Arrange
        var service = CreateService();
        service.RecordResourceMetrics(25.0, 50.0, 75.0);
        service.RecordResourceMetrics(30.0, 55.0, 80.0);

        // Act
        var cpuSeries = service.GetCpuSeries(hours: 1);

        // Assert
        cpuSeries.Should().HaveCount(2);
        cpuSeries[0].Value.Should().Be(25.0);
        cpuSeries[1].Value.Should().Be(30.0);
    }

    [Fact]
    public void GetMemorySeries_ShouldReturnCorrectValues()
    {
        // Arrange
        var service = CreateService();
        service.RecordResourceMetrics(25.0, 50.0, 75.0);

        // Act
        var memorySeries = service.GetMemorySeries(hours: 1);

        // Assert
        memorySeries.Should().HaveCount(1);
        memorySeries[0].Value.Should().Be(50.0);
    }

    [Fact]
    public void GetDiskSeries_ShouldReturnCorrectValues()
    {
        // Arrange
        var service = CreateService();
        service.RecordResourceMetrics(25.0, 50.0, 75.0);

        // Act
        var diskSeries = service.GetDiskSeries(hours: 1);

        // Assert
        diskSeries.Should().HaveCount(1);
        diskSeries[0].Value.Should().Be(75.0);
    }

    [Fact]
    public void ResourceMetrics_ShouldHaveTimestamp()
    {
        // Arrange
        var service = CreateService();
        var before = DateTime.UtcNow;
        service.RecordResourceMetrics(50.0, 60.0, 70.0);
        var after = DateTime.UtcNow;

        // Act
        var cpuSeries = service.GetCpuSeries(hours: 1);

        // Assert
        cpuSeries[0].Timestamp.Should().BeOnOrAfter(before);
        cpuSeries[0].Timestamp.Should().BeOnOrBefore(after);
    }

    #endregion

    #region Data Retention Tests

    [Fact]
    public void RecordMetrics_ShouldTrimOldData()
    {
        // Arrange
        var service = CreateService();

        // Act - record many metrics
        for (int i = 0; i < 5000; i++)
        {
            service.RecordResourceMetrics(i % 100, i % 100, i % 100);
        }

        // Assert - should not exceed max records
        var cpuSeries = service.GetCpuSeries(hours: 24);
        cpuSeries.Count.Should().BeLessThanOrEqualTo(3600); // MaxMetricPoints
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task ConcurrentRecordOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var service = CreateService();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var idx = i;
            tasks.Add(Task.Run(() =>
            {
                service.RecordResourceMetrics(idx, idx, idx);
                service.RecordPlayerCount(idx % 5, idx);
                service.RecordServerStart(idx % 10, $"Server {idx % 10}");
            }));
        }
        await Task.WhenAll(tasks);

        // Assert - should not throw
        var cpuSeries = service.GetCpuSeries(hours: 1);
        cpuSeries.Should().NotBeEmpty();
    }

    #endregion
}
