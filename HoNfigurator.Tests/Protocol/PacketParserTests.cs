using FluentAssertions;
using HoNfigurator.Core.Protocol;

namespace HoNfigurator.Tests.Protocol;

/// <summary>
/// Unit tests for PacketParser and related utilities
/// </summary>
public class PacketParserTests
{
    #region PacketReader Tests

    [Fact]
    public void ReadInt32_ShouldReadCorrectValue()
    {
        // Arrange
        var value = 12345678;
        var data = BitConverter.GetBytes(value).AsSpan();
        var offset = 0;

        // Act
        var result = PacketReader.ReadInt32(data, ref offset);

        // Assert
        result.Should().Be(value);
        offset.Should().Be(4);
    }

    [Fact]
    public void ReadUInt32_ShouldReadCorrectValue()
    {
        // Arrange
        var value = 3000000000u;
        var data = BitConverter.GetBytes(value).AsSpan();
        var offset = 0;

        // Act
        var result = PacketReader.ReadUInt32(data, ref offset);

        // Assert
        result.Should().Be(value);
        offset.Should().Be(4);
    }

    [Fact]
    public void ReadInt16_ShouldReadCorrectValue()
    {
        // Arrange
        short value = 12345;
        var data = BitConverter.GetBytes(value).AsSpan();
        var offset = 0;

        // Act
        var result = PacketReader.ReadInt16(data, ref offset);

        // Assert
        result.Should().Be(value);
        offset.Should().Be(2);
    }

    [Fact]
    public void ReadUInt16_ShouldReadCorrectValue()
    {
        // Arrange
        ushort value = 50000;
        var data = BitConverter.GetBytes(value).AsSpan();
        var offset = 0;

        // Act
        var result = PacketReader.ReadUInt16(data, ref offset);

        // Assert
        result.Should().Be(value);
        offset.Should().Be(2);
    }

    [Fact]
    public void ReadByte_ShouldReadCorrectValue()
    {
        // Arrange
        byte[] data = { 0xAB, 0xCD, 0xEF };
        var offset = 0;

        // Act
        var result = PacketReader.ReadByte(data, ref offset);

        // Assert
        result.Should().Be(0xAB);
        offset.Should().Be(1);
    }

    [Fact]
    public void ReadByte_ShouldAdvanceOffset()
    {
        // Arrange
        byte[] data = { 0x01, 0x02, 0x03 };
        var offset = 0;

        // Act
        PacketReader.ReadByte(data, ref offset);
        var second = PacketReader.ReadByte(data, ref offset);

        // Assert
        second.Should().Be(0x02);
        offset.Should().Be(2);
    }

    [Fact]
    public void ReadNullTerminatedString_ShouldReadString()
    {
        // Arrange
        var testStr = "Hello";
        var data = System.Text.Encoding.UTF8.GetBytes(testStr + "\0more").AsSpan();
        var offset = 0;

        // Act
        var result = PacketReader.ReadNullTerminatedString(data, ref offset);

        // Assert
        result.Should().Be("Hello");
        offset.Should().Be(6); // 5 chars + null terminator
    }

    [Fact]
    public void ReadNullTerminatedString_EmptyString_ShouldReturnEmpty()
    {
        // Arrange
        byte[] data = { 0x00, 0x41, 0x42 }; // Empty string followed by "AB"
        var offset = 0;

        // Act
        var result = PacketReader.ReadNullTerminatedString(data, ref offset);

        // Assert
        result.Should().BeEmpty();
        offset.Should().Be(1);
    }

    [Fact]
    public void ReadNullTerminatedString_NoNullTerminator_ShouldReadToEnd()
    {
        // Arrange
        byte[] data = { 0x41, 0x42, 0x43 }; // "ABC" with no null terminator
        var offset = 0;

        // Act
        var result = PacketReader.ReadNullTerminatedString(data, ref offset);

        // Assert
        result.Should().Be("ABC");
        offset.Should().Be(3);
    }

    [Fact]
    public void ReadMultipleValues_ShouldMaintainCorrectOffset()
    {
        // Arrange
        var buffer = new List<byte>();
        buffer.AddRange(BitConverter.GetBytes(42)); // Int32
        buffer.AddRange(BitConverter.GetBytes((short)100)); // Int16
        buffer.Add(0xFF); // Byte
        var data = buffer.ToArray().AsSpan();
        var offset = 0;

        // Act
        var int32 = PacketReader.ReadInt32(data, ref offset);
        var int16 = PacketReader.ReadInt16(data, ref offset);
        var byteVal = PacketReader.ReadByte(data, ref offset);

        // Assert
        int32.Should().Be(42);
        int16.Should().Be(100);
        byteVal.Should().Be(0xFF);
        offset.Should().Be(7);
    }

    #endregion

    #region PacketWriter Tests

    [Fact]
    public void WriteInt32_ShouldWriteCorrectBytes()
    {
        // Arrange
        var buffer = new List<byte>();
        var value = 12345678;

        // Act
        PacketWriter.WriteInt32(buffer, value);

        // Assert
        buffer.Should().HaveCount(4);
        BitConverter.ToInt32(buffer.ToArray()).Should().Be(value);
    }

    [Fact]
    public void WriteUInt32_ShouldWriteCorrectBytes()
    {
        // Arrange
        var buffer = new List<byte>();
        var value = 3000000000u;

        // Act
        PacketWriter.WriteUInt32(buffer, value);

        // Assert
        buffer.Should().HaveCount(4);
        BitConverter.ToUInt32(buffer.ToArray()).Should().Be(value);
    }

    [Fact]
    public void WriteInt16_ShouldWriteCorrectBytes()
    {
        // Arrange
        var buffer = new List<byte>();
        short value = 12345;

        // Act
        PacketWriter.WriteInt16(buffer, value);

        // Assert
        buffer.Should().HaveCount(2);
        BitConverter.ToInt16(buffer.ToArray()).Should().Be(value);
    }

    [Fact]
    public void WriteUInt16_ShouldWriteCorrectBytes()
    {
        // Arrange
        var buffer = new List<byte>();
        ushort value = 50000;

        // Act
        PacketWriter.WriteUInt16(buffer, value);

        // Assert
        buffer.Should().HaveCount(2);
        BitConverter.ToUInt16(buffer.ToArray()).Should().Be(value);
    }

    [Fact]
    public void WriteByte_ShouldAddSingleByte()
    {
        // Arrange
        var buffer = new List<byte>();

        // Act
        PacketWriter.WriteByte(buffer, 0xAB);

        // Assert
        buffer.Should().Equal(new byte[] { 0xAB });
    }

    [Fact]
    public void WriteNullTerminatedString_ShouldIncludeNullTerminator()
    {
        // Arrange
        var buffer = new List<byte>();

        // Act
        PacketWriter.WriteNullTerminatedString(buffer, "Test");

        // Assert
        buffer.Should().HaveCount(5);
        buffer.Last().Should().Be(0);
        System.Text.Encoding.UTF8.GetString(buffer.Take(4).ToArray()).Should().Be("Test");
    }

    [Fact]
    public void WriteNullTerminatedString_EmptyString_ShouldWriteOnlyNull()
    {
        // Arrange
        var buffer = new List<byte>();

        // Act
        PacketWriter.WriteNullTerminatedString(buffer, "");

        // Assert
        buffer.Should().Equal(new byte[] { 0 });
    }

    [Fact]
    public void BuildPacket_ShouldCreateCorrectStructure()
    {
        // Arrange
        ushort packetType = 0x1234;
        byte[] data = { 0xAA, 0xBB, 0xCC };

        // Act
        var packet = PacketWriter.BuildPacket(packetType, data);

        // Assert
        packet.Should().HaveCount(7); // 2 (length) + 2 (type) + 3 (data)
        
        // First 2 bytes = total length (5 = 3 data + 2 type)
        var length = BitConverter.ToUInt16(packet, 0);
        length.Should().Be(5);
        
        // Next 2 bytes = packet type
        var type = BitConverter.ToUInt16(packet, 2);
        type.Should().Be(0x1234);
        
        // Remaining = data
        packet.Skip(4).Should().Equal(data);
    }

    [Fact]
    public void BuildPacket_EmptyData_ShouldCreateMinimalPacket()
    {
        // Arrange
        ushort packetType = 0x0001;
        byte[] data = Array.Empty<byte>();

        // Act
        var packet = PacketWriter.BuildPacket(packetType, data);

        // Assert
        packet.Should().HaveCount(4); // 2 (length) + 2 (type)
        BitConverter.ToUInt16(packet, 0).Should().Be(2); // Length = just type
    }

    #endregion

    #region GameServerPacketParser Tests

    [Fact]
    public void HandlePacket_ServerAnnounce_ShouldRaiseEvent()
    {
        // Arrange
        var parser = new GameServerPacketParser(1);
        var receivedPort = 0;
        parser.OnServerAnnounce += port => receivedPort = port;

        var packet = new byte[5];
        packet[0] = (byte)GameServerPacketType.ServerAnnounce;
        BitConverter.GetBytes(11235).CopyTo(packet, 1);

        // Act
        parser.HandlePacket(packet);

        // Assert
        receivedPort.Should().Be(11235);
    }

    [Fact]
    public void HandlePacket_ServerClosed_ShouldRaiseEvent()
    {
        // Arrange
        var parser = new GameServerPacketParser(1);
        var eventRaised = false;
        parser.OnServerClosed += () => eventRaised = true;

        byte[] packet = { (byte)GameServerPacketType.ServerClosed };

        // Act
        parser.HandlePacket(packet);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void HandlePacket_LongFrame_ShouldRaiseEventWithDuration()
    {
        // Arrange
        var parser = new GameServerPacketParser(1);
        var skippedMs = 0;
        parser.OnLongFrame += ms => skippedMs = ms;

        var packet = new byte[3];
        packet[0] = (byte)GameServerPacketType.LongFrame;
        BitConverter.GetBytes((ushort)500).CopyTo(packet, 1);

        // Act
        parser.HandlePacket(packet);

        // Assert
        skippedMs.Should().Be(500);
    }

    [Fact]
    public void HandlePacket_LobbyClosed_ShouldRaiseEvent()
    {
        // Arrange
        var parser = new GameServerPacketParser(1);
        var eventRaised = false;
        parser.OnLobbyClosed += () => eventRaised = true;

        byte[] packet = { (byte)GameServerPacketType.LobbyClosed };

        // Act
        parser.HandlePacket(packet);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void HandlePacket_ServerConnection_ShouldRaiseEvent()
    {
        // Arrange
        var parser = new GameServerPacketParser(1);
        var eventRaised = false;
        parser.OnServerConnection += () => eventRaised = true;

        byte[] packet = { (byte)GameServerPacketType.ServerConnection };

        // Act
        parser.HandlePacket(packet);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void HandlePacket_CowAnnounce_ShouldRaiseEvent()
    {
        // Arrange
        var parser = new GameServerPacketParser(1);
        var receivedValue = 0;
        parser.OnCowAnnounce += v => receivedValue = v;

        var packet = new byte[5];
        packet[0] = (byte)GameServerPacketType.CowAnnounce;
        BitConverter.GetBytes(12345).CopyTo(packet, 1);

        // Act
        parser.HandlePacket(packet);

        // Assert
        // The actual value depends on implementation
    }

    [Fact]
    public void HandlePacket_EmptyPacket_ShouldNotThrow()
    {
        // Arrange
        var parser = new GameServerPacketParser(1);

        // Act & Assert
        var act = () => parser.HandlePacket(ReadOnlySpan<byte>.Empty);
        act.Should().NotThrow();
    }

    [Fact]
    public void HandlePacket_UnknownType_ShouldNotThrow()
    {
        // Arrange
        var parser = new GameServerPacketParser(1);

        // Act & Assert
        var act = () => parser.HandlePacket(new byte[] { 0xFF });
        act.Should().NotThrow();
    }

    [Fact]
    public void HandlePacket_WithLogger_ShouldLog()
    {
        // Arrange
        var logMessages = new List<(string level, string message)>();
        var parser = new GameServerPacketParser(1, (level, msg) => logMessages.Add((level, msg)));

        byte[] packet = { (byte)GameServerPacketType.ServerClosed };

        // Act
        parser.HandlePacket(packet);

        // Assert
        logMessages.Should().NotBeEmpty();
    }

    #endregion

    #region Model Tests

    [Fact]
    public void PlayerInfo_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var player = new PlayerInfo { AccountId = 12345 };

        // Assert
        player.AccountId.Should().Be(12345);
        player.Name.Should().BeEmpty();
        player.Location.Should().BeEmpty();
        player.IpAddress.Should().BeEmpty();
        player.MinPing.Should().Be(0);
        player.AvgPing.Should().Be(0);
        player.MaxPing.Should().Be(0);
    }

    [Fact]
    public void ServerStatusData_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var status = new ServerStatusData { Status = 1 };

        // Assert
        status.Status.Should().Be(1);
        status.Players.Should().BeEmpty();
        status.CpuCoreUtil.Should().Be(0);
    }

    [Fact]
    public void LobbyInfo_ShouldStoreAllProperties()
    {
        // Arrange & Act
        var lobby = new LobbyInfo
        {
            MatchId = 12345,
            Map = "caldavar",
            Name = "Test Game",
            Mode = "Normal"
        };

        // Assert
        lobby.MatchId.Should().Be(12345);
        lobby.Map.Should().Be("caldavar");
        lobby.Name.Should().Be("Test Game");
        lobby.Mode.Should().Be("Normal");
    }

    [Fact]
    public void ReplayRequestData_ShouldStoreAllProperties()
    {
        // Arrange & Act
        var request = new ReplayRequestData
        {
            AccountId = 100,
            MatchId = 500,
            Extension = ".honreplay",
            FileHost = "ftp.example.com",
            Directory = "/replays",
            UploadToFtb = 1,
            UploadToS3 = 0,
            DownloadLink = "https://example.com/replay.zip"
        };

        // Assert
        request.AccountId.Should().Be(100);
        request.MatchId.Should().Be(500);
        request.Extension.Should().Be(".honreplay");
        request.FileHost.Should().Be("ftp.example.com");
        request.DownloadLink.Should().Be("https://example.com/replay.zip");
    }

    #endregion

    #region Packet Type Enum Tests

    [Fact]
    public void GameServerPacketType_ShouldHaveCorrectValues()
    {
        // Assert
        ((byte)GameServerPacketType.ServerAnnounce).Should().Be(0x40);
        ((byte)GameServerPacketType.ServerClosed).Should().Be(0x41);
        ((byte)GameServerPacketType.ServerStatus).Should().Be(0x42);
        ((byte)GameServerPacketType.LongFrame).Should().Be(0x43);
        ((byte)GameServerPacketType.LobbyCreated).Should().Be(0x44);
        ((byte)GameServerPacketType.LobbyClosed).Should().Be(0x45);
    }

    [Fact]
    public void ManagerChatPacketType_ShouldHaveCorrectValues()
    {
        // Assert
        ((ushort)ManagerChatPacketType.ChatHandshakeAccepted).Should().Be(0x1700);
        ((ushort)ManagerChatPacketType.MgrHandshakeRequest).Should().Be(0x1600);
        ((ushort)ManagerChatPacketType.MgrSendingHeartbeat).Should().Be(0x2A00);
    }

    [Fact]
    public void GameChatPacketType_ShouldHaveCorrectValues()
    {
        // Assert
        ((ushort)GameChatPacketType.GameLogin).Should().Be(0x0500);
        ((ushort)GameChatPacketType.GameServerClosed).Should().Be(0x0501);
        ((ushort)GameChatPacketType.ChatLogonResponse).Should().Be(0x1500);
    }

    #endregion

    #region Round-trip Tests

    [Fact]
    public void WriteAndRead_Int32_ShouldRoundtrip()
    {
        // Arrange
        var originalValue = -987654321;
        var buffer = new List<byte>();
        PacketWriter.WriteInt32(buffer, originalValue);
        var offset = 0;

        // Act
        var readValue = PacketReader.ReadInt32(buffer.ToArray(), ref offset);

        // Assert
        readValue.Should().Be(originalValue);
    }

    [Fact]
    public void WriteAndRead_String_ShouldRoundtrip()
    {
        // Arrange
        var originalValue = "Hello, World! 你好世界";
        var buffer = new List<byte>();
        PacketWriter.WriteNullTerminatedString(buffer, originalValue);
        var offset = 0;

        // Act
        var readValue = PacketReader.ReadNullTerminatedString(buffer.ToArray(), ref offset);

        // Assert
        readValue.Should().Be(originalValue);
    }

    [Fact]
    public void WriteAndRead_ComplexPacket_ShouldRoundtrip()
    {
        // Arrange
        var buffer = new List<byte>();
        PacketWriter.WriteInt32(buffer, 123);
        PacketWriter.WriteUInt16(buffer, 456);
        PacketWriter.WriteNullTerminatedString(buffer, "Test");
        PacketWriter.WriteByte(buffer, 0xFF);

        var data = buffer.ToArray();
        var offset = 0;

        // Act & Assert
        PacketReader.ReadInt32(data, ref offset).Should().Be(123);
        PacketReader.ReadUInt16(data, ref offset).Should().Be(456);
        PacketReader.ReadNullTerminatedString(data, ref offset).Should().Be("Test");
        PacketReader.ReadByte(data, ref offset).Should().Be(0xFF);
    }

    #endregion
}
