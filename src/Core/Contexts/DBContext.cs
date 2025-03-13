using Microsoft.EntityFrameworkCore;
using Herta.Models.DataModels.Users;
using Herta.Models.DataModels.UserInfos;
using Herta.Models.DataModels.Groups;
using Herta.Models.DataModels.GroupMembers;
using Herta.Models.DataModels.Messages;
using Herta.Models.DataModels.File;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Herta.Core.Contexts.DBContext;

public class ApplicationDbContext : DbContext
{
    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<UserInfo> UserInfos { get; set; }
    public DbSet<GroupMembers> GroupMembers { get; set; }
    public DbSet<Groups> Groups { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<UserFile> Files { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<GroupMembers>()
            .Property(gm => gm.RoleIs)
            .HasConversion<string>();
    }
}
