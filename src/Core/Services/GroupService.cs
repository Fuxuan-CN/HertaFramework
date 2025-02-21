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

namespace Herta.Services.GroupService
{
    [Service(ServiceLifetime.Scoped)]
    public class GroupService : IGroupService
    {
        private readonly ApplicationDbContext _context;

        public GroupService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CreateGroupAsync(Groups group)
        {
            await _context.Groups.AddAsync(group);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteGroupAsync(int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null) return false;

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
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null) return false;

            member.GroupId = groupId;
            await _context.GroupMembers.AddAsync(member);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveMemberFromGroupAsync(int groupId, int userId)
        {
            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
            if (member == null) return false;

            _context.GroupMembers.Remove(member);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<GroupMembers>> GetAllGroupMembersAsync(int groupId)
        {
            return await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId)
                .ToListAsync();
        }

        public async Task<bool> UpdateGroupAsync(Groups group)
        {
            var existingGroup = await _context.Groups.FindAsync(group.Id);
            if (existingGroup == null) return false;

            _context.Entry(existingGroup).CurrentValues.SetValues(group);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateGroupMemberAsync(int groupId, GroupMembers member)
        {
            var existingMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == member.UserId);
            if (existingMember == null) return false;

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

        public async Task<bool> ChangeMemberRoleAsync(int groupId, int userId, GroupRole role)
        {
            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
            if (member == null) return false;

            member.RoleIs = role;
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
                    member.RoleIs = role;
                }
            }
            await _context.SaveChangesAsync();
            return true;
        }
    }
}