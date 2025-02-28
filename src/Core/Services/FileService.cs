using System;
using System.IO;
using System.Threading.Tasks;
using Herta.Models.DataModels.Users;
using Herta.Interfaces.IFileService;
using Herta.Core.Contexts.DBContext;
using Herta.Decorators.Services;
using Herta.Exceptions.HttpException;
using Herta.Utils.Logger;
using NLog;
using Microsoft.Extensions.Configuration;

namespace Herta.Core.Services.FileService;

[Service]
public class FileService : IFileService
{
    private readonly ApplicationDbContext _context;
    private readonly string _baseDirectory;
    private readonly IConfiguration _configuration;
    private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(FileService));

    public FileService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _baseDirectory = _configuration.GetValue<string>("FileService:BaseDirectory") ?? string.Empty;

        // 在构造函数中订阅事件
        _context.OnUserAdded += OnUserAdded;
        _context.OnUserDeleted += OnUserDeleted;
    }

    private void OnUserAdded(object? sender, User user)
    {
        // 调用异步方法并忽略返回值
        _ = CreateUserFolderAsync(user);
    }

    private void OnUserDeleted(object? sender, User user)
    {
        // 调用异步方法并忽略返回值
        _ = CleanUpFilesForUserAsync(user);
    }

    public string GetUserFolderPath(User user)
    {
        return Path.Combine(_baseDirectory, user.Username);
    }

    public async Task CreateUserFolderAsync(User user)
    {
        var userFolderPath = GetUserFolderPath(user);
        if (!Directory.Exists(userFolderPath))
        {
            // 使用 Task.Run 来异步执行同步方法
            await Task.Run(() => Directory.CreateDirectory(userFolderPath));
            _logger.Trace($"Created user folder: {userFolderPath}");
        }
    }

    public async Task CleanUpFilesForUserAsync(User user)
    {
        var userFolderPath = GetUserFolderPath(user);
        if (Directory.Exists(userFolderPath))
        {
            // 使用 Task.Run 来异步执行同步方法
            await Task.Run(() => Directory.Delete(userFolderPath, recursive: true));
            _logger.Trace($"Deleted user folder: {userFolderPath}");
        }
    }

    public async Task SaveFileFromStreamAsync(User user, string fileName, Stream stream)
    {
        var userFolderPath = GetUserFolderPath(user);
        var filePath = Path.Combine(userFolderPath, fileName);

        // 确保文件夹存在
        if (!Directory.Exists(userFolderPath))
        {
            await Task.Run(() => Directory.CreateDirectory(userFolderPath));
        }

        // 将流保存为文件
        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(fileStream);
        }

        _logger.Trace($"File saved to: {filePath}");
    }

    public async Task<string> GetFilePathAsync(User user, string fileName)
    {
        string ResultPath = "";
        await Task.Run(() =>
        {
            var userFolderPath = GetUserFolderPath(user);
            var filePath = Path.Combine(userFolderPath, fileName);

            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                throw new HttpException(404, $"文件 {fileName} 不存在");
            }

            ResultPath = filePath;
        });
        return ResultPath;
    }
}