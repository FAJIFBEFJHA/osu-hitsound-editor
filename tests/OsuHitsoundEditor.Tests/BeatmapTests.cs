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
    [Fact]
    public void GetEffectiveNormalSetType_HitObjectWithDefaultTimingSet_UsesBeatmapDefaultSampleSet()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            DefaultSampleSet = SampleSetType.Soft
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            SampleSet = 0
        });

        HitObject hitObject = new HitObject
        {
            Time = 1500,
            HitSample = new HitSample
            {
                NormalSet = 0
            }
        };

        // Act
        SampleSetType result = beatmap.GetEffectiveNormalSetType(hitObject);

        // Assert
        Assert.Equal(SampleSetType.Soft, result);
    }

    [Fact]
    public void GetEffectiveNormalSetType_SliderEdgeWithDefaultTimingSet_UsesBeatmapDefaultSampleSet()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            DefaultSampleSet = SampleSetType.Drum
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 2000,
            SampleSet = 0
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
    public void GetActiveUninheritedTimingPoint_ReturnsLatestUninheritedPointBeforeTime()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        TimingPoint firstUninherited = new TimingPoint
        {
            Time = 1000,
            BeatLength = 500,
            IsUninherited = true
        };

        TimingPoint inherited = new TimingPoint
        {
            Time = 1500,
            BeatLength = -50,
            IsUninherited = false
        };

        beatmap.TimingPoints.Add(firstUninherited);
        beatmap.TimingPoints.Add(inherited);

        // Act
        TimingPoint? result =
            beatmap.GetActiveUninheritedTimingPoint(1700);

        // Assert
        Assert.Same(firstUninherited, result);
    }

    [Fact]
    public void GetActiveInheritedTimingPoint_WhenInheritedPointIsActive_ReturnsIt()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            BeatLength = 500,
            IsUninherited = true
        });

        TimingPoint inherited = new TimingPoint
        {
            Time = 1500,
            BeatLength = -50,
            IsUninherited = false
        };

        beatmap.TimingPoints.Add(inherited);

        // Act
        TimingPoint? result =
            beatmap.GetActiveInheritedTimingPoint(1700);

        // Assert
        Assert.Same(inherited, result);
    }

    [Fact]
    public void GetActiveInheritedTimingPoint_WhenUninheritedPointIsActive_ReturnsNull()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            BeatLength = 500,
            IsUninherited = true
        });

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1500,
            BeatLength = -50,
            IsUninherited = false
        });

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 2000,
            BeatLength = 400,
            IsUninherited = true
        });

        // Act
        TimingPoint? result =
            beatmap.GetActiveInheritedTimingPoint(2300);

        // Assert
        Assert.Null(result);
    }
    [Fact]
    public void GetEffectiveSliderVelocityMultiplier_WithoutInheritedTimingPoint_ReturnsOne()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            BeatLength = 500,
            IsUninherited = true
        });

        // Act
        double result = beatmap.GetEffectiveSliderVelocityMultiplier(1500);

        // Assert
        Assert.Equal(1.0, result);
    }

    [Theory]
    [InlineData(-100, 1.0)]
    [InlineData(-50, 2.0)]
    [InlineData(-25, 4.0)]
    [InlineData(-200, 0.5)]
    public void GetEffectiveSliderVelocityMultiplier_WithInheritedTimingPoint_ReturnsExpectedMultiplier(
        double beatLength,
        double expectedMultiplier)
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            BeatLength = 500,
            IsUninherited = true
        });

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1500,
            BeatLength = beatLength,
            IsUninherited = false
        });

        // Act
        double result = beatmap.GetEffectiveSliderVelocityMultiplier(1700);

        // Assert
        Assert.Equal(expectedMultiplier, result);
    }
    [Fact]
    public void GetSliderSpanDuration_WithNormalVelocity_ReturnsExpectedDuration()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            SliderMultiplier = 1.4
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            BeatLength = 500,
            IsUninherited = true
        });

        HitObject slider = new HitObject
        {
            Time = 1500,
            Length = 280
        };

        // Act
        double result = beatmap.GetSliderSpanDuration(slider);

        // Assert
        Assert.Equal(1000, result);
    }

    [Fact]
    public void GetSliderSpanDuration_WithDoubleSliderVelocity_ReturnsHalfDuration()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            SliderMultiplier = 1.4
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            BeatLength = 500,
            IsUninherited = true
        });

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1200,
            BeatLength = -50,
            IsUninherited = false
        });

        HitObject slider = new HitObject
        {
            Time = 1500,
            Length = 280
        };

        // Act
        double result = beatmap.GetSliderSpanDuration(slider);

        // Assert
        Assert.Equal(500, result);
    }
    [Fact]
    public void GetSliderSpanDuration_WithoutUninheritedTimingPoint_ThrowsInvalidOperationException()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            SliderMultiplier = 1.4
        };

        HitObject slider = new HitObject
        {
            Time = 1500,
            Length = 280
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => beatmap.GetSliderSpanDuration(slider));
    }
    [Fact]
    public void GetSliderDuration_WithOneSpan_ReturnsSpanDuration()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            SliderMultiplier = 1.4
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            BeatLength = 500,
            IsUninherited = true
        });

        HitObject slider = new HitObject
        {
            Time = 1500,
            Length = 280,
            Slides = 1
        };

        // Act
        double result = beatmap.GetSliderDuration(slider);

        // Assert
        Assert.Equal(1000, result);
    }

    [Fact]
    public void GetSliderDuration_WithMultipleSpans_ReturnsTotalDuration()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            SliderMultiplier = 1.4
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            BeatLength = 500,
            IsUninherited = true
        });

        HitObject slider = new HitObject
        {
            Time = 1500,
            Length = 280,
            Slides = 3
        };

        // Act
        double result = beatmap.GetSliderDuration(slider);

        // Assert
        Assert.Equal(3000, result);
    }

    [Fact]
    public void GetSliderDuration_WithDoubleSliderVelocity_UsesEffectiveSpanDuration()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            SliderMultiplier = 1.4
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            BeatLength = 500,
            IsUninherited = true
        });

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1200,
            BeatLength = -50,
            IsUninherited = false
        });

        HitObject slider = new HitObject
        {
            Time = 1500,
            Length = 280,
            Slides = 2
        };

        // Act
        double result = beatmap.GetSliderDuration(slider);

        // Assert
        Assert.Equal(1000, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetSliderDuration_WithInvalidSlides_ThrowsInvalidOperationException(int slides)
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        HitObject slider = new HitObject
        {
            Slides = slides
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => beatmap.GetSliderDuration(slider));
    }
    [Theory]
    [InlineData(0, 1500)]
    [InlineData(1, 2000)]
    [InlineData(2, 2500)]
    [InlineData(3, 3000)]
    public void GetSliderEdgeTime_WithValidEdgeIndex_ReturnsExpectedTime(
    int edgeIndex,
    double expectedTime)
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            SliderMultiplier = 1.4
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            BeatLength = 500,
            IsUninherited = true
        });

        HitObject slider = new HitObject
        {
            Time = 1500,
            Length = 140,
            Slides = 3
        };

        // Act
        double result = beatmap.GetSliderEdgeTime(slider, edgeIndex);

        // Assert
        Assert.Equal(expectedTime, result);
    }

    [Fact]
    public void GetSliderEdgeTime_WithInvalidSlides_ThrowsInvalidOperationException()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        HitObject slider = new HitObject
        {
            Slides = 0
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => beatmap.GetSliderEdgeTime(slider, 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void GetSliderEdgeTime_WithOutOfRangeEdgeIndex_ThrowsArgumentOutOfRangeException(
        int edgeIndex)
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        HitObject slider = new HitObject
        {
            Slides = 3
        };

        // Act
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => beatmap.GetSliderEdgeTime(slider, edgeIndex));

        // Assert
        Assert.Equal(nameof(edgeIndex), exception.ParamName);
        Assert.Equal(edgeIndex, exception.ActualValue);
    }
    [Fact]
    public void GetSliderEdgeHitSoundLayers_WithValidEdge_ReturnsExpectedLayers()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            SliderMultiplier = 1.4
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            BeatLength = 500,
            SampleSet = 1,
            SampleIndex = 2,
            Volume = 80,
            IsUninherited = true
        });

        HitObject slider = new HitObject
        {
            Time = 1500,
            Length = 140,
            Slides = 1,
            HitSample = new HitSample()
        };

        slider.SliderEdges.Add(new SliderEdge
        {
            HitSound = 0,
            NormalSet = 0,
            AdditionSet = 0
        });

        slider.SliderEdges.Add(new SliderEdge
        {
            HitSound = 8,
            NormalSet = 0,
            AdditionSet = 0
        });

        // Act
        List<HitSoundLayer> result =
            beatmap.GetSliderEdgeHitSoundLayers(slider, 1);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(HitSoundType.Normal, result[0].Type);
        Assert.Equal(HitSoundType.Clap, result[1].Type);

        Assert.Equal(SampleSetType.Normal, result[0].SampleSet);
        Assert.Equal(SampleSetType.Normal, result[1].SampleSet);

        Assert.Equal(2, result[0].SampleIndex);
        Assert.Equal(80, result[0].Volume);
    }
    [Fact]
    public void GetSliderEdgeHitSoundLayers_UsesTimingPointAtEdgeTime()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            SliderMultiplier = 1.4
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            BeatLength = 500,
            SampleSet = 1,
            SampleIndex = 2,
            Volume = 80,
            IsUninherited = true
        });

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1800,
            BeatLength = -100,
            SampleSet = 2,
            SampleIndex = 5,
            Volume = 60,
            IsUninherited = false
        });

        HitObject slider = new HitObject
        {
            Time = 1500,
            Length = 140,
            Slides = 2,
            HitSample = new HitSample()
        };

        slider.SliderEdges.Add(new SliderEdge());
        slider.SliderEdges.Add(new SliderEdge());
        slider.SliderEdges.Add(new SliderEdge());

        // Act
        List<HitSoundLayer> result =
            beatmap.GetSliderEdgeHitSoundLayers(slider, 1);

        // Assert
        Assert.Single(result);

        Assert.Equal(SampleSetType.Soft, result[0].SampleSet);
        Assert.Equal(5, result[0].SampleIndex);
        Assert.Equal(60, result[0].Volume);
    }
    [Fact]
    public void GetSliderEdgeHitSoundLayers_WithIncorrectEdgeCount_ThrowsInvalidOperationException()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            SliderMultiplier = 1.4
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            BeatLength = 500,
            IsUninherited = true
        });

        HitObject slider = new HitObject
        {
            Time = 1500,
            Length = 140,
            Slides = 2
        };

        slider.SliderEdges.Add(new SliderEdge());
        slider.SliderEdges.Add(new SliderEdge());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => beatmap.GetSliderEdgeHitSoundLayers(slider, 1));
    }
    [Fact]
    public void GetSliderBodyHitSoundLayers_WithoutWhistle_ReturnsSliderSlideOnly()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            SampleSet = 2,
            SampleIndex = 4,
            Volume = 70,
            IsUninherited = true
        });

        HitObject slider = new HitObject
        {
            Time = 1500,
            HitSound = 0,
            HitSample = new HitSample()
        };

        // Act
        List<HitSoundLayer> result =
            beatmap.GetSliderBodyHitSoundLayers(slider);

        // Assert
        Assert.Single(result);

        Assert.Equal(HitSoundType.SliderSlide, result[0].Type);
        Assert.Equal(SampleSetType.Soft, result[0].SampleSet);
        Assert.Equal(4, result[0].SampleIndex);
        Assert.Equal(70, result[0].Volume);
    }

    [Fact]
    public void GetSliderBodyHitSoundLayers_WithWhistle_ReturnsSlideAndWhistle()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            SampleSet = 1,
            SampleIndex = 3,
            Volume = 80,
            IsUninherited = true
        });

        HitObject slider = new HitObject
        {
            Time = 1500,
            HitSound = 2,
            HitSample = new HitSample
            {
                NormalSet = 2,
                AdditionSet = 3
            }
        };

        // Act
        List<HitSoundLayer> result =
            beatmap.GetSliderBodyHitSoundLayers(slider);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(HitSoundType.SliderSlide, result[0].Type);
        Assert.Equal(SampleSetType.Soft, result[0].SampleSet);

        Assert.Equal(HitSoundType.SliderWhistle, result[1].Type);
        Assert.Equal(SampleSetType.Drum, result[1].SampleSet);

        Assert.Equal(3, result[0].SampleIndex);
        Assert.Equal(3, result[1].SampleIndex);

        Assert.Equal(80, result[0].Volume);
        Assert.Equal(80, result[1].Volume);
    }

    [Fact]
    public void GetSliderBodyHitSoundLayers_WithFinishAndClap_DoesNotCreateContinuousFinishOrClapLayers()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            SampleSet = 1,
            SampleIndex = 2,
            Volume = 60,
            IsUninherited = true
        });

        HitObject slider = new HitObject
        {
            Time = 1500,
            HitSound = 12,
            HitSample = new HitSample()
        };

        // Act
        List<HitSoundLayer> result =
            beatmap.GetSliderBodyHitSoundLayers(slider);

        // Assert
        Assert.Single(result);
        Assert.Equal(HitSoundType.SliderSlide, result[0].Type);
    }
}