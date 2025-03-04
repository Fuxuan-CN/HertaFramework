using Microsoft.AspNetCore.Mvc;
using System;
using Herta.Utils.HertaWebsocketUtil;
using Herta.Utils.WebsocketGroup;
using Herta.Exceptions.HttpException;
using Herta.Utils.Logger;
using Herta.Models.DataModels.GroupMembers;
using Herta.Interfaces.IGroupService;
using Herta.Interfaces.IHertaWsGroup;
using Herta.Decorators.Websocket;
using NLog;

namespace Herta.Controllers.ChatController;

[ApiController]
[Route("group/chat")]
public class ChatController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly IHertaWsGroup _wsGroup;
    private static readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(ChatController));

    public ChatController(IGroupService groupService, IHertaWsGroup wsGroup) 
    {
        _groupService = groupService;
        _wsGroup = wsGroup;
        _logger.Info("ChatController created.");
    }

    [Websocket("{groupId}")]
    public async Task groupChat(HertaWebsocket ws)
    {
        if (ws.Parameters.GetValueOrDefault("userId", null) == null)
        {
            await ws.CloseAsync(1008, "parameter userId missing.");
            return;
        }
        int groupId = int.Parse(ws.Parameters.GetValueOrDefault("groupId", "0")!);
        int userId = int.Parse(ws.Parameters.GetValueOrDefault("userId", "0")!);

        _logger.Debug($"User {userId} trying to connect to group {groupId}.");

        GroupMembers? member = await _groupService.GetGroupMemberAsync(groupId, userId);

        if (member == null)
        {
            await ws.CloseAsync(1008, "user not in group.");
            return;
        }

        _wsGroup.AddToGroup(groupId, ws);

        try
        {
            while (ws.IsConnected())
            {
                var message = await ws.ReceiveTextAsync();
                _logger.Info($"Received message: {message}");
                await BroadcastMessageAsync(groupId, message);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "WebSocket error.");
        }
        finally
        {
            _wsGroup.RemoveFromGroup(groupId, ws);
        }
    }

    private async Task BroadcastMessageAsync(int groupId, string message)
    {
        await _wsGroup.BroadcastTextAsync(groupId, message);
    }
}