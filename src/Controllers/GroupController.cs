using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Herta.Responses.Response;
using Herta.Exceptions.HttpException;
using Herta.Interfaces.IGroupService;
using Herta.Interfaces.IAuthService;
using Herta.Models.DataModels.Groups;
using Herta.Models.DataModels.GroupMembers;
using Herta.Models.Enums.GroupRole;
using Herta.Models.Forms.CreatGroupForm;
using Herta.Models.Forms.JoinGroupForm;
using Herta.Utils.WebsocketGroup;
using Herta.Utils.Logger;
using NLog;

namespace Herta.Controllers.GroupController;

[ApiController]
[Route("api/group")]
public class GroupController
{
    private readonly IGroupService _groupService;
    private readonly IAuthService _authService;
    private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(GroupController));

    public GroupController(IGroupService groupService, IAuthService authService)
    {
        _groupService = groupService;
        _authService = authService;
    }

    [HttpPost("create")]
    [Authorize(Policy = "JwtAuth")]
    public async Task<Response> CreateGroup([FromBody] CreateGroupForm form)
    {
        _logger.Info($"Creating group {form.GroupName}");
        var group = new Groups
        {
            GroupName = form.GroupName,
            Description = form.Description,
            AvatarUrl = form.AvatarUrl,
        };
        var result = await _groupService.CreateGroupAsync(group, form.WhatUserCreatedId);
        return new Response(new { message = "群创建成功", GroupId = result.groupId });
    }

    [HttpPut("member")]
    [Authorize(Policy = "JwtAuth")]
    public async Task<Response> JoinGroup([FromBody] JoinGroupForm form)
    {
        var vailed = await _authService.ValidateUserAsync(form.UserId.ToString());
        if (!vailed)
        {
            throw new HttpException(403, "不允许非当前用户加群");
        }
        _logger.Info($"Joining group {form.GroupId}");
        var group = await _groupService.GetGroupAsync(form.GroupId);
        if (group == null)
        {
            throw new HttpException(404, "没有找到群");
        }
        var member = new GroupMembers
        {
            GroupId = group.Id,
            UserId = form.UserId,
            RoleIs = GroupRole.MEMBER,
        };
        await _groupService.AddMemberToGroupAsync(form.GroupId, member);
        
        return new Response(new { message = "加群成功", GroupId = group.Id });
    }

    [HttpDelete("member")]
    [Authorize(Policy = "JwtAuth")]
    public async Task<Response> LeaveGroupFromMember([FromQuery] int groupId, [FromQuery] int userId)
    {
        var vailed = await _authService.ValidateUserAsync(userId.ToString());
        if (!vailed)
        {
            throw new HttpException(403, "不允许非当前用户离开群");
        }
        _logger.Info($"Leaving group {groupId} from member {userId}");
        var member = await _groupService.GetGroupMemberAsync(groupId, userId);
        if (member == null)
        {
            throw new HttpException(404, "没有找到群成员");
        }
        await _groupService.RemoveMemberFromGroupAsync(groupId, userId);
        return new Response(new { message = "退群成功" });
    }
}