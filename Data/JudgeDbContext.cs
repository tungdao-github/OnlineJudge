using Microsoft.EntityFrameworkCore;
using OnlineJudge.Controllers;
using OnlineJudge.Models;
using System.Collections.Generic;

namespace OnlineJudge.Data
{
    public class JudgeDbContext : DbContext
    {
        public JudgeDbContext(DbContextOptions<JudgeDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Problem> Problems { get; set; }
        public DbSet<Submission> Submissions { get; set; }
    }
}
