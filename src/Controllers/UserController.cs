using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Herta.Responses.Response;
using Herta.Models.Forms.RegisterUserForm;
using Herta.Models.Forms.LoginUserForm;
using Herta.Models.Forms.ChangePasswordForm;
using Herta.Interfaces.IUserService;
using Herta.Interfaces.IAuthService;
using Herta.Exceptions.HttpException;
using Herta.Utils.Logger;
using NLog;
using Microsoft.AspNetCore.Authorization;

namespace Herta.Controllers.UserController
{
    // 用户相关的接口
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(UserController));

        public UserController(IUserService userService, IAuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<Response> Register([FromBody] RegisterUserForm form)
        {
            _logger.Info($"Registering user {form.Username}");
            if (!ModelState.IsValid)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Invalid input", ModelState.Values
                                                                                     .SelectMany(v => v.Errors)
                                                                                     .Select(e => e.ErrorMessage)
                                                                                     .ToList());
            }

            var user = await _userService.RegisterAsync(form.Username, form.Password, form.Email);
            var token = await _authService.GenTokenAsync(new Dictionary<string, object> { { "user", user.Username } });
            return new Response(new { message = "Registration successful", token = token });
        }

        [HttpPost("login")]
        public async Task<Response> Login([FromBody] LoginUserForm form)
        {
            _logger.Info($"Logging in user {form.Username}");
            if (!ModelState.IsValid)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Invalid input", ModelState.Values
                                                                                     .SelectMany(v => v.Errors)
                                                                                     .Select(e => e.ErrorMessage)
                                                                                     .ToList());
            }

            var user = await _userService.LoginAsync(form.Username, form.Password);
            var token = await _authService.GenTokenAsync(new Dictionary<string, object> { { "user", user.Username } });
            return new Response(new { message = "Login successful", token = token });
        }

        [HttpPost("change")]
        [Authorize(Policy = "JwtAuth")]
        public async Task<Response> ChangePassword([FromBody] ChangePasswordForm form)
        {
            _logger.Info($"Changing password for user {form.Username}");
            if (!ModelState.IsValid)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Invalid input", ModelState.Values
                                                                                     .SelectMany(v => v.Errors)
                                                                                     .Select(e => e.ErrorMessage)
                                                                                     .ToList());
            }

            await _userService.ChangePasswordAsync(form.Username, form.OldPassword, form.NewPassword);
            return new Response(new { message = "Password changed successfully" });
        }

        [HttpDelete("delete/{userId}")]
        [Authorize(Policy = "JwtAuth")]
        public async Task<Response> DeleteUser(int userId)
        {
            _logger.Info($"Deleting user {userId}");
            await _userService.DeleteUserAsync(userId);
            return new Response(new { message = "User deleted successfully" });
        }
    }
}