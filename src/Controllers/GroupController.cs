using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Herta.Responses.BusinessResponse;
using Herta.Models.Enums.BusinessCode;
using Herta.Exceptions.HttpException;
using Herta.Interfaces.IGroupService;
using Herta.Interfaces.IAuthService;
using Herta.Models.DataModels.Groups;
using Herta.Models.DataModels.GroupMembers;
using Herta.Models.Enums.GroupRole;
using Herta.Models.Forms.CreateGroupForm;
using Herta.Models.Forms.JoinGroupForm;
using Herta.Models.Forms.UpdateGroupForm;
using Herta.Models.Forms.LeaveGroupForm;
using Herta.Utils.WebsocketGroup;
using Herta.Utils.Logger;
using NLog;

namespace Herta.Controllers.GroupController;

[ApiController]
[Route("groups")]
public class GroupController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly IAuthService _authService;
    private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(GroupController));

    public GroupController(IGroupService groupService, IAuthService authService)
    {
        _groupService = groupService;
        _authService = authService;
    }

    private async Task AllowAccess(int userId)
    {
        if (!await _authService.ValidateUserAsync(userId))
        {
            throw new HttpException(403, "not allowed unauthorized access");
        }
    }

    [HttpGet("{groupId}")]
    public async Task<BusinessResponse<object>> GetGroup(int groupId)
    {
        _logger.Info($"Getting group {groupId}");
        var group = await _groupService.GetGroupAsync(groupId);
        if (group == null)
        {
            throw new HttpException(404, "group not found");
        }
        return new BusinessResponse<object>(BusinessCode.Success, "group found", group);
    }

    [HttpPost]
    [Authorize(Policy = "JwtAuth")]
    public async Task<BusinessResponse<object>> CreateGroup([FromBody] CreateGroupForm form)
    {
        _logger.Info($"Creating group {form.GroupName}");
        await AllowAccess(form.OwnerId);
        var group = new Groups
        {
            OwnerId = form.OwnerId,
            GroupName = form.GroupName,
            Description = form.Description,
            AvatarUrl = form.AvatarUrl,
        };
        var result = await _groupService.CreateGroupAsync(group);
        return new BusinessResponse<object>(BusinessCode.Success, "group created", new { groupId = result.groupId });
    }

    [HttpPut]
    [Authorize(Policy = "JwtAuth")]
    public async Task<BusinessResponse<object>> UpdateGroup([FromBody] Groups group)
    {
        _logger.Info($"Updating group {group.Id}");
        await AllowAccess(group.OwnerId);
        await _groupService.UpdateGroupAsync(group);
        return new BusinessResponse<object>(BusinessCode.Success, "group updated");
    }

    [HttpPatch]
    [Authorize(Policy = "JwtAuth")]
    public async Task<BusinessResponse<object>> UpdateGroupPart([FromBody] UpdateGroupForm form)
    {
        _logger.Info($"Updating part of group {form.GroupId}");
        await AllowAccess(form.OwnerId);
        var group = await _groupService.GetGroupAsync(form.GroupId);
        if (group == null)
        {
            throw new HttpException(404, "group not found");
        }
        var IsSuccess = await _groupService.UpdateGroupPartAsync(form);
        if (IsSuccess)
        {
            return new BusinessResponse<object>(BusinessCode.Success, "group updated");
        }
        else
        {
            throw new HttpException(500, "group update failed, try again later");
        }
    }

    [HttpDelete]
    [Authorize(Policy = "JwtAuth")]
    public async Task<BusinessResponse<object>> DeleteGroup([FromQuery] int groupId, [FromQuery] int userId)
    {
        _logger.Info($"Deleting group {groupId}");
        await AllowAccess(userId);
        var group = await _groupService.GetGroupAsync(groupId);
        if (group == null)
        {
            throw new HttpException(404, "group not found");
        }
        await _groupService.DeleteGroupAsync(groupId);
        return new BusinessResponse<object>(BusinessCode.Success, "group deleted");
    }

    [HttpPut("member")]
    [Authorize(Policy = "JwtAuth")]
    public async Task<BusinessResponse<object>> JoinGroup([FromBody] JoinGroupForm form)
    {
        await AllowAccess(form.UserId);
        _logger.Info($"Joining group {form.GroupId}");
        var group = await _groupService.GetGroupAsync(form.GroupId);
        if (group == null)
        {
            throw new HttpException(404, "group not found");
        }
        var member = new GroupMembers
        {
            GroupId = group.Id,
            UserId = form.UserId,
            RoleIs = GroupRole.MEMBER,
        };
        await _groupService.AddMemberToGroupAsync(form.GroupId, member);
        
        return new BusinessResponse<object>(BusinessCode.Success, "join group success", new { groupId = group.Id });
    }

    [HttpDelete("member")]
    [Authorize(Policy = "JwtAuth")]
    public async Task<BusinessResponse<object>> LeaveGroupFromMember([FromBody] LeaveGroupForm form)
    {
        await AllowAccess(form.UserId);
        _logger.Info($"Leaving group {form.GroupId} from member {form.UserId}");
        var member = await _groupService.GetGroupMemberAsync(form.GroupId, form.UserId);
        if (member == null)
        {
            throw new HttpException(404, "member not found");
        }
        await _groupService.RemoveMemberFromGroupAsync(form.GroupId, form.UserId);
        return new BusinessResponse<object>(BusinessCode.Success, "leave group success");
    }
}