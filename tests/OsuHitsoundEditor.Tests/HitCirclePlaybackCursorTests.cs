
using OsuHitsoundEditor;

namespace OsuHitsoundEditor.Tests;

public class HitCirclePlaybackCursorTests
{
    [Fact]
    public void AdvanceTo_FirstCall_IncludesZeroAndUpperBoundary()
    {
        var beatmap = new Beatmap();

        HitObject atZero = new() { ObjectType = 1, Time = 0 };
        HitObject atEnd = new() { ObjectType = 1, Time = 1000 };
        HitObject afterEnd = new() { ObjectType = 1, Time = 1001 };

        beatmap.HitObjects.AddRange(
            new[] { atZero, atEnd, afterEnd });

        var cursor = new HitCirclePlaybackCursor();

        List<HitObject> result = cursor.AdvanceTo(beatmap, 1000);

        Assert.Equal(2, result.Count);
        Assert.Same(atZero, result[0]);
        Assert.Same(atEnd, result[1]);
    }

    [Fact]
    public void AdvanceTo_AdjacentIntervals_DoesNotRepeatEvents()
    {
        var beatmap = new Beatmap();

        HitObject first = new() { ObjectType = 1, Time = 500 };
        HitObject second = new() { ObjectType = 1, Time = 1000 };
        HitObject third = new() { ObjectType = 1, Time = 1500 };

        beatmap.HitObjects.AddRange(
            new[] { first, second, third });

        var cursor = new HitCirclePlaybackCursor();

        List<HitObject> firstResult = cursor.AdvanceTo(beatmap, 1000);
        List<HitObject> secondResult = cursor.AdvanceTo(beatmap, 1500);

        Assert.Equal(2, firstResult.Count);
        Assert.Same(first, firstResult[0]);
        Assert.Same(second, firstResult[1]);

        Assert.Same(third, Assert.Single(secondResult));
    }

    [Fact]
    public void AdvanceTo_WhenPositionDoesNotChange_ReturnsEmptyList()
    {
        var beatmap = new Beatmap();

        beatmap.HitObjects.Add(
            new HitObject { ObjectType = 1, Time = 1000 });

        var cursor = new HitCirclePlaybackCursor();

        cursor.AdvanceTo(beatmap, 1000);

        List<HitObject> result = cursor.AdvanceTo(beatmap, 1000);

        Assert.Empty(result);
    }

    [Fact]
    public void AdvanceTo_WhenBeatmapHasNoCircles_ReturnsEmptyList()
    {
        var beatmap = new Beatmap();

        beatmap.HitObjects.AddRange(new[]
        {
            new HitObject { ObjectType = 2, Time = 500 },
            new HitObject { ObjectType = 8, Time = 1000 }
        });

        var cursor = new HitCirclePlaybackCursor();

        List<HitObject> result = cursor.AdvanceTo(beatmap, 1500);

        Assert.Empty(result);
    }

    [Fact]
    public void AdvanceTo_WhenBeatmapIsNull_ThrowsArgumentNullException()
    {
        var cursor = new HitCirclePlaybackCursor();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                cursor.AdvanceTo(null!, 1000));

        Assert.Equal("beatmap", exception.ParamName);
    }

    [Fact]
    public void AdvanceTo_WhenPositionIsNegative_ThrowsArgumentOutOfRangeException()
    {
        var beatmap = new Beatmap();
        var cursor = new HitCirclePlaybackCursor();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                cursor.AdvanceTo(beatmap, -0.5));

        Assert.Equal(
            "currentPositionMilliseconds",
            exception.ParamName);

        Assert.Contains(
            "The timeline position must be finite and non-negative.",
            exception.Message);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AdvanceTo_WhenPositionIsNotFinite_ThrowsArgumentOutOfRangeException(
        double invalidPosition)
    {
        var beatmap = new Beatmap();
        var cursor = new HitCirclePlaybackCursor();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                cursor.AdvanceTo(beatmap, invalidPosition));

        Assert.Equal(
            "currentPositionMilliseconds",
            exception.ParamName);

        Assert.Contains(
            "The timeline position must be finite and non-negative.",
            exception.Message);
    }

    [Fact]
    public void AdvanceTo_WhenPositionMovesBackwards_ThrowsInvalidOperationException()
    {
        var beatmap = new Beatmap();
        var cursor = new HitCirclePlaybackCursor();

        cursor.AdvanceTo(beatmap, 1500);

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                cursor.AdvanceTo(beatmap, 1000));

        Assert.Equal(
            "The timeline position moved backwards. Reset the playback cursor after seeking.",
            exception.Message);
    }

    [Fact]
    public void AdvanceTo_WhenBackwardAdvanceFails_PreservesPreviousPosition()
    {
        var beatmap = new Beatmap();

        HitObject first = new() { ObjectType = 1, Time = 800 };
        HitObject second = new() { ObjectType = 1, Time = 1200 };

        beatmap.HitObjects.AddRange(new[] { first, second });

        var cursor = new HitCirclePlaybackCursor();

        List<HitObject> initial = cursor.AdvanceTo(beatmap, 1000);

        Assert.Same(first, Assert.Single(initial));

        Assert.Throws<InvalidOperationException>(() =>
            cursor.AdvanceTo(beatmap, 500));

        List<HitObject> result = cursor.AdvanceTo(beatmap, 1500);

        Assert.Same(second, Assert.Single(result));
    }

    [Fact]
    public void AdvanceTo_WhenInvalidAdvanceFails_PreservesPreviousPosition()
    {
        var beatmap = new Beatmap();

        HitObject first = new() { ObjectType = 1, Time = 400 };
        HitObject second = new() { ObjectType = 1, Time = 800 };

        beatmap.HitObjects.AddRange(new[] { first, second });

        var cursor = new HitCirclePlaybackCursor();

        Assert.Same(
            first,
            Assert.Single(cursor.AdvanceTo(beatmap, 500)));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            cursor.AdvanceTo(beatmap, double.NaN));

        List<HitObject> result = cursor.AdvanceTo(beatmap, 1000);

        Assert.Same(second, Assert.Single(result));
    }

    [Fact]
    public void AdvanceTo_SeparateCursors_MaintainIndependentPositions()
    {
        var beatmap = new Beatmap();

        HitObject first = new() { ObjectType = 1, Time = 100 };
        HitObject second = new() { ObjectType = 1, Time = 200 };

        beatmap.HitObjects.AddRange(new[] { first, second });

        var firstCursor = new HitCirclePlaybackCursor();
        var secondCursor = new HitCirclePlaybackCursor();

        List<HitObject> firstResult =
            firstCursor.AdvanceTo(beatmap, 100);

        List<HitObject> secondResult =
            secondCursor.AdvanceTo(beatmap, 200);

        Assert.Same(first, Assert.Single(firstResult));

        Assert.Equal(2, secondResult.Count);
        Assert.Same(first, secondResult[0]);
        Assert.Same(second, secondResult[1]);

        Assert.Same(
            second,
            Assert.Single(firstCursor.AdvanceTo(beatmap, 200)));

        Assert.Empty(secondCursor.AdvanceTo(beatmap, 200));
    }

    [Fact]
    public void AdvanceTo_PreservesBeatmapObjectOrder()
    {
        var beatmap = new Beatmap();

        HitObject later = new() { ObjectType = 1, Time = 1500 };
        HitObject earlier = new() { ObjectType = 1, Time = 1000 };

        beatmap.HitObjects.AddRange(new[] { later, earlier });

        var cursor = new HitCirclePlaybackCursor();

        List<HitObject> result = cursor.AdvanceTo(beatmap, 2000);

        Assert.Equal(2, result.Count);
        Assert.Same(later, result[0]);
        Assert.Same(earlier, result[1]);
    }

    [Fact]
    public void SeekTo_Zero_AllowsReplayingCircleAtZero()
    {
        var beatmap = new Beatmap();

        HitObject atZero = new() { ObjectType = 1, Time = 0 };
        HitObject at500 = new() { ObjectType = 1, Time = 500 };

        beatmap.HitObjects.AddRange(new[] { atZero, at500 });

        var cursor = new HitCirclePlaybackCursor();

        Assert.Equal(2, cursor.AdvanceTo(beatmap, 500).Count);

        cursor.SeekTo(0);

        List<HitObject> result = cursor.AdvanceTo(beatmap, 0);

        Assert.Same(atZero, Assert.Single(result));

        // The same event must not repeat without another seek.
        Assert.Empty(cursor.AdvanceTo(beatmap, 0));
    }

    [Fact]
    public void SeekTo_Forward_SkipsEventsUpToNewPosition()
    {
        var beatmap = new Beatmap();

        HitObject skipped = new() { ObjectType = 1, Time = 500 };
        HitObject atSeekPosition = new() { ObjectType = 1, Time = 1000 };
        HitObject firstPending = new() { ObjectType = 1, Time = 1200 };
        HitObject secondPending = new() { ObjectType = 1, Time = 1500 };

        beatmap.HitObjects.AddRange(new[]
        {
            skipped,
            atSeekPosition,
            firstPending,
            secondPending
        });

        var cursor = new HitCirclePlaybackCursor();

        cursor.AdvanceTo(beatmap, 300);
        cursor.SeekTo(1000);

        List<HitObject> result = cursor.AdvanceTo(beatmap, 1500);

        Assert.Equal(2, result.Count);
        Assert.Same(firstPending, result[0]);
        Assert.Same(secondPending, result[1]);
    }

    [Fact]
    public void SeekTo_Backward_AllowsProcessingEventsAgain()
    {
        var beatmap = new Beatmap();

        HitObject first = new() { ObjectType = 1, Time = 500 };
        HitObject second = new() { ObjectType = 1, Time = 1200 };
        HitObject third = new() { ObjectType = 1, Time = 1800 };

        beatmap.HitObjects.AddRange(new[] { first, second, third });

        var cursor = new HitCirclePlaybackCursor();

        Assert.Equal(3, cursor.AdvanceTo(beatmap, 1800).Count);

        cursor.SeekTo(500);

        List<HitObject> result = cursor.AdvanceTo(beatmap, 1200);

        Assert.Same(second, Assert.Single(result));
    }

    [Fact]
    public void SeekTo_AtEventTime_ExcludesEventAtBoundary()
    {
        var beatmap = new Beatmap();

        HitObject atBoundary = new() { ObjectType = 1, Time = 1000 };
        HitObject afterBoundary = new() { ObjectType = 1, Time = 1001 };

        beatmap.HitObjects.AddRange(
            new[] { atBoundary, afterBoundary });

        var cursor = new HitCirclePlaybackCursor();

        cursor.SeekTo(1000);

        Assert.Empty(cursor.AdvanceTo(beatmap, 1000));

        List<HitObject> result = cursor.AdvanceTo(beatmap, 1001);

        Assert.Same(afterBoundary, Assert.Single(result));
    }

    [Fact]
    public void SeekTo_FractionalPosition_PreservesExclusiveBoundary()
    {
        var beatmap = new Beatmap();

        HitObject before = new() { ObjectType = 1, Time = 1000 };
        HitObject after = new() { ObjectType = 1, Time = 1001 };

        beatmap.HitObjects.AddRange(new[] { before, after });

        var cursor = new HitCirclePlaybackCursor();

        cursor.SeekTo(1000.5);

        List<HitObject> result = cursor.AdvanceTo(beatmap, 1001);

        Assert.Same(after, Assert.Single(result));
    }

    [Fact]
    public void SeekTo_BeforeFirstAdvance_SkipsEarlierEvents()
    {
        var beatmap = new Beatmap();

        HitObject earlier = new() { ObjectType = 1, Time = 100 };
        HitObject later = new() { ObjectType = 1, Time = 200 };

        beatmap.HitObjects.AddRange(new[] { earlier, later });

        var cursor = new HitCirclePlaybackCursor();

        cursor.SeekTo(150);

        List<HitObject> result = cursor.AdvanceTo(beatmap, 200);

        Assert.Same(later, Assert.Single(result));
    }

    [Fact]
    public void SeekTo_WhenInvalidValueIsRejected_PreservesCursorPosition()
    {
        var beatmap = new Beatmap();

        HitObject first = new() { ObjectType = 1, Time = 200 };
        HitObject second = new() { ObjectType = 1, Time = 700 };

        beatmap.HitObjects.AddRange(new[] { first, second });

        var cursor = new HitCirclePlaybackCursor();

        Assert.Same(
            first,
            Assert.Single(cursor.AdvanceTo(beatmap, 400)));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            cursor.SeekTo(-2));

        List<HitObject> result = cursor.AdvanceTo(beatmap, 800);

        Assert.Same(second, Assert.Single(result));
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void SeekTo_WhenPositionIsInvalid_ThrowsArgumentOutOfRangeException(
        double invalidPosition)
    {
        var cursor = new HitCirclePlaybackCursor();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                cursor.SeekTo(invalidPosition));

        Assert.Equal(
            "positionMilliseconds",
            exception.ParamName);

        Assert.Contains(
            "The timeline position must be finite and non-negative.",
            exception.Message);
    }

}
