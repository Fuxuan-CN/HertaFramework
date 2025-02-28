using System;
using System.Linq;
using System.Text.Json;
using System.Collections;
using System.Threading.Tasks;
using Herta.Exceptions.HttpException;
using Herta.Decorators.Services;
using Herta.Interfaces.IUserInfoService;
using Microsoft.Extensions.DependencyInjection;
using Herta.Models.DataModels.UserInfos;
using Herta.Core.Contexts.DBContext;
using Microsoft.EntityFrameworkCore;
using Herta.Models.Forms.UpdateInfoForm;
using Herta.Utils.Logger;
using NLog;

namespace Herta.Core.Services.UserInfoService;

[Service(ServiceLifetime.Scoped)]
public class UserInfoService : IUserInfoService
{
    private readonly ApplicationDbContext _context;
    private static readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(UserInfoService));

    public UserInfoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserInfo> GetUserInfoAsync(int userId)
    {
        var userInfo = await _context.UserInfos.FirstOrDefaultAsync(ui => ui.UserId == userId);
        if (userInfo == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "用户信息不存在。");
        }
        return userInfo;
    }

    public async Task<bool> UpdateUserInfoAsync(UserInfo userInfo)
    {
        var existingUserInfo = await _context.Users.FirstOrDefaultAsync(user => user.Id == userInfo.UserId);
        if (existingUserInfo == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "用户信息不存在。");
        }

        _context.UserInfos.Update(userInfo);
        await _context.SaveChangesAsync();

        return true;
    }

    // 实现部分更新
    public async Task<bool> PartialUpdateUserInfoAsync(UpdateInfoForm updateInfoForm)
    {
        var originalUserInfo = await _context.UserInfos.FirstOrDefaultAsync(ui => ui.Id == updateInfoForm.UserId);
        if (originalUserInfo == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "用户信息不存在。");
        }

        var entry = _context.Entry(originalUserInfo);
        foreach (var (key, value) in updateInfoForm.UpdateInfo)
        {
            var property = entry.Property(key);
            if (property == null)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"字段 '{key}' 不存在。");
            }

            // 获取字段的 CLR 类型
            var propertyType = property.Metadata.ClrType;

            _logger.Trace($"Update property '{key}' with value '{value}', type '{propertyType.Name}'");
            // 尝试将前端传来的 JSON 字符串转换为对应的 CLR 类型
            switch (propertyType)
            {
                case Type t when (t == typeof(string)):
                    property.CurrentValue = value.ToString();
                    break;
                case Type t when (t == typeof(int)):
                    property.CurrentValue = (int)value;
                    break;
                case Type t when (t == typeof(bool)):
                    property.CurrentValue = (bool)value;
                    break;
                case Type t when (t == typeof(DateTime)):
                    property.CurrentValue = DateTime.Parse(value.ToString()!);
                    break;
                case Type t when (t == typeof(double)):
                    property.CurrentValue = double.Parse(value.ToString()!);
                    break;
                case Type t when (t == typeof(decimal)):
                    property.CurrentValue = decimal.Parse(value.ToString()!);
                    break;
                case Type t when (t == typeof(string[])):
                    property.CurrentValue = JsonSerializer.Deserialize<string[]>(value.ToString()!);
                    break;
                default:
                    throw new HttpException(StatusCodes.Status400BadRequest, $"字段 '{key}' 的类型 '{propertyType.Name}' 不支持更新。");
            }
        }

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (Exception ex)
        {
            throw new HttpException(StatusCodes.Status500InternalServerError, "内部服务器错误", ex);
        }
    }
}
