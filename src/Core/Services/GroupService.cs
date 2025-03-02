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
using Herta.Models.Forms.UpdateGroupForm;
using Herta.Utils.Logger;
using NLog;

namespace Herta.Services.GroupService;

[Service]
public class GroupService : IGroupService
{
    private readonly ApplicationDbContext _context;
    private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(GroupService));

    public GroupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool success, int groupId)> CreateGroupAsync(Groups group)
    {
        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                _logger.Trace($"creating group: {group.GroupName}");
                await _context.Groups.AddAsync(group);
                await _context.SaveChangesAsync(); // 确保 group.Id 被分配
                _logger.Trace($"user: {group.OwnerId} is owner of group: {group.Id}");
                var user = await _context.Users.FindAsync(group.OwnerId);
                if (user == null) throw new HttpException(404, "user not found");

                var groupMember = new GroupMembers
                {
                    GroupId = group.Id,
                    UserId = group.OwnerId,
                    RoleIs = GroupRole.OWNER
                };

                await AddMemberToGroupAsync(group.Id, groupMember);
                transaction.Commit();
                return (true, group.Id);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new HttpException(500, "group creation failed", ex);
            }
        }
    }

    public async Task<bool> DeleteGroupAsync(int groupId)
    {
        var group = await _context.Groups.FindAsync(groupId);
        if (group == null) throw new HttpException(404, "group not found");

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
        if (member.GroupId != groupId) throw new HttpException(400, "groupId and member.GroupId do not match");

        var existingMember = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == member.UserId);
        if (existingMember != null) throw new HttpException(400, "member already exists in group");

        await _context.GroupMembers.AddAsync(member);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveMemberFromGroupAsync(int groupId, int userId)
    {
        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        if (member == null) throw new HttpException(404, "member not found in group");

        _context.GroupMembers.Remove(member);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<GroupMembers>> GetAllGroupMembersAsync(int groupId)
    {
        var groupMember = await _context.GroupMembers.Where(gm => gm.GroupId == groupId).ToArrayAsync();
        return groupMember;
    }

    public async Task<bool> UpdateGroupAsync(Groups group)
    {
        var existingGroup = await _context.Groups.FindAsync(group.Id);
        if (existingGroup == null) throw new HttpException(404, "group not found");

        existingGroup.GroupName = group.GroupName;
        existingGroup.Description = group.Description;
        existingGroup.AvatarUrl = group.AvatarUrl;
        existingGroup.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateGroupPartAsync(UpdateGroupForm updates)
    {
        var group = await _context.Groups.FindAsync(updates.GroupId);
        if (group == null) throw new HttpException(404, "group not found");

        var entity = _context.Entry(group);

        if (updates.Fields == null) return true;

        foreach (var (key, value) in updates.Fields)
        {
            var property = entity.Property(key);
            var propertyType = property.Metadata.ClrType;

            if (property != null)
            {
                property.CurrentValue = value.ToString();
            }
            else
            {
                throw new HttpException(400, "invalid field");
            }
        }

        group.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateGroupMemberAsync(int groupId, GroupMembers member)
    {
        var existingMember = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == member.UserId);
        if (existingMember == null) throw new HttpException(404, "member not found in group");
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
        if (member == null) throw new HttpException(404, "member not found in group");

        if (newRole == GroupRole.OWNER && member.RoleIs != GroupRole.OWNER)
            throw new HttpException(400, "only owner can change role to owner");

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
