using Microsoft.EntityFrameworkCore;
using Herta.Models.DataModels.Users;
using Herta.Models.DataModels.UserInfos;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Herta.Core.Contexts.DBContext
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserInfo> UserInfos { get; set; }

        // 添加一个接受 DbContextOptions<ApplicationDbContext> 参数的构造函数
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}