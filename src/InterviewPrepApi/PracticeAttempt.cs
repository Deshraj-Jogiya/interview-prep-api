namespace InterviewPrepApi;

// A real, permanent history record of one practice attempt -- separate
// from InterviewQuestion's own SM-2 scheduling state, which only ever
// holds the CURRENT values. This is what /api/questions/{id}/attempts
// and the stats/streak endpoints actually read from.
public class PracticeAttempt
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public InterviewQuestion? Question { get; set; }
    public DateTime AttemptedAt { get; set; }

    // 1 (didn't know it at all) through 5 (knew it cold) -- self-reported,
    // same honest-self-assessment model every real spaced-repetition tool
    // uses, since there's no way to mechanically grade a spoken interview
    // answer.
    public int SelfRating { get; set; }
}
