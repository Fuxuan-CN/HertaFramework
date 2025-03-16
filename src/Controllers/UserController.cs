using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Herta.Responses.BusinessResponse;
using Herta.Models.Enums.BusinessCode;
using Herta.Models.Forms.RegisterUserForm;
using Herta.Models.Forms.LoginUserForm;
using Herta.Models.DataModels.UserInfos;
using Herta.Models.Forms.ChangePasswordForm;
using Herta.Models.Forms.UpdateInfoForm;
using Herta.Models.Forms.DeleteUserForm;
using Herta.Interfaces.IUserService;
using Herta.Interfaces.IAuthService;
using Herta.Interfaces.IUserInfoService;
using Herta.Exceptions.HttpException;
using Herta.Utils.Logger;
using NLog;
using Microsoft.AspNetCore.Authorization;

namespace Herta.Controllers.UserController;

// 用户相关的接口
[ApiController]
[Route("users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IUserInfoService _userInfoService;
    private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(UserController));

    public UserController(IUserService userService, IAuthService authService, IUserInfoService userInfoService)
    {
        _userService = userService;
        _authService = authService;
        _userInfoService = userInfoService;
    }

    private void ValidateData()
    {
        if (!ModelState.IsValid)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, 
            "不正确的输入", ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList());
        }
    }

    [HttpPost("register")]
    public async Task<BusinessResponse<Dictionary<string, object>>> Register([FromBody] RegisterUserForm form)
    {
        _logger.Info($"Registering user {form.Username}");
        ValidateData();
        var user = await _userService.RegisterAsync(form.Username, form.Password);
        var token = await _authService.GenTokenAsync(new Dictionary<string, object> { { "user", user.Username }, { "userId", user.Id } });
        return new BusinessResponse<Dictionary<string, object>>(BusinessCode.Success, "register success", new Dictionary<string, object> { { "username", user.Username }, { "token", token } });
    }

    [HttpPost("login")]
    public async Task<BusinessResponse<Dictionary<string, object>>> Login([FromBody] LoginUserForm form)
    {
        _logger.Info($"Logging in user {form.Username}");
        ValidateData();
        var user = await _userService.LoginAsync(form.Username, form.Password);
        var token = await _authService.GenTokenAsync(new Dictionary<string, object> { { "user", user.Username }, { "userId", user.Id } });
        return new BusinessResponse<Dictionary<string, object>>(BusinessCode.Success, "login success", new Dictionary<string, object> { { "username", user.Username }, { "token", token } });
    }

    [HttpPost("change")]
    public async Task<BusinessResponse<object>> ChangePassword([FromBody] ChangePasswordForm form)
    {
        _logger.Info($"Changing password for user {form.Username}");
        ValidateData();
        await _userService.ChangePasswordAsync(form.Username, form.OldPassword, form.NewPassword);
        return new BusinessResponse<object>(BusinessCode.Success, "password changed success");
    }

    [HttpDelete("delete")]
    public async Task<BusinessResponse<object>> DeleteUser([FromBody] DeleteUserForm deleteUsrForm)
    {
        _logger.Info($"Deleting user {deleteUsrForm.Username}");
        ValidateData();
        await _userService.DeleteUserAsync(deleteUsrForm);
        return new BusinessResponse<object>(BusinessCode.Success, "user deleted success");
    }

    [HttpPut("update/info")]
    [Authorize(Policy = "JwtAuth")]
    public async Task<BusinessResponse<object>> UpdateUserInfo([FromBody] UserInfo userInfo)
    {
        _logger.Info($"Updating user info");
        ValidateData();
        var vialed = await _authService.ValidateUserAsync(userInfo.UserId);
        if (!vialed)
        {
            throw new HttpException(StatusCodes.Status403Forbidden, "unauthorized access.");
        }
        await _userInfoService.UpdateUserInfoAsync(userInfo);
        return new BusinessResponse<object>(BusinessCode.Success, "user info updated success");
    }

    [HttpPatch("update/info")]
    [Authorize(Policy = "JwtAuth")]
    public async Task<BusinessResponse<object>> PartialUpdateUserInfo([FromBody] UpdateInfoForm userInfo)
    {
        _logger.Info($"Partial updating user info.");
        ValidateData();
        var vialed = await _authService.ValidateUserAsync(userInfo.UserId);
        if (!vialed)
        {
            throw new HttpException(StatusCodes.Status403Forbidden, "unauthorized access.");
        }
        await _userInfoService.PartialUpdateUserInfoAsync(userInfo);
        return new BusinessResponse<object>(BusinessCode.Success, "user info updated success");
    }

    [HttpGet("get/info/{userId:int}")]
    public async Task<BusinessResponse<UserInfo>> GetUserInfo(int userId)
    {
        _logger.Info($"Getting user info for user {userId}");
        var userInfo = await _userInfoService.GetUserInfoAsync(userId);
        return new BusinessResponse<UserInfo>(BusinessCode.Success, "user info retrieved success", userInfo);
    }
}
