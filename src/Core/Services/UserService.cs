using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Herta.Models.DataModels.Users;
using Herta.Exceptions.HttpException;
using Herta.Core.Contexts.DBContext;
using Herta.Decorators.Services;
using Herta.Models.Forms.DeleteUserForm;
using Herta.Interfaces.IUserService;
using Herta.Utils.Logger;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BCrypt.Net;
using NLog;


namespace Herta.Core.Services.UserService;

// 用户登录注册相关的服务
[Service(ServiceLifetime.Scoped)]
public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(UserService));

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User> RegisterAsync(string username, string password)
    {
        if (await _context.Users.AnyAsync(u => u.Username == username))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "user already exists.");
        }

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<User> LoginAsync(string username, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "error username or password.");
        }

        return user;
    }

    public async Task ChangePasswordAsync(string username, string oldPassword, string newPassword)
    {
        await CheckUserFreezed(username);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "user not found.");
        }

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "old password error.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(DeleteUserForm form)
    {
        await CheckUserFreezed(form.Username);
        // 从数据库中获取用户
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == form.Username) ?? throw new HttpException(StatusCodes.Status404NotFound, "用户不存在.");

        if (await VerifyPwd(form.Password, user.PasswordHash))
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
        else
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "password error.");
        }
    }

    private async Task CheckUserFreezed(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.Freezed);
        if (user != null)
        {
            throw new HttpException(StatusCodes.Status403Forbidden, "user freezed.");
        }
    }

    private async Task<bool> VerifyPwd(string pwd, string hash)
    {
        if (string.IsNullOrEmpty(pwd) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        bool isPasswordValid = await Task.Run(() => BCrypt.Net.BCrypt.Verify(pwd, hash));
        return isPasswordValid;
    }
}
