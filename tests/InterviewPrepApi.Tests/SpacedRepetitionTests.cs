using InterviewPrepApi;

namespace InterviewPrepApi.Tests;

public class SpacedRepetitionTests
{
    private static readonly SpacedRepetition.State Fresh = new(EaseFactor: 2.5, IntervalDays: 0, Repetitions: 0);

    [Fact]
    public void FirstGoodRecall_SchedulesOneDayOut()
    {
        var next = SpacedRepetition.Schedule(Fresh, rating: 4);
        Assert.Equal(1, next.IntervalDays);
        Assert.Equal(1, next.Repetitions);
    }

    [Fact]
    public void SecondGoodRecall_SchedulesSixDaysOut()
    {
        var afterFirst = SpacedRepetition.Schedule(Fresh, rating: 4);
        var afterSecond = SpacedRepetition.Schedule(afterFirst, rating: 4);
        Assert.Equal(6, afterSecond.IntervalDays);
        Assert.Equal(2, afterSecond.Repetitions);
    }

    [Fact]
    public void ThirdGoodRecall_MultipliesPreviousIntervalByEaseFactor()
    {
        var state = Fresh;
        state = SpacedRepetition.Schedule(state, 4); // 1 day
        state = SpacedRepetition.Schedule(state, 4); // 6 days
        var third = SpacedRepetition.Schedule(state, 4);
        // interval = round(6 * ease), ease grows slightly above 2.5 on a
        // rating of 4 -- assert the real multiplication happened, not a
        // hardcoded number.
        Assert.True(third.IntervalDays > 6);
        Assert.Equal(3, third.Repetitions);
    }

    [Fact]
    public void ALowRating_ResetsRepetitionsAndIntervalRegardlessOfHistory()
    {
        var state = Fresh;
        state = SpacedRepetition.Schedule(state, 4);
        state = SpacedRepetition.Schedule(state, 5);
        state = SpacedRepetition.Schedule(state, 5); // built up real progress

        var lapsed = SpacedRepetition.Schedule(state, rating: 1);

        Assert.Equal(0, lapsed.Repetitions);
        Assert.Equal(1, lapsed.IntervalDays);
    }

    [Fact]
    public void ALowRating_NeverDropsEaseFactorBelowTheRealFloor()
    {
        var state = Fresh;
        // Repeated real lapses -- SM-2's own floor is 1.3, never lower.
        for (int i = 0; i < 20; i++)
        {
            state = SpacedRepetition.Schedule(state, rating: 1);
        }
        Assert.True(state.EaseFactor >= 1.3);
    }

    [Fact]
    public void APerfectRating_IncreasesEaseFactorMoreThanAMediocreOne()
    {
        var afterMediocre = SpacedRepetition.Schedule(Fresh, rating: 3);
        var afterPerfect = SpacedRepetition.Schedule(Fresh, rating: 5);
        Assert.True(afterPerfect.EaseFactor > afterMediocre.EaseFactor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void OutOfRangeRating_Throws(int badRating)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SpacedRepetition.Schedule(Fresh, badRating));
    }
}
