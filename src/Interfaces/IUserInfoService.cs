
using System.Threading.Tasks;
using Herta.Models.DataModels.UserInfos;
using Herta.Models.Forms.UpdateInfoForm;

namespace Herta.Interfaces.IUserInfoService;

public interface IUserInfoService
{
    Task<UserInfo> GetUserInfoAsync(int userId);
    Task<bool> UpdateUserInfoAsync(UserInfo userInfo);
    Task<bool> PartialUpdateUserInfoAsync(UpdateInfoForm updateInfoForm);
}