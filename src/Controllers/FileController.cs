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
using Herta.Responses.Response;

namespace Herta.Controllers.FileController;

[ApiController]
[Route("api/file")]
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

    private async Task AllowAccess(string userId)
    {
        if (!await _authService.ValidateUserAsync(userId))
        {
            throw new HttpException(403, "not allowed unauthorized access");
        }
    }

    [HttpGet("{userId}/{fileName}")]
    public async Task<FileResponse> GetFile([FromRoute] string userId, [FromRoute] string fileName)
    {
        string filePath = await _fileService.GetFilePathAsync(userId, fileName);
        return new FileResponse(fileName, filePath);
    }

    [HttpPut("{userId}/{fileName}")]
    [Authorize(Policy = "JwtAuth")]
    public async Task<Response> UploadFile(string userId, string fileName, IFormFile file)
    {
        await AllowAccess(userId);
        var fileStream = file.OpenReadStream();
        await _fileService.SaveFileFromStreamAsync(userId, fileName, fileStream);
        return new Response("File uploaded successfully");
    }

    [HttpDelete("{userId}/{fileName}")]
    [Authorize(Policy = "JwtAuth")]
    public async Task<Response> DeleteFile(string userId, string fileName)
    {
        await AllowAccess(userId);
        await _fileService.DeleteFileAsync(userId, fileName);
        return new Response("File deleted successfully");
    }
}
