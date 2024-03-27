using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SimpleMathQuizzesAPI.Entities;

namespace SimpleMathQuizzesAPI.Data
{
    /// <summary>
    /// The DbContext for this app.<br/>
    /// Has a DbSet for Users and Quizzes.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) :
            base(options)
        { }
    }
}
