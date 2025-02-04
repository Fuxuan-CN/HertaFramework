using Microsoft.EntityFrameworkCore;
using Herta.Models.DataModels.User;

namespace Herta.Core.Contexts.DBContext
{
    // 数据库上下文
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id); // 明确指定主键
                // 其他配置...
            });
        }
    }
}