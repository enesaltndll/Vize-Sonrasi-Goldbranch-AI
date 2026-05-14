using GoldBranchAI.Models;
using Microsoft.EntityFrameworkCore;

namespace GoldBranchAI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<TodoTask> Tasks { get; set; }
        public DbSet<DailyTimeLog> DailyTimeLogs { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<AiTaskBreakdown> AiTaskBreakdowns { get; set; }
        public DbSet<AiResearchLog> AiResearchLogs { get; set; }
        public DbSet<ChatGroup> ChatGroups { get; set; }
        public DbSet<ChatGroupMember> ChatGroupMembers { get; set; }
        public DbSet<SystemNotification> SystemNotifications { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<TaskFilterView> TaskFilterViews { get; set; }
        public DbSet<Friendship> Friendships { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Friend)
                .WithMany()
                .HasForeignKey(f => f.FriendId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<ChatMessage>().HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ChatMessage>().HasOne(m => m.Receiver).WithMany().HasForeignKey(m => m.ReceiverId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ChatMessage>().HasOne(m => m.ChatGroup).WithMany(g => g.Messages).HasForeignKey(m => m.ChatGroupId).OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<ChatGroup>().HasOne(g => g.CreatedByUser).WithMany().HasForeignKey(g => g.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<ChatGroupMember>().HasOne(m => m.ChatGroup).WithMany(g => g.Members).HasForeignKey(m => m.ChatGroupId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ChatGroupMember>().HasOne(m => m.AppUser).WithMany(u => u.Memberships).HasForeignKey(m => m.AppUserId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskComment>().HasOne(c => c.TodoTask).WithMany(t => t.Comments).HasForeignKey(c => c.TodoTaskId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TaskComment>().HasOne(c => c.AppUser).WithMany(u => u.TaskComments).HasForeignKey(c => c.AppUserId).OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<TodoTask>().HasOne(t => t.DependsOnTask).WithMany().HasForeignKey(t => t.DependsOnTaskId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskFilterView>()
                .HasIndex(v => new { v.AppUserId, v.Name })
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}