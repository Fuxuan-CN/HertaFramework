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
    //events
    public event EventHandler<User>? OnUserAdded;
    public event EventHandler<User>? OnUserDeleted;
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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        var addedUsers = ChangeTracker.Entries<User>()
        .Where(e => e.State == EntityState.Added)
        .Select(e => e.Entity);
        foreach (var user in addedUsers)
        {
            OnUserAdded?.Invoke(this, user);
        }

        var deletedUsers = ChangeTracker.Entries<User>()
        .Where(e => e.State == EntityState.Deleted)
        .Select(e => e.Entity);
        foreach (var user in deletedUsers)
        {
            OnUserDeleted?.Invoke(this, user);
        }

        return result;
    }
}
