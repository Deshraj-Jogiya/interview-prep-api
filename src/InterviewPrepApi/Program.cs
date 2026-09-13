using InterviewPrepApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.Run();

public partial class Program { }
