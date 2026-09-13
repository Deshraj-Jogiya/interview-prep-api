using Microsoft.EntityFrameworkCore;

namespace InterviewPrepApi;

public class InterviewPrepDbContext : DbContext
{
    public InterviewPrepDbContext(DbContextOptions<InterviewPrepDbContext> options) : base(options) { }

    public DbSet<InterviewQuestion> Questions => Set<InterviewQuestion>();
}
