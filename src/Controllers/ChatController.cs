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
    private IAuthService _authService;
    private IGroupService _groupService;
    private HertaWsGroup _wsGroup = new HertaWsGroup();

    public ChatController(
        IAuthService authService,
        IGroupService groupService
    ) 
    {
        _groupService = groupService;
        _authService = authService;
    }
}