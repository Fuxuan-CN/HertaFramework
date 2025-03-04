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
using System.Text.RegularExpressions;

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
        _ = CreateUserFolderAsync(user.Username);
    }

    private void OnUserDeleted(object? sender, User user)
    {
        // 调用异步方法并忽略返回值
        _ = CleanUpFilesForUserAsync(user.Username);
    }

    public string GetUserFolderPath(string username)
    {
        return Path.Combine(_baseDirectory, username);
    }

    public async Task CreateUserFolderAsync(string username)
    {
        var userFolderPath = GetUserFolderPath(username);
        if (!Directory.Exists(userFolderPath))
        {
            // 使用 Task.Run 来异步执行同步方法
            await Task.Run(() => Directory.CreateDirectory(userFolderPath));
            _logger.Trace($"Created user folder: {userFolderPath}");
        }
    }

    public async Task CleanUpFilesForUserAsync(string username)
    {
        var userFolderPath = GetUserFolderPath(username);
        if (Directory.Exists(userFolderPath))
        {
            // 使用 Task.Run 来异步执行同步方法
            await Task.Run(() => Directory.Delete(userFolderPath, recursive: true));
            _logger.Trace($"Deleted user folder: {userFolderPath}");
        }
    }

    public async Task SaveFileFromStreamAsync(string username, string fileName, Stream stream)
    {
        var userFolderPath = GetUserFolderPath(username);
        var filePath = Path.Combine(userFolderPath, fileName);

        // 确保文件夹存在
        if (!Directory.Exists(userFolderPath))
        {
            await Task.Run(() => Directory.CreateDirectory(userFolderPath));
        }

        // 保存文件
        using (var fileStream = File.Create(filePath))
        {
            await stream.CopyToAsync(fileStream);
        }

        _logger.Trace($"File saved to: {filePath}");
    }

    public async Task DeleteFileAsync(string username, string fileName)
    {
        var userFolderPath = GetUserFolderPath(username);
        var filePath = Path.Combine(userFolderPath, fileName);

        // 检查文件是否存在
        if (!File.Exists(filePath))
        {
            throw new HttpException(404, $"file {fileName} not found");
        }

        // 删除文件
        await Task.Run(() => File.Delete(filePath));
        _logger.Trace($"File deleted: {filePath}");
    }

    public async Task<string> GetFilePathAsync(string username, string fileName)
    {
        string ResultPath = "";
        await Task.Run(() =>
        {
            var userFolderPath = GetUserFolderPath(username);
            var filePath = Path.Combine(userFolderPath, fileName);

            // 验证路径是否在基础目录内
            var fullPath = Path.GetFullPath(filePath);

            if (!File.Exists(filePath))
            {
                throw new HttpException(404, $"file {fileName} not found");
            }

            ResultPath = filePath;
        });

        return ResultPath;
    }
}