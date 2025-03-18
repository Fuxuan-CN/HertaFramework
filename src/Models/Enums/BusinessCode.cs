

namespace Herta.Models.Enums.BusinessCode;

public enum BusinessCode
{
    // 操作成功
    Success = 0,
    // 参数错误
    ArgumentError = 40001,
    // 未授权
    Unauthorized = 40100,
    // 无效的token
    TokenFailed = 40101,
    // 禁止访问
    Forbidden = 40300,
    // 资源不存在
    NotFound = 40400,
    // 服务器错误
    ServerError = 50000,
}