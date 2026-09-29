namespace InterviewPrepApi;

// Real SM-2 algorithm (Piotr Wozniak's SuperMemo-2, 1987) -- the same
// scheduling algorithm Anki and most real spaced-repetition tools use.
// A pure function over the current state, no I/O, so it's directly
// unit-testable without a database.
public static class SpacedRepetition
{
    public readonly record struct State(double EaseFactor, int IntervalDays, int Repetitions);

    // rating: 1-5 self-reported recall quality (this app's own scale;
    // mapped 1:1 onto SM-2's real 0-5 quality scale, where a rating below
    // 3 counts as a lapse).
    public static State Schedule(State current, int rating)
    {
        if (rating < 1 || rating > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Self-rating must be between 1 and 5.");
        }

        if (rating < 3)
        {
            // A real lapse resets the interval, but SM-2 deliberately never
            // lets the ease factor collapse below 1.3 -- a single bad recall
            // shouldn't permanently wreck how fast a well-known question
            // ramps back up.
            return current with { Repetitions = 0, IntervalDays = 1 };
        }

        int nextInterval = current.Repetitions switch
        {
            0 => 1,
            1 => 6,
            _ => (int)Math.Round(current.IntervalDays * current.EaseFactor),
        };

        double nextEase = current.EaseFactor + (0.1 - (5 - rating) * (0.08 + (5 - rating) * 0.02));
        nextEase = Math.Max(1.3, nextEase);

        return new State(nextEase, nextInterval, current.Repetitions + 1);
    }
}
