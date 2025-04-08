using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.Controllers;
using OnlineJudgeAPI.Models;

namespace OnlineJudgeAPI.Services
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        
        public DbSet<Submission> Submissions { get; set; }
        
        public DbSet<TestCase> TestCases { get; set; }
        public DbSet<Problem> Problems { get; set; }
        //public DbSet<User> Users { get; set; }
        //public DbSet<Role> Roles { get; set; }
        //public void AddResultColumn()
        //{
        //    var query = "ALTER TABLE Submissions ADD COLUMN Result LONGTEXT CHARACTER SET utf8mb4 NULL;";
        //    this.Database.ExecuteSqlRaw(query);
        //}
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);

        //    // Thêm chỉ mục cho ProblemId trong bảng TestCases
        //    modelBuilder.Entity<TestCase>()
        //        .HasIndex(tc => tc.ProblemId)
        //        .HasDatabaseName("IX_TestCases_ProblemId");

        //    // Thêm chỉ mục cho Submission.Status để truy vấn nhanh hơn
        //    modelBuilder.Entity<Submission>()
        //        .HasIndex(s => s.Status)
        //        .HasDatabaseName("IX_Submissions_Status");
        //    //modelBuilder.Entity<User>()
        //    //    .HasIndex(u => u.Email)
        //    //    .IsUnique();

        //    modelBuilder.Entity<Role>()
        //        .HasIndex(r => new { r.UserId, r.RoleName })
        //        .IsUnique();
        //}
      
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Contest> Contests { get; set; }
        public DbSet<ContestProblem> ContestProblems { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);
            modelBuilder.Entity<Submission>()
    .HasOne(s => s.User)
    .WithMany(u => u.Submissions)
    .HasForeignKey(s => s.UserId);

            modelBuilder.Entity<Submission>()
                .HasOne(s => s.Problem)
                .WithMany(p => p.Submissions)
                .HasForeignKey(s => s.ProblemId);
            modelBuilder.Entity<ContestProblem>()
            .HasKey(cp => new { cp.ContestId, cp.ProblemId });

            modelBuilder.Entity<ContestProblem>()
                .HasOne(cp => cp.Contest)
                .WithMany(c => c.ContestProblems)
                .HasForeignKey(cp => cp.ContestId);

            modelBuilder.Entity<ContestProblem>()
                .HasOne(cp => cp.Problem)
                .WithMany()
                .HasForeignKey(cp => cp.ProblemId);

        }
    }
}
