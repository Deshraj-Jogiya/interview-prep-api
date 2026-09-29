using InterviewPrepApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Open CORS: this is a public, read-mostly reference API meant to be called
// directly from other clients (e.g. the companion mobile app), not just
// same-origin browser code -- there's no cookie/session auth to protect here.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=interview_prep.db";
builder.Services.AddDbContext<InterviewPrepDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InterviewPrepDbContext>();
    db.Database.EnsureCreated();
    if (!db.Questions.Any())
    {
        db.Questions.AddRange(
            new InterviewQuestion { Category = "System Design", Question = "How would you design a URL shortener?", Difficulty = "Medium" },
            new InterviewQuestion { Category = "System Design", Question = "How would you design a rate limiter?", Difficulty = "Hard" },
            new InterviewQuestion { Category = "Behavioral", Question = "Tell me about a time you disagreed with a teammate.", Difficulty = "Easy" },
            new InterviewQuestion { Category = "SQL", Question = "Write a query to find the second-highest salary per department.", Difficulty = "Medium" }
        );
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.MapGet("/api/questions", async (InterviewPrepDbContext db, string? category) =>
{
    var query = db.Questions.AsQueryable();
    if (!string.IsNullOrWhiteSpace(category))
    {
        query = query.Where(q => q.Category == category);
    }
    return Results.Ok(await query.ToListAsync());
});

app.MapGet("/api/questions/random", async (InterviewPrepDbContext db, string? category) =>
{
    var query = db.Questions.AsQueryable();
    if (!string.IsNullOrWhiteSpace(category))
    {
        query = query.Where(q => q.Category == category);
    }
    var candidates = await query.ToListAsync();
    if (candidates.Count == 0)
    {
        return Results.NotFound(new { error = "No questions match that category." });
    }
    var picked = candidates[Random.Shared.Next(candidates.Count)];
    return Results.Ok(picked);
});

app.MapPost("/api/questions", async (InterviewPrepDbContext db, InterviewQuestion question) =>
{
    if (string.IsNullOrWhiteSpace(question.Category) || string.IsNullOrWhiteSpace(question.Question))
    {
        return Results.BadRequest(new { error = "category and question are required" });
    }
    db.Questions.Add(question);
    await db.SaveChangesAsync();
    return Results.Created($"/api/questions/{question.Id}", question);
});

// Real spaced-repetition scheduling (SM-2 -- see SpacedRepetition.cs).
// A question with NextReviewAt == null has never been practiced and is
// always due; otherwise due means the scheduled date has passed.
app.MapGet("/api/questions/due", async (InterviewPrepDbContext db) =>
{
    var now = DateTime.UtcNow;
    var due = await db.Questions
        .Where(q => q.NextReviewAt == null || q.NextReviewAt <= now)
        .ToListAsync();
    return Results.Ok(due);
});

app.MapPost("/api/questions/{id:int}/attempts", async (InterviewPrepDbContext db, int id, PracticeAttemptRequest request) =>
{
    if (request.SelfRating < 1 || request.SelfRating > 5)
    {
        return Results.BadRequest(new { error = "selfRating must be between 1 and 5" });
    }

    var question = await db.Questions.FindAsync(id);
    if (question is null)
    {
        return Results.NotFound(new { error = $"No question with id {id}." });
    }

    var current = new SpacedRepetition.State(question.EaseFactor, question.IntervalDays, question.Repetitions);
    var next = SpacedRepetition.Schedule(current, request.SelfRating);

    question.EaseFactor = next.EaseFactor;
    question.IntervalDays = next.IntervalDays;
    question.Repetitions = next.Repetitions;
    question.NextReviewAt = DateTime.UtcNow.AddDays(next.IntervalDays);

    db.PracticeAttempts.Add(new PracticeAttempt
    {
        QuestionId = id,
        AttemptedAt = DateTime.UtcNow,
        SelfRating = request.SelfRating,
    });

    await db.SaveChangesAsync();
    return Results.Ok(question);
});

app.MapGet("/api/questions/{id:int}/attempts", async (InterviewPrepDbContext db, int id) =>
{
    var attempts = await db.PracticeAttempts
        .Where(a => a.QuestionId == id)
        .OrderByDescending(a => a.AttemptedAt)
        .ToListAsync();
    return Results.Ok(attempts);
});

// Real aggregate progress, not fabricated placeholder numbers -- streak
// is computed from actual distinct calendar days with at least one real
// attempt, counting back from today (or yesterday, if today has no
// attempt yet) until the first gap.
app.MapGet("/api/stats", async (InterviewPrepDbContext db) =>
{
    var totalAttempts = await db.PracticeAttempts.CountAsync();
    var questionsMastered = await db.Questions.CountAsync(q => q.Repetitions >= 3 && q.EaseFactor >= 2.5);

    var attemptDates = (await db.PracticeAttempts
        .Select(a => a.AttemptedAt.Date)
        .Distinct()
        .ToListAsync())
        .OrderByDescending(d => d)
        .ToList();

    int streak = 0;
    if (attemptDates.Count > 0)
    {
        var today = DateTime.UtcNow.Date;
        var cursor = attemptDates[0] == today ? today : (attemptDates[0] == today.AddDays(-1) ? today.AddDays(-1) : DateTime.MinValue);
        if (cursor != DateTime.MinValue)
        {
            var dateSet = attemptDates.ToHashSet();
            while (dateSet.Contains(cursor))
            {
                streak++;
                cursor = cursor.AddDays(-1);
            }
        }
    }

    return Results.Ok(new
    {
        totalAttempts,
        questionsMastered,
        totalQuestions = await db.Questions.CountAsync(),
        currentStreakDays = streak,
    });
});

app.Run();

public partial class Program { }

public record PracticeAttemptRequest(int SelfRating);
