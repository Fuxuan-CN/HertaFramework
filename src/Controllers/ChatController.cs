using System;
using Herta.Utils.HertaWebsocketUtil;
using Herta.Utils.WebsocketGroup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Herta.Exceptions.HttpException;
using Herta.Utils.Logger;
using Herta.Interfaces.IAuthService;
using Herta.Interfaces.IGroupService;
using Herta.Decorators.Websocket;
using NLog;

namespace Herta.Controllers.ChatController;

[ApiController]
[Route("chat")]
public class ChatController
{
    private static readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(ChatController));
    private readonly IAuthService _authService;
    private readonly IGroupService _groupService;
    private HertaWsGroup _wsGroup = new HertaWsGroup();

    public ChatController(IGroupService groupService, IAuthService authService) 
    {
        _groupService = groupService;
        _authService = authService;
    }

    [Websocket("{groupId}")]
    public async Task Chat(HertaWebsocket ws)
    {
        if (ws.Parameters.GetValueOrDefault("userId", null) == null)
        {
            await ws.CloseAsync(1008, "缺少userId参数.");
            return;
        }

        int groupId = int.Parse(ws.Parameters.GetValueOrDefault("groupId")!);
        int userId = int.Parse(ws.Parameters.GetValueOrDefault("userId")!);
        _logger.Debug($"User {userId} trying to join group {groupId}.");
        Dictionary<string, object?> ConnMeta = new Dictionary<string, object?>
        {
            { "userId", userId },
            { "groupId", groupId }
        };
        ws.Metadata = ConnMeta;
        var member = await _groupService.GetGroupMemberAsync(groupId, userId);
        if (member == null)
        {
            await ws.CloseAsync(1008, "用户不在该群中.");
        }
        _wsGroup.AddToGroup(groupId, ws);
        _logger.Debug($"User {userId} joined group {groupId}.");

        ws.OnTextReceivedAsync += async (text) =>
        {
            _logger.Debug($"Received message from {userId}: {text}");
            await _wsGroup.BroadcastTextAsync(groupId, text);
        };

        ws.OnClosed += (sender, e) =>
        {
            _logger.Debug($"User {userId} left group {groupId}.");
            _wsGroup.RemoveFromGroup(groupId, ws);
        };
    }
}