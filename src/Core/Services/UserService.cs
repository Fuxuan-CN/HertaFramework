using System;
using System.Linq;
using System.Threading.Tasks;
using Herta.Models.DataModels.User;
using Herta.Exceptions.HttpException;
using Herta.Core.Contexts.DBContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Herta.Decorators.Services;
using BCrypt.Net;
using Herta.Interfaces.IUserService;

namespace Herta.Core.Services.UserService
{
    // 用户登录注册相关的服务
    [Service(ServiceLifetime.Scoped)]
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> RegisterAsync(string username, string password, string email)
        {
            if (await _context.Users.AnyAsync(u => u.Username == username || u.Email == email))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "user or email already exists.");
            }

            var user = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Email = email
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
                throw new HttpException(StatusCodes.Status401Unauthorized, "Invalid username or password.");
            }

            return user;
        }

        public async Task ChangePasswordAsync(string username, string oldPassword, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                throw new HttpException(StatusCodes.Status404NotFound, "User not found.");
            }

            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Invalid old password.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new HttpException(StatusCodes.Status404NotFound, "User not found.");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}