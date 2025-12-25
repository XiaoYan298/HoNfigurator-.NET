using FluentAssertions;
using HoNfigurator.Core.Protocol;

namespace HoNfigurator.Tests.Protocol;

/// <summary>
/// Unit tests for ChatProtocol constants
/// </summary>
public class ChatProtocolConstantsTests
{
    #region Version Constants Tests

    [Fact]
    public void ExternalVersion_ShouldBeExpectedValue()
    {
        // Assert
        ChatProtocol.CHAT_PROTOCOL_EXTERNAL_VERSION.Should().Be(68);
    }

    [Fact]
    public void InternalVersion_ShouldBeExpectedValue()
    {
        // Assert
        ChatProtocol.CHAT_PROTOCOL_INTERNAL_VERSION.Should().Be(69);
    }

    [Fact]
    public void InternalVersion_ShouldBeGreaterOrEqualToExternal()
    {
        // Assert
        ChatProtocol.CHAT_PROTOCOL_INTERNAL_VERSION.Should()
            .BeGreaterThanOrEqualTo(ChatProtocol.CHAT_PROTOCOL_EXTERNAL_VERSION);
    }

    #endregion

    #region Command Constants Tests

    [Fact]
    public void Command_ChannelMsg_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_CHANNEL_MSG.Should().Be(0x0003);
    }

    [Fact]
    public void Command_ChangedChannel_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_CHANGED_CHANNEL.Should().Be(0x0004);
    }

    [Fact]
    public void Command_JoinedChannel_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_JOINED_CHANNEL.Should().Be(0x0005);
    }

    [Fact]
    public void Command_LeftChannel_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_LEFT_CHANNEL.Should().Be(0x0006);
    }

    [Fact]
    public void Command_Disconnected_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_DISCONNECTED.Should().Be(0x0007);
    }

    [Fact]
    public void Command_Whisper_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_WHISPER.Should().Be(0x0008);
    }

    [Fact]
    public void Command_WhisperFailed_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_WHISPER_FAILED.Should().Be(0x0009);
    }

    [Fact]
    public void Command_InitialStatus_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_INITIAL_STATUS.Should().Be(0x000B);
    }

    [Fact]
    public void Command_UpdateStatus_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_UPDATE_STATUS.Should().Be(0x000C);
    }

    [Fact]
    public void Command_JoiningGame_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_JOINING_GAME.Should().Be(0x000F);
    }

    [Fact]
    public void Command_JoinedGame_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_JOINED_GAME.Should().Be(0x0010);
    }

    [Fact]
    public void Command_LeftGame_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_LEFT_GAME.Should().Be(0x0011);
    }

    [Fact]
    public void Command_ClanWhisper_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_CLAN_WHISPER.Should().Be(0x0013);
    }

    [Fact]
    public void Command_Flooding_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_FLOODING.Should().Be(0x001B);
    }

    [Fact]
    public void Command_JoinChannel_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_JOIN_CHANNEL.Should().Be(0x001E);
    }

    [Fact]
    public void Command_LeaveChannel_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_LEAVE_CHANNEL.Should().Be(0x0022);
    }

    [Fact]
    public void Command_PlayerCount_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_PLAYER_COUNT.Should().Be(0x0068);
    }

    [Fact]
    public void Command_Logout_ShouldHaveCorrectValue()
    {
        ChatProtocol.Command.CHAT_CMD_LOGOUT.Should().Be(0x00C1);
    }

    #endregion

    #region Bidirectional Constants Tests

    [Fact]
    public void Bidirectional_Ping_ShouldHaveCorrectValue()
    {
        ChatProtocol.Bidirectional.NET_CHAT_PING.Should().Be(0x2A00);
    }

    [Fact]
    public void Bidirectional_Pong_ShouldHaveCorrectValue()
    {
        ChatProtocol.Bidirectional.NET_CHAT_PONG.Should().Be(0x2A01);
    }

    [Fact]
    public void Bidirectional_PingPong_ShouldBeConsecutive()
    {
        var ping = ChatProtocol.Bidirectional.NET_CHAT_PING;
        var pong = ChatProtocol.Bidirectional.NET_CHAT_PONG;
        
        (pong - ping).Should().Be(1);
    }

    #endregion

    #region ClientToChatServer Constants Tests

    [Fact]
    public void ClientToChatServer_Connect_ShouldHaveCorrectValue()
    {
        ChatProtocol.ClientToChatServer.NET_CHAT_CL_CONNECT.Should().Be(0x0C00);
    }

    [Fact]
    public void ClientToChatServer_GetChannelList_ShouldHaveCorrectValue()
    {
        ChatProtocol.ClientToChatServer.NET_CHAT_CL_GET_CHANNEL_LIST.Should().Be(0x0C01);
    }

    [Fact]
    public void ClientToChatServer_GetUserStatus_ShouldHaveCorrectValue()
    {
        ChatProtocol.ClientToChatServer.NET_CHAT_CL_GET_USER_STATUS.Should().Be(0x0C05);
    }

    [Fact]
    public void ClientToChatServer_AdminKick_ShouldHaveCorrectValue()
    {
        ChatProtocol.ClientToChatServer.NET_CHAT_CL_ADMIN_KICK.Should().Be(0x0C08);
    }

    #endregion

    #region ChatServerToClient Constants Tests

    [Fact]
    public void ChatServerToClient_Accept_ShouldHaveCorrectValue()
    {
        ChatProtocol.ChatServerToClient.NET_CHAT_CL_ACCEPT.Should().Be(0x1C00);
    }

    [Fact]
    public void ChatServerToClient_Reject_ShouldHaveCorrectValue()
    {
        ChatProtocol.ChatServerToClient.NET_CHAT_CL_REJECT.Should().Be(0x1C01);
    }

    [Fact]
    public void ChatServerToClient_ChannelInfo_ShouldHaveCorrectValue()
    {
        ChatProtocol.ChatServerToClient.NET_CHAT_CL_CHANNEL_INFO.Should().Be(0x1C02);
    }

    [Fact]
    public void ChatServerToClient_GameLobbyJoined_ShouldHaveCorrectValue()
    {
        ChatProtocol.ChatServerToClient.NET_CHAT_CL_GAME_LOBBY_JOINED.Should().Be(0x1C09);
    }

    #endregion

    #region GameServerToChatServer Constants Tests

    [Fact]
    public void GameServerToChatServer_Connect_ShouldHaveCorrectValue()
    {
        ChatProtocol.GameServerToChatServer.NET_CHAT_GS_CONNECT.Should().Be(0x0500);
    }

    [Fact]
    public void GameServerToChatServer_Disconnect_ShouldHaveCorrectValue()
    {
        ChatProtocol.GameServerToChatServer.NET_CHAT_GS_DISCONNECT.Should().Be(0x0501);
    }

    [Fact]
    public void GameServerToChatServer_Status_ShouldHaveCorrectValue()
    {
        ChatProtocol.GameServerToChatServer.NET_CHAT_GS_STATUS.Should().Be(0x0502);
    }

    [Fact]
    public void GameServerToChatServer_AnnounceMatch_ShouldHaveCorrectValue()
    {
        ChatProtocol.GameServerToChatServer.NET_CHAT_GS_ANNOUNCE_MATCH.Should().Be(0x0503);
    }

    [Fact]
    public void GameServerToChatServer_AbandonMatch_ShouldHaveCorrectValue()
    {
        ChatProtocol.GameServerToChatServer.NET_CHAT_GS_ABANDON_MATCH.Should().Be(0x0504);
    }

    [Fact]
    public void GameServerToChatServer_MatchStarted_ShouldHaveCorrectValue()
    {
        ChatProtocol.GameServerToChatServer.NET_CHAT_GS_MATCH_STARTED.Should().Be(0x0505);
    }

    [Fact]
    public void GameServerToChatServer_MatchAborted_ShouldHaveCorrectValue()
    {
        ChatProtocol.GameServerToChatServer.NET_CHAT_GS_MATCH_ABORTED.Should().Be(0x0509);
    }

    [Fact]
    public void GameServerToChatServer_MatchEnded_ShouldHaveCorrectValue()
    {
        ChatProtocol.GameServerToChatServer.NET_CHAT_GS_MATCH_ENDED.Should().Be(0x0515);
    }

    #endregion

    #region ChatServerToGameServer Constants Tests

    [Fact]
    public void ChatServerToGameServer_Accept_ShouldHaveCorrectValue()
    {
        ChatProtocol.ChatServerToGameServer.NET_CHAT_GS_ACCEPT.Should().Be(0x1500);
    }

    [Fact]
    public void ChatServerToGameServer_Reject_ShouldHaveCorrectValue()
    {
        ChatProtocol.ChatServerToGameServer.NET_CHAT_GS_REJECT.Should().Be(0x1501);
    }

    [Fact]
    public void ChatServerToGameServer_CreateMatch_ShouldHaveCorrectValue()
    {
        ChatProtocol.ChatServerToGameServer.NET_CHAT_GS_CREATE_MATCH.Should().Be(0x1502);
    }

    [Fact]
    public void ChatServerToGameServer_EndMatch_ShouldHaveCorrectValue()
    {
        ChatProtocol.ChatServerToGameServer.NET_CHAT_GS_END_MATCH.Should().Be(0x1503);
    }

    [Fact]
    public void ChatServerToGameServer_RemoteCommand_ShouldHaveCorrectValue()
    {
        ChatProtocol.ChatServerToGameServer.NET_CHAT_GS_REMOTE_COMMAND.Should().Be(0x1504);
    }

    #endregion

    #region ServerManagerToChatServer Constants Tests

    [Fact]
    public void ServerManagerToChatServer_Connect_ShouldHaveCorrectValue()
    {
        ChatProtocol.ServerManagerToChatServer.NET_CHAT_SM_CONNECT.Should().Be(0x1600);
    }

    [Fact]
    public void ServerManagerToChatServer_Disconnect_ShouldHaveCorrectValue()
    {
        ChatProtocol.ServerManagerToChatServer.NET_CHAT_SM_DISCONNECT.Should().Be(0x1601);
    }

    [Fact]
    public void ServerManagerToChatServer_Status_ShouldHaveCorrectValue()
    {
        ChatProtocol.ServerManagerToChatServer.NET_CHAT_SM_STATUS.Should().Be(0x1602);
    }

    #endregion

    #region Protocol Range Tests

    [Fact]
    public void ClientToChatServer_ShouldBeIn0x0CxxRange()
    {
        // All client->chat constants should be in 0x0Cxx range
        ChatProtocol.ClientToChatServer.NET_CHAT_CL_CONNECT.Should().BeInRange(0x0C00, 0x0CFF);
        ChatProtocol.ClientToChatServer.NET_CHAT_CL_GET_CHANNEL_LIST.Should().BeInRange(0x0C00, 0x0CFF);
        ChatProtocol.ClientToChatServer.NET_CHAT_CL_ADMIN_KICK.Should().BeInRange(0x0C00, 0x0CFF);
    }

    [Fact]
    public void ChatServerToClient_ShouldBeIn0x1CxxRange()
    {
        // All chat->client constants should be in 0x1Cxx range
        ChatProtocol.ChatServerToClient.NET_CHAT_CL_ACCEPT.Should().BeInRange(0x1C00, 0x1CFF);
        ChatProtocol.ChatServerToClient.NET_CHAT_CL_REJECT.Should().BeInRange(0x1C00, 0x1CFF);
    }

    [Fact]
    public void GameServerToChatServer_ShouldBeIn0x05xxRange()
    {
        // All gameserver->chat constants should be in 0x05xx range
        ChatProtocol.GameServerToChatServer.NET_CHAT_GS_CONNECT.Should().BeInRange(0x0500, 0x05FF);
        ChatProtocol.GameServerToChatServer.NET_CHAT_GS_MATCH_ENDED.Should().BeInRange(0x0500, 0x05FF);
    }

    [Fact]
    public void ChatServerToGameServer_ShouldBeIn0x15xxRange()
    {
        // All chat->gameserver constants should be in 0x15xx range
        ChatProtocol.ChatServerToGameServer.NET_CHAT_GS_ACCEPT.Should().BeInRange(0x1500, 0x15FF);
        ChatProtocol.ChatServerToGameServer.NET_CHAT_GS_OPTIONS.Should().BeInRange(0x1500, 0x15FF);
    }

    [Fact]
    public void ServerManagerToChatServer_ShouldBeIn0x16xxRange()
    {
        // All servermanager->chat constants should be in 0x16xx range
        ChatProtocol.ServerManagerToChatServer.NET_CHAT_SM_CONNECT.Should().BeInRange(0x1600, 0x16FF);
    }

    #endregion

    #region Consistency Tests

    [Fact]
    public void AllConstants_ShouldBeUnique()
    {
        // Collect all constants into a list
        var constants = new List<ushort>
        {
            ChatProtocol.Command.CHAT_CMD_CHANNEL_MSG,
            ChatProtocol.Command.CHAT_CMD_CHANGED_CHANNEL,
            ChatProtocol.Command.CHAT_CMD_JOINED_CHANNEL,
            ChatProtocol.Command.CHAT_CMD_LEFT_CHANNEL,
            ChatProtocol.Command.CHAT_CMD_DISCONNECTED,
            ChatProtocol.Command.CHAT_CMD_WHISPER,
            ChatProtocol.Command.CHAT_CMD_WHISPER_FAILED,
            ChatProtocol.Bidirectional.NET_CHAT_PING,
            ChatProtocol.Bidirectional.NET_CHAT_PONG,
        };

        // Assert all are unique
        constants.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void RequestResponse_ShouldFollowPattern()
    {
        // Client request starts with 0x0C, response with 0x1C
        var clientRequest = ChatProtocol.ClientToChatServer.NET_CHAT_CL_CONNECT;
        var clientResponse = ChatProtocol.ChatServerToClient.NET_CHAT_CL_ACCEPT;
        
        ((clientRequest >> 8) & 0xFF).Should().Be(0x0C);
        ((clientResponse >> 8) & 0xFF).Should().Be(0x1C);
    }

    #endregion
}
