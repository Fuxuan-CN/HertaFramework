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
using Herta.Utils.Logger;
using NLog;

namespace Herta.Core.Services.AuthService
{
    [Service(ServiceLifetime.Scoped)]
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(AuthService));

        public AuthService(IConfiguration configuration)
        {
            _configuration = configuration;
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
                    ValidateIssuer = true,
                    ValidateAudience = false,
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
}