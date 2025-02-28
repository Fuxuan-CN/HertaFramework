using Herta.Core.Contexts.DBContext;
using Herta.Models.DataModels.Groups;
using Herta.Models.DataModels.GroupMembers;
using Herta.Models.Enums.GroupRole;
using Herta.Models.DataModels.Messages;
using Herta.Models.DataModels.Users;
using Herta.Interfaces.IGroupService;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Herta.Decorators.Services;
using Herta.Exceptions.HttpException;

namespace Herta.Services.GroupService;

[Service(ServiceLifetime.Scoped)]
public class GroupService : IGroupService
{
    private readonly ApplicationDbContext _context;

    public GroupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CreateGroupAsync(Groups group, int whatUserCreatedItId)
    {
        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                await _context.Groups.AddAsync(group);
                await _context.SaveChangesAsync(); // 确保 group.Id 被分配

                var user = await _context.Users.FindAsync(whatUserCreatedItId);
                if (user == null) throw new HttpException(404, "没有找到创建者");

                var groupMember = new GroupMembers
                {
                    GroupId = group.Id,
                    UserId = user.Id,
                    RoleIs = GroupRole.OWNER
                };

                await AddMemberToGroupAsync(group.Id, groupMember);
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new HttpException(500, "创建群组失败", ex);
            }
        }
    }

    public async Task<bool> DeleteGroupAsync(int groupId)
    {
        var group = await _context.Groups.FindAsync(groupId);
        if (group == null) throw new HttpException(404, "没有找到该群聊");

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Groups?> GetGroupAsync(int groupId)
    {
        return await _context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
    }

    public async Task<GroupMembers?> GetGroupMemberAsync(int groupId, int userId)
    {
        return await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
    }

    public async Task<bool> AddMemberToGroupAsync(int groupId, GroupMembers member)
    {
        if (member.GroupId != groupId) throw new HttpException(400, "群组 ID 不匹配");

        var existingMember = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == member.UserId);
        if (existingMember != null) throw new HttpException(400, "成员已存在于群组中");

        await _context.GroupMembers.AddAsync(member);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveMemberFromGroupAsync(int groupId, int userId)
    {
        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        if (member == null) throw new HttpException(404, "没有找到该成员");

        _context.GroupMembers.Remove(member);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<GroupMembers>> GetAllGroupMembersAsync(int groupId)
    {
        var groupMember = await _context.GroupMembers.Where(gm => gm.GroupId == groupId).ToListAsync();
        return groupMember;
    }

    public async Task<bool> UpdateGroupAsync(Groups group)
    {
        var existingGroup = await _context.Groups.FindAsync(group.Id);
        if (existingGroup == null) throw new HttpException(404, "没有找到该群聊");

        existingGroup.GroupName = group.GroupName;
        existingGroup.Description = group.Description;
        existingGroup.AvatarUrl = group.AvatarUrl;
        existingGroup.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateGroupMemberAsync(int groupId, GroupMembers member)
    {
        var existingMember = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == member.UserId);
        if (existingMember == null) throw new HttpException(404, "没有找到该成员");
        _context.Entry(existingMember).CurrentValues.SetValues(member);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateGroupMembersAsync(int groupId, IEnumerable<GroupMembers> members)
    {
        foreach (var member in members)
        {
            var existingMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == member.UserId);
            if (existingMember != null)
            {
                _context.Entry(existingMember).CurrentValues.SetValues(member);
            }
            else
            {
                _context.GroupMembers.Add(member);
            }
        }
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangeMemberRoleAsync(int groupId, int userId, GroupRole newRole)
    {
        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        if (member == null) throw new HttpException(404, "没有找到该成员");

        if (newRole == GroupRole.OWNER && member.RoleIs != GroupRole.OWNER)
            throw new HttpException(400, "只能由群主更改成员为群主角色");

        member.RoleIs = newRole;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangeMembersRoleAsync(int groupId, IEnumerable<(int userId, GroupRole newRole)> membersAndRoles)
    {
        foreach (var (userId, role) in membersAndRoles)
        {
            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
            if (member != null)
            {
                await ChangeMemberRoleAsync(groupId, userId, role);
            }
        }
        await _context.SaveChangesAsync();
        return true;
    }
}
