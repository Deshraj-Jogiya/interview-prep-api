namespace InterviewPrepApi;

public class InterviewQuestion
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Medium";
}
