using OsuHitsoundEditor;

namespace OsuHitsoundEditor.Tests;

public class BeatmapTests
{
    [Theory]
    [InlineData(0, SampleSetType.Default)]
    [InlineData(1, SampleSetType.Normal)]
    [InlineData(2, SampleSetType.Soft)]
    [InlineData(3, SampleSetType.Drum)]
    [InlineData(99, SampleSetType.Default)]
    public void GetSampleSetType_ReturnsExpectedType(
        int sampleSet,
        SampleSetType expected)
    {
        Beatmap beatmap = new Beatmap();

        SampleSetType result = beatmap.GetSampleSetType(sampleSet);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, false, false, false)]
    [InlineData(2, true, false, false)]
    [InlineData(4, false, true, false)]
    [InlineData(8, false, false, true)]
    [InlineData(6, true, true, false)]
    [InlineData(10, true, false, true)]
    [InlineData(12, false, true, true)]
    [InlineData(14, true, true, true)]
    public void GetHitSoundTypes_ReturnsExpectedLayers(
        int hitSound,
        bool whistle,
        bool finish,
        bool clap)
    {
        Beatmap beatmap = new Beatmap();

        HitObject hitObject = new HitObject
        {
            HitSound = hitSound
        };

        List<HitSoundType> expected = new List<HitSoundType>
        {
            HitSoundType.Normal
        };

        if (whistle)
        {
            expected.Add(HitSoundType.Whistle);
        }

        if (finish)
        {
            expected.Add(HitSoundType.Finish);
        }

        if (clap)
        {
            expected.Add(HitSoundType.Clap);
        }

        List<HitSoundType> result = beatmap.GetHitSoundTypes(hitObject);

        Assert.Equal(expected, result);
    }
    [Fact]
    public void GetActiveTimingPoint_ReturnsLatestTimingPointBeforeHitObject()
    {
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000
        });

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 2000
        });

        HitObject hitObject = new HitObject
        {
            Time = 2500
        };

        TimingPoint? result = beatmap.GetActiveTimingPoint(hitObject.Time);

        Assert.NotNull(result);
        Assert.Equal(2000, result.Time);
    }

    [Fact]
    public void GetEffectiveSampleIndex_InheritsFromTimingPointWhenIndexIsZero()
    {
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            SampleIndex = 4
        });

        HitObject hitObject = new HitObject
        {
            Time = 1500,
            HitSample = new HitSample
            {
                Index = 0
            }
        };

        int result = beatmap.GetEffectiveSampleIndex(hitObject);

        Assert.Equal(4, result);
    }

    [Fact]
    public void GetEffectiveVolume_InheritsFromTimingPointWhenVolumeIsZero()
    {
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            Volume = 70
        });

        HitObject hitObject = new HitObject
        {
            Time = 1500,
            HitSample = new HitSample
            {
                Volume = 0
            }
        };

        int result = beatmap.GetEffectiveVolume(hitObject);

        Assert.Equal(70, result);
    }

    [Fact]
    public void GetEffectiveNormalSet_InheritsFromTimingPointWhenNormalSetIsZero()
    {
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            SampleSet = 2
        });

        HitObject hitObject = new HitObject
        {
            Time = 1500,
            HitSample = new HitSample
            {
                NormalSet = 0
            }
        };

        int result = beatmap.GetEffectiveNormalSet(hitObject);

        Assert.Equal(2, result);
    }

    [Fact]
    public void GetEffectiveAdditionSet_InheritsFromEffectiveNormalSet()
    {
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            SampleSet = 2
        });

        HitObject hitObject = new HitObject
        {
            Time = 1500,
            HitSample = new HitSample
            {
                NormalSet = 0,
                AdditionSet = 0
            }
        };

        int result = beatmap.GetEffectiveAdditionSet(hitObject);

        Assert.Equal(2, result);
    }

    [Fact]
    public void GetEffectiveNormalSetType_ReturnsInheritedSampleSetType()
    {
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            SampleSet = 2
        });

        HitObject hitObject = new HitObject
        {
            Time = 1500,
            HitSample = new HitSample
            {
                NormalSet = 0
            }
        };

        SampleSetType result = beatmap.GetEffectiveNormalSetType(hitObject);

        Assert.Equal(SampleSetType.Soft, result);
    }

    [Fact]
    public void GetEffectiveAdditionSetType_ReturnsInheritedNormalSetType()
    {
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            SampleSet = 3
        });

        HitObject hitObject = new HitObject
        {
            Time = 1500,
            HitSample = new HitSample
            {
                NormalSet = 0,
                AdditionSet = 0
            }
        };

        SampleSetType result = beatmap.GetEffectiveAdditionSetType(hitObject);

        Assert.Equal(SampleSetType.Drum, result);
    }
}