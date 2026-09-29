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
    [Fact]
    public void GetHitSoundLayers_NormalOnly_ReturnsSingleNormalLayer()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        HitObject hitObject = new HitObject
        {
            Time = 1000,
            HitSound = 0,
            HitSample = new HitSample
            {
                NormalSet = 1,
                AdditionSet = 0,
                Index = 3,
                Volume = 80
            }
        };

        // Act
        List<HitSoundLayer> layers = beatmap.GetHitSoundLayers(hitObject);

        // Assert
        Assert.Single(layers);

        Assert.Equal(HitSoundType.Normal, layers[0].Type);
        Assert.Equal(SampleSetType.Normal, layers[0].SampleSet);
        Assert.Equal(3, layers[0].SampleIndex);
        Assert.Equal(80, layers[0].Volume);
    }
    [Fact]
    public void GetHitSoundLayers_NormalAndClap_UsesCorrectSampleSets()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        HitObject hitObject = new HitObject
        {
            Time = 1000,
            HitSound = 8,
            HitSample = new HitSample
            {
                NormalSet = 1,
                AdditionSet = 2,
                Index = 4,
                Volume = 70
            }
        };

        // Act
        List<HitSoundLayer> layers = beatmap.GetHitSoundLayers(hitObject);

        // Assert
        Assert.Equal(2, layers.Count);

        Assert.Equal(HitSoundType.Normal, layers[0].Type);
        Assert.Equal(SampleSetType.Normal, layers[0].SampleSet);
        Assert.Equal(4, layers[0].SampleIndex);
        Assert.Equal(70, layers[0].Volume);

        Assert.Equal(HitSoundType.Clap, layers[1].Type);
        Assert.Equal(SampleSetType.Soft, layers[1].SampleSet);
        Assert.Equal(4, layers[1].SampleIndex);
        Assert.Equal(70, layers[1].Volume);
    }
    [Fact]
    public void GetHitSoundLayers_AllAdditions_ReturnsFourLayers()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        HitObject hitObject = new HitObject
        {
            Time = 1000,
            HitSound = 14,
            HitSample = new HitSample
            {
                NormalSet = 3,
                AdditionSet = 2,
                Index = 5,
                Volume = 60
            }
        };

        // Act
        List<HitSoundLayer> layers = beatmap.GetHitSoundLayers(hitObject);

        // Assert
        Assert.Equal(4, layers.Count);

        Assert.Equal(HitSoundType.Normal, layers[0].Type);
        Assert.Equal(SampleSetType.Drum, layers[0].SampleSet);

        Assert.Equal(HitSoundType.Whistle, layers[1].Type);
        Assert.Equal(SampleSetType.Soft, layers[1].SampleSet);

        Assert.Equal(HitSoundType.Finish, layers[2].Type);
        Assert.Equal(SampleSetType.Soft, layers[2].SampleSet);

        Assert.Equal(HitSoundType.Clap, layers[3].Type);
        Assert.Equal(SampleSetType.Soft, layers[3].SampleSet);

        foreach (HitSoundLayer layer in layers)
        {
            Assert.Equal(5, layer.SampleIndex);
            Assert.Equal(60, layer.Volume);
        }
    }
    [Fact]
    public void GetHitSoundLayers_CustomFilename_ReplacesAdditionLayer()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        HitObject hitObject = new HitObject
        {
            Time = 1000,
            HitSound = 8,
            HitSample = new HitSample
            {
                NormalSet = 1,
                AdditionSet = 2,
                Index = 4,
                Volume = 70,
                Filename = "custom.wav"
            }
        };

        // Act
        List<HitSoundLayer> layers = beatmap.GetHitSoundLayers(hitObject);

        // Assert
        Assert.Equal(2, layers.Count);

        Assert.Equal(HitSoundType.Normal, layers[0].Type);
        Assert.Equal(SampleSetType.Normal, layers[0].SampleSet);
        Assert.Equal(4, layers[0].SampleIndex);
        Assert.Equal(70, layers[0].Volume);

        Assert.Equal(HitSoundType.Custom, layers[1].Type);
        Assert.Equal("custom.wav", layers[1].Filename);
        Assert.Equal(70, layers[1].Volume);

        Assert.DoesNotContain(
            layers,
            layer => layer.Type == HitSoundType.Clap
        );
    }
    [Fact]
    public void GetEffectiveNormalSet_SliderEdgeWithOwnSet_ReturnsOwnSet()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        SliderEdge sliderEdge = new SliderEdge
        {
            NormalSet = 3
        };

        // Act
        int result = beatmap.GetEffectiveNormalSet(sliderEdge, 2500);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void GetEffectiveNormalSet_SliderEdgeWithoutOwnSet_InheritsTimingPointSet()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            SampleSet = 2
        });

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 2000,
            SampleSet = 3
        });

        SliderEdge sliderEdge = new SliderEdge
        {
            NormalSet = 0
        };

        // Act
        int result = beatmap.GetEffectiveNormalSet(sliderEdge, 2500);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void GetEffectiveAdditionSet_SliderEdgeWithOwnSet_ReturnsOwnSet()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        SliderEdge sliderEdge = new SliderEdge
        {
            NormalSet = 1,
            AdditionSet = 2
        };

        // Act
        int result = beatmap.GetEffectiveAdditionSet(sliderEdge, 2500);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void GetEffectiveAdditionSet_SliderEdgeWithoutOwnSet_InheritsEffectiveNormalSet()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 2000,
            SampleSet = 3
        });

        SliderEdge sliderEdge = new SliderEdge
        {
            NormalSet = 0,
            AdditionSet = 0
        };

        // Act
        int result = beatmap.GetEffectiveAdditionSet(sliderEdge, 2500);

        // Assert
        Assert.Equal(3, result);
    }
    [Fact]
    public void GetEffectiveNormalSetType_SliderEdge_ReturnsConvertedEffectiveSet()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 2000,
            SampleSet = 3
        });

        SliderEdge sliderEdge = new SliderEdge
        {
            NormalSet = 0
        };

        // Act
        SampleSetType result =
            beatmap.GetEffectiveNormalSetType(sliderEdge, 2500);

        // Assert
        Assert.Equal(SampleSetType.Drum, result);
    }

    [Fact]
    public void GetEffectiveAdditionSetType_SliderEdge_ReturnsConvertedEffectiveSet()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        SliderEdge sliderEdge = new SliderEdge
        {
            NormalSet = 1,
            AdditionSet = 2
        };

        // Act
        SampleSetType result =
            beatmap.GetEffectiveAdditionSetType(sliderEdge, 2500);

        // Assert
        Assert.Equal(SampleSetType.Soft, result);
    }
    [Fact]
    public void GetEffectiveSampleIndex_HitSampleWithOwnIndex_ReturnsOwnIndex()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 2000,
            SampleIndex = 4
        });

        HitSample hitSample = new HitSample
        {
            Index = 6
        };

        // Act
        int result = beatmap.GetEffectiveSampleIndex(hitSample, 2500);

        // Assert
        Assert.Equal(6, result);
    }

    [Fact]
    public void GetEffectiveSampleIndex_HitSampleWithoutOwnIndex_InheritsTimingPointIndex()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 2000,
            SampleIndex = 4
        });

        HitSample hitSample = new HitSample
        {
            Index = 0
        };

        // Act
        int result = beatmap.GetEffectiveSampleIndex(hitSample, 2500);

        // Assert
        Assert.Equal(4, result);
    }

    [Fact]
    public void GetEffectiveVolume_HitSampleWithOwnVolume_ReturnsOwnVolume()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 2000,
            Volume = 60
        });

        HitSample hitSample = new HitSample
        {
            Volume = 80
        };

        // Act
        int result = beatmap.GetEffectiveVolume(hitSample, 2500);

        // Assert
        Assert.Equal(80, result);
    }

    [Fact]
    public void GetEffectiveVolume_HitSampleWithoutOwnVolume_InheritsTimingPointVolume()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 2000,
            Volume = 60
        });

        HitSample hitSample = new HitSample
        {
            Volume = 0
        };

        // Act
        int result = beatmap.GetEffectiveVolume(hitSample, 2500);

        // Assert
        Assert.Equal(60, result);
    }
    [Fact]
    public void GetHitSoundLayers_SliderEdgeNormalOnly_ReturnsNormalLayer()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 2000,
            SampleSet = 2,
            SampleIndex = 4,
            Volume = 70
        });

        SliderEdge sliderEdge = new SliderEdge
        {
            HitSound = 0,
            NormalSet = 0,
            AdditionSet = 0
        };

        HitSample hitSample = new HitSample
        {
            Index = 0,
            Volume = 0
        };

        // Act
        List<HitSoundLayer> layers =
            beatmap.GetHitSoundLayers(sliderEdge, hitSample, 2500);

        // Assert
        Assert.Single(layers);

        Assert.Equal(HitSoundType.Normal, layers[0].Type);
        Assert.Equal(SampleSetType.Soft, layers[0].SampleSet);
        Assert.Equal(4, layers[0].SampleIndex);
        Assert.Equal(70, layers[0].Volume);
    }
    [Fact]
    public void GetHitSoundLayers_SliderEdgeNormalAndClap_UsesCorrectSampleSets()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        SliderEdge sliderEdge = new SliderEdge
        {
            HitSound = 8,
            NormalSet = 1,
            AdditionSet = 3
        };

        HitSample hitSample = new HitSample
        {
            Index = 5,
            Volume = 80
        };

        // Act
        List<HitSoundLayer> layers =
            beatmap.GetHitSoundLayers(sliderEdge, hitSample, 2500);

        // Assert
        Assert.Equal(2, layers.Count);

        Assert.Equal(HitSoundType.Normal, layers[0].Type);
        Assert.Equal(SampleSetType.Normal, layers[0].SampleSet);
        Assert.Equal(5, layers[0].SampleIndex);
        Assert.Equal(80, layers[0].Volume);

        Assert.Equal(HitSoundType.Clap, layers[1].Type);
        Assert.Equal(SampleSetType.Drum, layers[1].SampleSet);
        Assert.Equal(5, layers[1].SampleIndex);
        Assert.Equal(80, layers[1].Volume);
    }
    [Fact]
    public void GetHitSoundLayers_SliderEdgeWithCustomFilename_ReplacesAdditionalLayer()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        SliderEdge sliderEdge = new SliderEdge
        {
            HitSound = 8,
            NormalSet = 1,
            AdditionSet = 3
        };

        HitSample hitSample = new HitSample
        {
            Index = 5,
            Volume = 80,
            Filename = "snare.wav"
        };

        // Act
        List<HitSoundLayer> layers =
            beatmap.GetHitSoundLayers(sliderEdge, hitSample, 2500);

        // Assert
        Assert.Equal(2, layers.Count);

        Assert.Equal(HitSoundType.Normal, layers[0].Type);
        Assert.Equal(SampleSetType.Normal, layers[0].SampleSet);
        Assert.Equal(5, layers[0].SampleIndex);
        Assert.Equal(80, layers[0].Volume);

        Assert.Equal(HitSoundType.Custom, layers[1].Type);
        Assert.Equal("snare.wav", layers[1].Filename);
        Assert.Equal(80, layers[1].Volume);
    }
}