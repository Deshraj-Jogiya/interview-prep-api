namespace InterviewPrepApi;

public class InterviewQuestion
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Medium";

    // SM-2 spaced-repetition state (the same real algorithm Anki uses) --
    // stored directly on the question since it's the CURRENT scheduling
    // state, not history (PracticeAttempt is the history log). A fresh
    // question with Repetitions == 0 and NextReviewAt == null has never
    // been practiced, and is always due.
    public double EaseFactor { get; set; } = 2.5;
    public int IntervalDays { get; set; } = 0;
    public int Repetitions { get; set; } = 0;
    public DateTime? NextReviewAt { get; set; }
}
