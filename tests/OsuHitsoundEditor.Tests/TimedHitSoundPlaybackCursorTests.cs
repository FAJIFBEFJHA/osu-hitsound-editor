
using OsuHitsoundEditor;

namespace OsuHitsoundEditor.Tests;

public class TimedHitSoundPlaybackCursorTests
{
    [Fact]
    public void AdvanceTo_FirstCall_IncludesZeroAndUpperBoundary()
    {
        var beatmap = new Beatmap();

        HitObject atZero = new() { ObjectType = 1, Time = 0 };
        HitObject atEnd = new() { ObjectType = 1, Time = 1000 };
        HitObject afterEnd = new() { ObjectType = 1, Time = 1001 };

        beatmap.HitObjects.AddRange(new[] { atZero, atEnd, afterEnd });

        var cursor = new TimedHitSoundPlaybackCursor();

        List<TimedHitSoundEvent> result = cursor.AdvanceTo(beatmap, 1000);

        Assert.Equal(2, result.Count);
        Assert.Same(atZero, result[0].HitObject);
        Assert.Same(atEnd, result[1].HitObject);
        Assert.Equal(new double[] { 0, 1000 }, result.Select(e => e.Time));
    }

    [Fact]
    public void AdvanceTo_AdjacentIntervals_DoesNotRepeatEvents()
    {
        var beatmap = new Beatmap();

        beatmap.HitObjects.AddRange(new[]
        {
            new HitObject { ObjectType = 1, Time = 100 },
            new HitObject { ObjectType = 1, Time = 200 },
            new HitObject { ObjectType = 1, Time = 300 }
        });

        var cursor = new TimedHitSoundPlaybackCursor();

        List<TimedHitSoundEvent> first = cursor.AdvanceTo(beatmap, 200);
        List<TimedHitSoundEvent> second = cursor.AdvanceTo(beatmap, 300);

        Assert.Equal(new double[] { 100, 200 }, first.Select(e => e.Time));
        Assert.Equal(new double[] { 300 }, second.Select(e => e.Time));
    }

    [Fact]
    public void AdvanceTo_WhenPositionDoesNotChange_ReturnsEmpty()
    {
        var beatmap = new Beatmap();

        beatmap.HitObjects.Add(
            new HitObject { ObjectType = 1, Time = 500 });

        var cursor = new TimedHitSoundPlaybackCursor();

        Assert.Single(cursor.AdvanceTo(beatmap, 500));

        List<TimedHitSoundEvent> result = cursor.AdvanceTo(beatmap, 500);

        Assert.Empty(result);
    }

    [Fact]
    public void AdvanceTo_WhenBeatmapHasNoEvents_ReturnsEmpty()
    {
        var beatmap = new Beatmap();
        var cursor = new TimedHitSoundPlaybackCursor();

        Assert.Empty(cursor.AdvanceTo(beatmap, 1000));
    }

    [Fact]
    public void AdvanceTo_WhenBeatmapIsNull_ThrowsArgumentNullException()
    {
        var cursor = new TimedHitSoundPlaybackCursor();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                cursor.AdvanceTo(null!, 1000));

        Assert.Equal("beatmap", exception.ParamName);
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AdvanceTo_WhenPositionIsInvalid_ThrowsArgumentOutOfRangeException(
        double invalidPosition)
    {
        var beatmap = new Beatmap();
        var cursor = new TimedHitSoundPlaybackCursor();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                cursor.AdvanceTo(beatmap, invalidPosition));

        Assert.Equal("currentPositionMilliseconds", exception.ParamName);
        Assert.Contains(
            "The timeline position must be finite and non-negative.",
            exception.Message);
    }

    [Fact]
    public void AdvanceTo_WhenMovingBackwards_ThrowsAndPreservesPosition()
    {
        var beatmap = new Beatmap();

        HitObject first = new() { ObjectType = 1, Time = 100 };
        HitObject second = new() { ObjectType = 1, Time = 800 };

        beatmap.HitObjects.AddRange(new[] { first, second });

        var cursor = new TimedHitSoundPlaybackCursor();

        Assert.Same(
            first,
            Assert.Single(cursor.AdvanceTo(beatmap, 500)).HitObject);

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                cursor.AdvanceTo(beatmap, 400));

        Assert.Equal(
            "The timeline position moved backwards. Reset the playback cursor after seeking.",
            exception.Message);

        Assert.Same(
            second,
            Assert.Single(cursor.AdvanceTo(beatmap, 800)).HitObject);
    }

    [Fact]
    public void AdvanceTo_WhenInvalidAdvanceFails_PreservesPosition()
    {
        var beatmap = new Beatmap();

        HitObject first = new() { ObjectType = 1, Time = 100 };
        HitObject second = new() { ObjectType = 1, Time = 600 };

        beatmap.HitObjects.AddRange(new[] { first, second });

        var cursor = new TimedHitSoundPlaybackCursor();

        Assert.Single(cursor.AdvanceTo(beatmap, 200));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            cursor.AdvanceTo(beatmap, double.NaN));

        Assert.Same(
            second,
            Assert.Single(cursor.AdvanceTo(beatmap, 600)).HitObject);
    }

    [Fact]
    public void AdvanceTo_MixedEvents_ReturnsChronologicalEdgesTicksAndCircles()
    {
        var beatmap = new Beatmap
        {
            BeatmapVersion = 14,
            SliderMultiplier = 1.4,
            SliderTickRate = 4
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 0,
            BeatLength = 500,
            IsUninherited = true
        });

        HitObject slider = new()
        {
            ObjectType = 2,
            Time = 1000,
            Length = 140,
            Slides = 2
        };

        HitObject circle = new()
        {
            ObjectType = 1,
            Time = 1250
        };

        beatmap.HitObjects.AddRange(new[] { slider, circle });

        var cursor = new TimedHitSoundPlaybackCursor();

        List<TimedHitSoundEvent> first = cursor.AdvanceTo(beatmap, 1250);
        List<TimedHitSoundEvent> second = cursor.AdvanceTo(beatmap, 1500);
        List<TimedHitSoundEvent> third = cursor.AdvanceTo(beatmap, 2000);

        Assert.Equal(
            new double[] { 1000, 1125, 1250, 1250 },
            first.Select(e => e.Time));

        Assert.Equal(
            new[]
            {
                HitSoundEventType.SliderEdge,
                HitSoundEventType.SliderTick,
                HitSoundEventType.HitCircle,
                HitSoundEventType.SliderTick
            },
            first.Select(e => e.EventType));

        Assert.Same(circle, first[2].HitObject);
        Assert.Equal(0, first[0].EdgeIndex);

        Assert.Equal(
            new double[] { 1375, 1500 },
            second.Select(e => e.Time));

        Assert.Equal(HitSoundEventType.SliderEdge, second[1].EventType);
        Assert.Equal(1, second[1].EdgeIndex);

        Assert.Equal(
            new double[] { 1625, 1750, 1875, 2000 },
            third.Select(e => e.Time));

        Assert.Equal(HitSoundEventType.SliderEdge, third[^1].EventType);
        Assert.Equal(2, third[^1].EdgeIndex);

        Assert.Equal(10, first.Count + second.Count + third.Count);
    }

    [Fact]
    public void AdvanceTo_CoincidentCircles_PreservesBothEvents()
    {
        var beatmap = new Beatmap();

        HitObject first = new() { ObjectType = 1, Time = 1000 };
        HitObject second = new() { ObjectType = 1, Time = 1000 };

        beatmap.HitObjects.AddRange(new[] { first, second });

        var cursor = new TimedHitSoundPlaybackCursor();

        List<TimedHitSoundEvent> result = cursor.AdvanceTo(beatmap, 1000);

        Assert.Equal(2, result.Count);
        Assert.Same(first, result[0].HitObject);
        Assert.Same(second, result[1].HitObject);

        Assert.Empty(cursor.AdvanceTo(beatmap, 1000));
    }

    [Fact]
    public void AdvanceTo_SeparateCursors_MaintainIndependentPositions()
    {
        var beatmap = new Beatmap();

        beatmap.HitObjects.AddRange(new[]
        {
            new HitObject { ObjectType = 1, Time = 100 },
            new HitObject { ObjectType = 1, Time = 200 }
        });

        var firstCursor = new TimedHitSoundPlaybackCursor();
        var secondCursor = new TimedHitSoundPlaybackCursor();

        Assert.Single(firstCursor.AdvanceTo(beatmap, 100));
        Assert.Equal(2, secondCursor.AdvanceTo(beatmap, 200).Count);

        List<TimedHitSoundEvent> pending = firstCursor.AdvanceTo(beatmap, 200);

        Assert.Equal(200.0, Assert.Single(pending).Time);
        Assert.Empty(secondCursor.AdvanceTo(beatmap, 200));
    }

    [Fact]
    public void AdvanceTo_WhenBeatmapQueryFails_PreservesPosition()
    {
        var beatmap = new Beatmap();

        HitObject first = new() { ObjectType = 1, Time = 200 };
        HitObject second = new() { ObjectType = 1, Time = 700 };

        beatmap.HitObjects.AddRange(new[] { first, second });

        var cursor = new TimedHitSoundPlaybackCursor();

        Assert.Single(cursor.AdvanceTo(beatmap, 500));

        HitObject invalidSlider = new()
        {
            ObjectType = 2,
            Time = 600,
            Slides = 0
        };

        beatmap.HitObjects.Add(invalidSlider);

        Assert.Throws<InvalidOperationException>(() =>
            cursor.AdvanceTo(beatmap, 1000));

        beatmap.HitObjects.Remove(invalidSlider);

        List<TimedHitSoundEvent> result = cursor.AdvanceTo(beatmap, 1000);

        Assert.Same(second, Assert.Single(result).HitObject);
    }

    [Fact]
    public void SeekTo_Zero_AllowsReplayingEventsAtZero()
    {
        var beatmap = new Beatmap();

        HitObject atZero = new() { ObjectType = 1, Time = 0 };
        HitObject at500 = new() { ObjectType = 1, Time = 500 };

        beatmap.HitObjects.AddRange(new[] { atZero, at500 });

        var cursor = new TimedHitSoundPlaybackCursor();

        Assert.Equal(2, cursor.AdvanceTo(beatmap, 500).Count);

        cursor.SeekTo(0);

        List<TimedHitSoundEvent> result = cursor.AdvanceTo(beatmap, 0);

        TimedHitSoundEvent replayed = Assert.Single(result);

        Assert.Same(atZero, replayed.HitObject);
        Assert.Equal(0, replayed.Time);

        Assert.Empty(cursor.AdvanceTo(beatmap, 0));
    }

    [Fact]
    public void SeekTo_Forward_SkipsEventsUpToNewPosition()
    {
        var beatmap = new Beatmap();

        HitObject first = new() { ObjectType = 1, Time = 500 };
        HitObject atBoundary = new() { ObjectType = 1, Time = 1000 };
        HitObject afterBoundary = new() { ObjectType = 1, Time = 1200 };

        beatmap.HitObjects.AddRange(
            new[] { first, atBoundary, afterBoundary });

        var cursor = new TimedHitSoundPlaybackCursor();

        cursor.AdvanceTo(beatmap, 500);
        cursor.SeekTo(1000);

        List<TimedHitSoundEvent> result = cursor.AdvanceTo(beatmap, 1200);

        Assert.Same(
            afterBoundary,
            Assert.Single(result).HitObject);
    }

    [Fact]
    public void SeekTo_Backward_AllowsProcessingEventsAgain()
    {
        var beatmap = new Beatmap();

        HitObject first = new() { ObjectType = 1, Time = 500 };
        HitObject second = new() { ObjectType = 1, Time = 1200 };
        HitObject third = new() { ObjectType = 1, Time = 1800 };

        beatmap.HitObjects.AddRange(new[] { first, second, third });

        var cursor = new TimedHitSoundPlaybackCursor();

        Assert.Equal(3, cursor.AdvanceTo(beatmap, 1800).Count);

        cursor.SeekTo(500);

        List<TimedHitSoundEvent> result = cursor.AdvanceTo(beatmap, 1200);

        Assert.Same(second, Assert.Single(result).HitObject);
    }

    [Fact]
    public void SeekTo_AtEventTime_ExcludesEventAtBoundary()
    {
        var beatmap = new Beatmap();

        HitObject atBoundary = new() { ObjectType = 1, Time = 1000 };
        HitObject afterBoundary = new() { ObjectType = 1, Time = 1001 };

        beatmap.HitObjects.AddRange(
            new[] { atBoundary, afterBoundary });

        var cursor = new TimedHitSoundPlaybackCursor();

        cursor.SeekTo(1000);

        Assert.Empty(cursor.AdvanceTo(beatmap, 1000));

        List<TimedHitSoundEvent> result = cursor.AdvanceTo(beatmap, 1001);

        Assert.Same(
            afterBoundary,
            Assert.Single(result).HitObject);
    }

    [Fact]
    public void SeekTo_FractionalPosition_PreservesExclusiveBoundary()
    {
        var beatmap = new Beatmap();

        HitObject before = new() { ObjectType = 1, Time = 1000 };
        HitObject after = new() { ObjectType = 1, Time = 1001 };

        beatmap.HitObjects.AddRange(new[] { before, after });

        var cursor = new TimedHitSoundPlaybackCursor();

        cursor.SeekTo(1000.5);

        List<TimedHitSoundEvent> result = cursor.AdvanceTo(beatmap, 1001);

        Assert.Same(after, Assert.Single(result).HitObject);
    }

    [Fact]
    public void SeekTo_BeforeFirstAdvance_SkipsEarlierEvents()
    {
        var beatmap = new Beatmap();

        HitObject earlier = new() { ObjectType = 1, Time = 100 };
        HitObject later = new() { ObjectType = 1, Time = 200 };

        beatmap.HitObjects.AddRange(new[] { earlier, later });

        var cursor = new TimedHitSoundPlaybackCursor();

        cursor.SeekTo(150);

        List<TimedHitSoundEvent> result = cursor.AdvanceTo(beatmap, 200);

        Assert.Same(later, Assert.Single(result).HitObject);
    }

    [Fact]
    public void SeekTo_WhenInvalidValueIsRejected_PreservesCursorPosition()
    {
        var beatmap = new Beatmap();

        HitObject first = new() { ObjectType = 1, Time = 200 };
        HitObject second = new() { ObjectType = 1, Time = 700 };

        beatmap.HitObjects.AddRange(new[] { first, second });

        var cursor = new TimedHitSoundPlaybackCursor();

        Assert.Same(
            first,
            Assert.Single(cursor.AdvanceTo(beatmap, 400)).HitObject);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            cursor.SeekTo(-2));

        List<TimedHitSoundEvent> result = cursor.AdvanceTo(beatmap, 800);

        Assert.Same(second, Assert.Single(result).HitObject);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void SeekTo_WhenPositionIsInvalid_ThrowsArgumentOutOfRangeException(
        double invalidPosition)
    {
        var cursor = new TimedHitSoundPlaybackCursor();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                cursor.SeekTo(invalidPosition));

        Assert.Equal("positionMilliseconds", exception.ParamName);

        Assert.Contains(
            "The timeline position must be finite and non-negative.",
            exception.Message);
    }

}
