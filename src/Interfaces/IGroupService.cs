using Herta.Models.DataModels.Groups;
using Herta.Models.DataModels.GroupMembers;
using Herta.Models.Enums.GroupRole;
using Herta.Models.Forms.UpdateGroupForm;

namespace Herta.Interfaces.IGroupService;

public interface IGroupService
{
    Task<(bool success, int groupId)> CreateGroupAsync(Groups group);
    Task<bool> DeleteGroupAsync(int groupId);
    Task<Groups?> GetGroupAsync(int groupId); // 使用可空类型处理群组不存在的情况
    Task<GroupMembers?> GetGroupMemberAsync(int groupId, int userId); // 使用可空类型处理成员不存在的情况
    Task<bool> AddMemberToGroupAsync(int groupId, GroupMembers member);
    Task<bool> RemoveMemberFromGroupAsync(int groupId, int userId);
    Task<IEnumerable<GroupMembers>> GetAllGroupMembersAsync(int groupId);
    Task<bool> UpdateGroupAsync(Groups group);
    Task<bool> UpdateGroupPartAsync(UpdateGroupForm form);
    Task<bool> UpdateGroupMemberAsync(int groupId, GroupMembers member);
    Task<bool> UpdateGroupMembersAsync(int groupId, IEnumerable<GroupMembers> members);
    Task<bool> ChangeMemberRoleAsync(int groupId, int userId, GroupRole newRole);
    Task<bool> ChangeMembersRoleAsync(int groupId, IEnumerable<(int userId, GroupRole newRole)> membersAndRoles);
}
