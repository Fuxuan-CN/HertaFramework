using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.IO;
using System.Threading.Tasks;
using Herta.Models.DataModels.Users;
using Herta.Interfaces.IFileService;
using Herta.Interfaces.IAuthService;
using Herta.Core.Services.FileService;
using Herta.Exceptions.HttpException;
using Herta.Utils.Logger;
using NLog;
using Herta.Responses.FileResponse;
using Herta.Responses.BusinessResponse;
using Herta.Models.Enums.BusinessCode;

namespace Herta.Controllers.FileController;

[ApiController]
[Route("files")]
public class FileController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly IAuthService _authService;
    private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(FileController));

    public FileController(IFileService fileService, IAuthService authService)
    {
        _fileService = fileService;
        _authService = authService;
    }

    private async Task AllowAccess(int userId)
    {
        if (!await _authService.ValidateUserAsync(userId))
        {
            throw new HttpException(403, "not allowed unauthorized access");
        }
    }

    [HttpGet("{userId}/{fileName}")]
    public async Task<FileResponse> GetFile([FromRoute] int userId, [FromRoute] string fileName)
    {
        string filePath = await _fileService.GetFilePathAsync(userId, fileName);
        _logger.Debug($"Try get file {filePath}.");
        return new FileResponse(fileName, filePath);
    }

    [HttpPut("{userId}/{fileName}")]
    [Authorize(Policy = "JwtAuth")]
    public async Task<BusinessResponse<bool>> UploadFile([FromRoute] int userId, [FromRoute] string fileName, IFormFile file)
    {
        _logger.Debug($"Try upload file {fileName}.");
        await AllowAccess(userId);
        var fileStream = file.OpenReadStream();
        await _fileService.SaveFileFromStreamAsync(userId, fileName, fileStream);
        return new BusinessResponse<bool>(BusinessCode.Success, "File uploaded successfully", true);
    }

    [HttpDelete("{userId}/{fileName}")]
    [Authorize(Policy = "JwtAuth")]
    public async Task<BusinessResponse<bool>> DeleteFile([FromRoute] int userId, [FromRoute] string fileName)
    {
        _logger.Debug($"Try delete file {fileName}.");
        await AllowAccess(userId);
        await _fileService.DeleteFileAsync(userId, fileName);
        return new BusinessResponse<bool>(BusinessCode.Success, "File deleted successfully", true);
    }
}
