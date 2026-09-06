using Elearning.Domain;
using Elearning.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Persistence;

public sealed class ElearningDbContext(
    DbContextOptions<ElearningDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<long>, long>(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<StudentAnswer> StudentAnswers => Set<StudentAnswer>();
    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<BlacklistedToken> BlacklistedTokens => Set<BlacklistedToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("pg_trgm");
        builder.ApplyConfigurationsFromAssembly(typeof(ElearningDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        IncrementVersion<Course>();
        IncrementVersion<Lesson>();
        IncrementVersion<Question>();
        IncrementVersion<LessonProgress>();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void IncrementVersion<TEntity>()
        where TEntity : class
    {
        foreach (var entry in ChangeTracker.Entries<TEntity>().Where(entry => entry.State == EntityState.Modified))
        {
            var version = entry.Property<long>("Version");
            version.CurrentValue = version.OriginalValue + 1;
        }
    }
}
