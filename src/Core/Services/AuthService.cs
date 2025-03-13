using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Herta.Decorators.Services;
using Herta.Interfaces.IAuthService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Herta.Utils.Logger;
using Herta.Exceptions.HttpException;
using NLog;

namespace Herta.Core.Services.AuthService;

[Service(ServiceLifetime.Scoped)]
public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(AuthService));

    public AuthService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public bool ValidateUser(int userId)
    {
        _logger.Trace("Validating user...");
        try
        {
            var token = _httpContextAccessor.HttpContext!.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "") ?? string.Empty;
            if (string.IsNullOrEmpty(token))
            {
                _logger.Trace("Authorization header is missing or invalid.");
                return false;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "secret");

            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, parameters, out var validatedToken);
            var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "userId");

            if (userIdClaim == null)
            {
                _logger.Trace("User or userId field is missing in the token.");
                return false;
            }

            if (userIdClaim.Value == userId.ToString())
            {
                _logger.Trace("User validation succeeded.");
                return true;
            }
            else
            {
                _logger.Trace("User validation failed.");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error during user validation, reason: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ValidateUserAsync(int userId)
    {
        return await Task.Run(() => ValidateUser(userId));
    }

    public async Task<bool> AuthorizeAsync(string token)
    {
        _logger.Trace("Authorizing...");
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "secret");
        try
        {
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };

            await tokenHandler.ValidateTokenAsync(token, parameters);

            _logger.Trace("Authorization succeeded.");
            return true;
        }
        catch
        {
            _logger.Trace("Authorization failed.");
            return false;
        }
    }

    public async Task<string> GenTokenAsync(Dictionary<string, object> payload)
    {
        _logger.Trace("Generating token...");
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "secret");
        var issuer = _configuration["Jwt:Issuer"] ?? "issuer";
        var claims = new List<Claim>();

        foreach (var item in payload)
        {
            claims.Add(new Claim(item.Key!, item.Value.ToString()!));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7), // 1周过期
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = issuer
        };

        var tokenString = await Task.Run(() =>
        {
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        });
        _logger.Trace("Token generated.");
        return tokenString;
    }
}
