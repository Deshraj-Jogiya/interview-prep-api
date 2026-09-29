using System.Net;
using System.Net.Http.Json;
using InterviewPrepApi;

namespace InterviewPrepApi.Tests;

public class PracticeAttemptsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PracticeAttemptsApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<int> GetAnyRealQuestionId()
    {
        var response = await _client.GetAsync("/api/questions");
        var questions = await response.Content.ReadFromJsonAsync<List<InterviewQuestion>>();
        return questions!.First().Id;
    }

    [Fact]
    public async Task AFreshQuestion_ShowsUpInDueBeforeAnyRealAttempt()
    {
        var response = await _client.GetAsync("/api/questions/due");
        response.EnsureSuccessStatusCode();
        var due = await response.Content.ReadFromJsonAsync<List<InterviewQuestion>>();
        Assert.NotEmpty(due!);
    }

    [Fact]
    public async Task PostingAGoodAttempt_SchedulesItOutOfDueRange()
    {
        var questionId = await GetAnyRealQuestionId();

        var postResponse = await _client.PostAsJsonAsync($"/api/questions/{questionId}/attempts", new { selfRating = 5 });
        postResponse.EnsureSuccessStatusCode();
        var updated = await postResponse.Content.ReadFromJsonAsync<InterviewQuestion>();
        Assert.NotNull(updated!.NextReviewAt);
        Assert.True(updated.NextReviewAt > DateTime.UtcNow);

        var dueResponse = await _client.GetAsync("/api/questions/due");
        var due = await dueResponse.Content.ReadFromJsonAsync<List<InterviewQuestion>>();
        Assert.DoesNotContain(due!, q => q.Id == questionId);
    }

    [Fact]
    public async Task PostingAnAttempt_RecordsRealHistoryReadableAfterward()
    {
        var questionId = await GetAnyRealQuestionId();

        await _client.PostAsJsonAsync($"/api/questions/{questionId}/attempts", new { selfRating = 3 });

        var historyResponse = await _client.GetAsync($"/api/questions/{questionId}/attempts");
        historyResponse.EnsureSuccessStatusCode();
        var history = await historyResponse.Content.ReadFromJsonAsync<List<PracticeAttempt>>();
        Assert.Contains(history!, a => a.SelfRating == 3);
    }

    [Fact]
    public async Task PostingAnAttempt_ForANonexistentQuestion_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/api/questions/999999/attempts", new { selfRating = 4 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostingAnAttempt_WithAnOutOfRangeRating_Returns400()
    {
        var questionId = await GetAnyRealQuestionId();
        var response = await _client.PostAsJsonAsync($"/api/questions/{questionId}/attempts", new { selfRating = 9 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Stats_ReflectsARealPostedAttempt()
    {
        var questionId = await GetAnyRealQuestionId();
        await _client.PostAsJsonAsync($"/api/questions/{questionId}/attempts", new { selfRating = 5 });

        var statsResponse = await _client.GetAsync("/api/stats");
        statsResponse.EnsureSuccessStatusCode();
        var stats = await statsResponse.Content.ReadFromJsonAsync<StatsResponse>();

        Assert.True(stats!.totalAttempts >= 1);
        Assert.True(stats.currentStreakDays >= 1); // the attempt just made is today
    }

    private record StatsResponse(int totalAttempts, int questionsMastered, int totalQuestions, int currentStreakDays);
}
