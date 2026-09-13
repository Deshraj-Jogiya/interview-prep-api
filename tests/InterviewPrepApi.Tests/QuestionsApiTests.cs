using System.Net;
using System.Net.Http.Json;
using InterviewPrepApi;

namespace InterviewPrepApi.Tests;

public class QuestionsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public QuestionsApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetQuestions_ReturnsTheRealSeededQuestions()
    {
        var response = await _client.GetAsync("/api/questions");
        response.EnsureSuccessStatusCode();

        var questions = await response.Content.ReadFromJsonAsync<List<InterviewQuestion>>();
        Assert.NotNull(questions);
        Assert.True(questions!.Count >= 4);
        Assert.Contains(questions, q => q.Category == "System Design");
    }

    [Fact]
    public async Task GetQuestions_FiltersByCategory()
    {
        var response = await _client.GetAsync("/api/questions?category=Behavioral");
        response.EnsureSuccessStatusCode();

        var questions = await response.Content.ReadFromJsonAsync<List<InterviewQuestion>>();
        Assert.NotNull(questions);
        Assert.All(questions!, q => Assert.Equal("Behavioral", q.Category));
    }

    [Fact]
    public async Task GetRandomQuestion_ReturnsNotFoundForAnUnknownCategory()
    {
        var response = await _client.GetAsync("/api/questions/random?category=NonexistentCategory");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostQuestion_PersistsItAndItShowsUpInTheList()
    {
        var newQuestion = new InterviewQuestion
        {
            Category = "Coding",
            Question = "Reverse a linked list in place.",
            Difficulty = "Medium",
        };

        var postResponse = await _client.PostAsJsonAsync("/api/questions", newQuestion);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var listResponse = await _client.GetAsync("/api/questions?category=Coding");
        var questions = await listResponse.Content.ReadFromJsonAsync<List<InterviewQuestion>>();
        Assert.Contains(questions!, q => q.Question == "Reverse a linked list in place.");
    }

    [Fact]
    public async Task PostQuestion_RejectsAMissingRequiredField()
    {
        var invalid = new InterviewQuestion { Category = "", Question = "Missing category" };
        var response = await _client.PostAsJsonAsync("/api/questions", invalid);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
