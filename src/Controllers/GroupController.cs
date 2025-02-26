using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Herta.Responses.Response;
using Herta.Exceptions.HttpException;
using Herta.Interfaces.IGroupService;
using Herta.Models.DataModels.Groups;
using Herta.Models.DataModels.GroupMembers;
using Herta.Models.Enums.GroupRole;
using Herta.Models.Forms.CreatGroupForm;
using Herta.Models.Forms.JoinGroupForm;
using Herta.Utils.WebsocketGroup;
using Herta.Utils.Logger;
using NLog;

namespace Herta.Controllers.GroupController
{
    [ApiController]
    [Route("api/group")]
    public class GroupController
    {
        private readonly IGroupService _groupService;
        private readonly HertaWsGroup _wsGroup = new HertaWsGroup();
        private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(GroupController));

        public GroupController(IGroupService groupService)
        {
            _groupService = groupService;
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
            return new Response(new { message = "群组创建成功" });
        }

        [HttpPost("join")]
        [Authorize(Policy = "JwtAuth")]
        public async Task<Response> JoinGroup([FromBody] JoinGroupForm form)
        {
            _logger.Info($"Joining group {form.GroupId}");
            var group = await _groupService.GetGroupAsync(form.GroupId);
            if (group == null)
            {
                throw new HttpException(404, "Group not found");
            }
            var member = new GroupMembers
            {
                GroupId = group.Id,
                UserId = form.UserId,
                RoleIs = GroupRole.MEMBER,
            };
            await _groupService.AddMemberToGroupAsync(form.GroupId, member);
            
            return new Response(new { message = "加入群组成功", GroupId = group.Id });
        }
    }
}