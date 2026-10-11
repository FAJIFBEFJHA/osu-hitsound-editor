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
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(14)]
    public void GetHitSoundLayers_CustomFilename_ReturnsCustomOnly(int hitSound)
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        HitObject hitObject = new HitObject
        {
            Time = 1000,
            HitSound = hitSound,
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
        HitSoundLayer layer = Assert.Single(layers);

        Assert.Equal(HitSoundType.Custom, layer.Type);
        Assert.Equal("custom.wav", layer.Filename);
        Assert.Equal(70, layer.Volume);

        Assert.Equal(hitSound, hitObject.HitSound);
        Assert.Equal("custom.wav", hitObject.HitSample.Filename);
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

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 0,
            SampleIndex = 5,
            Volume = 80
        });
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
    public void GetHitSoundLayers_SliderEdge_IgnoresSliderHitSampleIndexVolumeAndFilename()
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
            HitSound = 8,
            NormalSet = 1,
            AdditionSet = 3
        };

        HitSample hitSample = new HitSample
        {
            Index = 9,
            Volume = 20,
            Filename = "custom.wav"
        };

        // Act
        List<HitSoundLayer> layers =
            beatmap.GetHitSoundLayers(sliderEdge, hitSample, 2500);

        // Assert
        Assert.Equal(2, layers.Count);

        Assert.Equal(HitSoundType.Normal, layers[0].Type);
        Assert.Equal(SampleSetType.Normal, layers[0].SampleSet);
        Assert.Equal(4, layers[0].SampleIndex);
        Assert.Equal(70, layers[0].Volume);

        Assert.Equal(HitSoundType.Clap, layers[1].Type);
        Assert.Equal(SampleSetType.Drum, layers[1].SampleSet);
        Assert.Equal(4, layers[1].SampleIndex);
        Assert.Equal(70, layers[1].Volume);

        Assert.DoesNotContain(
            layers,
            layer => layer.Type == HitSoundType.Custom
        );

        Assert.Equal(9, hitSample.Index);
        Assert.Equal(20, hitSample.Volume);
        Assert.Equal("custom.wav", hitSample.Filename);
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
    [Fact]
    public void GetSliderTickDistance_Version14WithoutInheritedTimingPoint_ReturnsBaseDistance()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            BeatmapVersion = 14,
            SliderMultiplier = 1.4,
            SliderTickRate = 1
        };

        HitObject slider = new HitObject
        {
            Time = 1000
        };

        // Act
        double result = beatmap.GetSliderTickDistance(slider);

        // Assert
        Assert.Equal(140, result);
    }

    [Fact]
    public void GetSliderTickDistance_Version14WithDoubleSliderVelocity_AppliesSliderVelocity()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            BeatmapVersion = 14,
            SliderMultiplier = 1.4,
            SliderTickRate = 1
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            BeatLength = -50,
            IsUninherited = false
        });

        HitObject slider = new HitObject
        {
            Time = 1500
        };

        // Act
        double result = beatmap.GetSliderTickDistance(slider);

        // Assert
        Assert.Equal(280, result);
    }

    [Fact]
    public void GetSliderTickDistance_Version7WithDoubleSliderVelocity_IgnoresSliderVelocity()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            BeatmapVersion = 7,
            SliderMultiplier = 1.4,
            SliderTickRate = 1
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            BeatLength = -50,
            IsUninherited = false
        });

        HitObject slider = new HitObject
        {
            Time = 1500
        };

        // Act
        double result = beatmap.GetSliderTickDistance(slider);

        // Assert
        Assert.Equal(140, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetSliderTickDistance_InvalidSliderTickRate_Throws(double sliderTickRate)
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            BeatmapVersion = 14,
            SliderMultiplier = 1.4,
            SliderTickRate = sliderTickRate
        };

        HitObject slider = new HitObject
        {
            Time = 1000
        };

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => beatmap.GetSliderTickDistance(slider));

        // Assert
        Assert.Equal(
            "It is not possible to calculate slider tick distance because SliderTickRate must be greater than zero.",
            exception.Message);
    }
    [Fact]
    public void GetSliderTickTimes_OneSpan_ReturnsTickTimes()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            BeatmapVersion = 14,
            SliderMultiplier = 1,
            SliderTickRate = 1
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 0,
            BeatLength = 500,
            IsUninherited = true
        });

        HitObject slider = new HitObject
        {
            Time = 1000,
            Length = 300,
            Slides = 1
        };

        // Act
        List<double> result = beatmap.GetSliderTickTimes(slider);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1500, result[0]);
        Assert.Equal(2000, result[1]);
    }

    [Fact]
    public void GetSliderTickTimes_TwoSpans_ReturnsTicksInChronologicalOrder()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            BeatmapVersion = 14,
            SliderMultiplier = 1,
            SliderTickRate = 1
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 0,
            BeatLength = 500,
            IsUninherited = true
        });

        HitObject slider = new HitObject
        {
            Time = 1000,
            Length = 300,
            Slides = 2
        };

        // Act
        List<double> result = beatmap.GetSliderTickTimes(slider);

        // Assert
        Assert.Equal(4, result.Count);

        Assert.Equal(1500, result[0]);
        Assert.Equal(2000, result[1]);
        Assert.Equal(3000, result[2]);
        Assert.Equal(3500, result[3]);
    }

    [Fact]
    public void GetSliderTickTimes_TickAtEnd_IsNotGenerated()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            BeatmapVersion = 14,
            SliderMultiplier = 1,
            SliderTickRate = 1
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 0,
            BeatLength = 500,
            IsUninherited = true
        });

        HitObject slider = new HitObject
        {
            Time = 1000,
            Length = 100,
            Slides = 1
        };

        // Act
        List<double> result = beatmap.GetSliderTickTimes(slider);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetSliderTickTimes_InvalidSlides_Throws()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        HitObject slider = new HitObject
        {
            Slides = 0
        };

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => beatmap.GetSliderTickTimes(slider));

        // Assert
        Assert.Equal(
            "It is not possible to calculate slider tick times because the slider must have at least one span.",
            exception.Message);
    }
    [Fact]
    public void GetSliderTickHitSoundLayers_ExplicitNormalSet_UsesTimingPointSampleValues()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 0,
            SampleIndex = 3,
            Volume = 70
        });
        HitObject slider = new HitObject
        {
            Time = 1000,
            HitSample = new HitSample
            {
                NormalSet = 2,
                Index = 3,
                Volume = 70
            }
        };

        // Act
        List<HitSoundLayer> result = beatmap.GetSliderTickHitSoundLayers(slider);

        // Assert
        HitSoundLayer layer = Assert.Single(result);

        Assert.Equal(HitSoundType.SliderTick, layer.Type);
        Assert.Equal(SampleSetType.Soft, layer.SampleSet);
        Assert.Equal(3, layer.SampleIndex);
        Assert.Equal(70, layer.Volume);
    }

    [Fact]
    public void GetSliderTickHitSoundLayers_InheritedValues_UsesTimingPointValues()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 0,
            BeatLength = 500,
            IsUninherited = true,
            SampleSet = 3,
            SampleIndex = 4,
            Volume = 65
        });

        HitObject slider = new HitObject
        {
            Time = 1000,
            HitSample = new HitSample()
        };

        // Act
        List<HitSoundLayer> result = beatmap.GetSliderTickHitSoundLayers(slider);

        // Assert
        HitSoundLayer layer = Assert.Single(result);

        Assert.Equal(HitSoundType.SliderTick, layer.Type);
        Assert.Equal(SampleSetType.Drum, layer.SampleSet);
        Assert.Equal(4, layer.SampleIndex);
        Assert.Equal(65, layer.Volume);
    }

    [Fact]
    public void GetSliderTickHitSoundLayers_DifferentAdditionSet_UsesNormalSet()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 0,
            SampleIndex = 2,
            Volume = 80
        });
        HitObject slider = new HitObject
        {
            Time = 1000,
            HitSample = new HitSample
            {
                NormalSet = 2,
                AdditionSet = 3,
                Index = 2,
                Volume = 80
            }
        };

        // Act
        List<HitSoundLayer> result = beatmap.GetSliderTickHitSoundLayers(slider);

        // Assert
        HitSoundLayer layer = Assert.Single(result);

        Assert.Equal(HitSoundType.SliderTick, layer.Type);
        Assert.Equal(SampleSetType.Soft, layer.SampleSet);
        Assert.Equal(2, layer.SampleIndex);
        Assert.Equal(80, layer.Volume);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetSliderSpanDuration_InvalidSliderMultiplier_Throws(double sliderMultiplier)
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            SliderMultiplier = sliderMultiplier
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 0,
            BeatLength = 500,
            IsUninherited = true
        });

        HitObject slider = new HitObject
        {
            Time = 1000,
            Length = 100
        };

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => beatmap.GetSliderSpanDuration(slider));

        // Assert
        Assert.Equal(
            "It is not possible to calculate slider span duration because SliderMultiplier must be greater than zero.",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetSliderTickDistance_InvalidSliderMultiplier_Throws(double sliderMultiplier)
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            BeatmapVersion = 14,
            SliderMultiplier = sliderMultiplier,
            SliderTickRate = 1
        };

        HitObject slider = new HitObject
        {
            Time = 1000
        };

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => beatmap.GetSliderTickDistance(slider));

        // Assert
        Assert.Equal(
            "It is not possible to calculate slider tick distance because SliderMultiplier must be greater than zero.",
            exception.Message);
    }
    [Fact]
    public void GetSliderTickDistance_Version8WithDoubleSliderVelocity_AppliesSliderVelocity()
    {
        // Arrange
        Beatmap beatmap = new Beatmap
        {
            BeatmapVersion = 8,
            SliderMultiplier = 1.4,
            SliderTickRate = 1
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1000,
            BeatLength = -50,
            IsUninherited = false
        });

        HitObject slider = new HitObject
        {
            Time = 1500
        };

        // Act
        double result = beatmap.GetSliderTickDistance(slider);

        // Assert
        Assert.Equal(280, result);
    }
    [Fact]
    public void GetSliderBodyHitSoundLayers_IgnoresSliderHitSampleIndexVolumeAndFilename()
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
                AdditionSet = 3,
                Index = 9,
                Volume = 20,
                Filename = "custom.wav"
            }
        };

        // Act
        List<HitSoundLayer> layers =
            beatmap.GetSliderBodyHitSoundLayers(slider);

        // Assert
        Assert.Equal(2, layers.Count);

        Assert.Equal(HitSoundType.SliderSlide, layers[0].Type);
        Assert.Equal(SampleSetType.Soft, layers[0].SampleSet);
        Assert.Equal(3, layers[0].SampleIndex);
        Assert.Equal(80, layers[0].Volume);

        Assert.Equal(HitSoundType.SliderWhistle, layers[1].Type);
        Assert.Equal(SampleSetType.Drum, layers[1].SampleSet);
        Assert.Equal(3, layers[1].SampleIndex);
        Assert.Equal(80, layers[1].Volume);

        Assert.Equal(9, slider.HitSample.Index);
        Assert.Equal(20, slider.HitSample.Volume);
        Assert.Equal("custom.wav", slider.HitSample.Filename);
    }

    [Fact]
    public void GetSliderTickHitSoundLayers_IgnoresSliderHitSampleIndexVolumeAndFilename()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 0,
            SampleSet = 3,
            SampleIndex = 4,
            Volume = 65,
            IsUninherited = true
        });

        HitObject slider = new HitObject
        {
            Time = 1000,
            HitSample = new HitSample
            {
                NormalSet = 2,
                AdditionSet = 3,
                Index = 9,
                Volume = 20,
                Filename = "custom.wav"
            }
        };

        // Act
        List<HitSoundLayer> layers =
            beatmap.GetSliderTickHitSoundLayers(slider);

        // Assert
        HitSoundLayer layer = Assert.Single(layers);

        Assert.Equal(HitSoundType.SliderTick, layer.Type);
        Assert.Equal(SampleSetType.Soft, layer.SampleSet);
        Assert.Equal(4, layer.SampleIndex);
        Assert.Equal(65, layer.Volume);

        Assert.Equal(9, slider.HitSample.Index);
        Assert.Equal(20, slider.HitSample.Volume);
        Assert.Equal("custom.wav", slider.HitSample.Filename);
    }
    [Fact]
    public void GetHitSoundLayers_SliderEdgeBeforeFirstTimingPoint_UsesFirstTimingPointSamples()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 3000,
            SampleSet = 1,
            SampleIndex = 4,
            Volume = 70
        });

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 5000,
            SampleSet = 2,
            SampleIndex = 6,
            Volume = 60
        });

        SliderEdge sliderEdge = new SliderEdge
        {
            HitSound = 0,
            NormalSet = 1
        };

        HitSample hitSample = new HitSample
        {
            Index = 9,
            Volume = 20,
            Filename = "custom.wav"
        };

        // Act
        List<HitSoundLayer> layers =
            beatmap.GetHitSoundLayers(sliderEdge, hitSample, 1000);

        // Assert
        HitSoundLayer layer = Assert.Single(layers);

        Assert.Equal(HitSoundType.Normal, layer.Type);
        Assert.Equal(4, layer.SampleIndex);
        Assert.Equal(70, layer.Volume);
    }

    [Fact]
    public void GetSliderBodyHitSoundLayers_BeforeFirstTimingPoint_UsesFirstTimingPointSamples()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 3000,
            SampleSet = 1,
            SampleIndex = 4,
            Volume = 70
        });

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 5000,
            SampleSet = 2,
            SampleIndex = 6,
            Volume = 60
        });

        HitObject slider = new HitObject
        {
            Time = 1000,
            HitSound = 0,
            HitSample = new HitSample
            {
                NormalSet = 1,
                Index = 9,
                Volume = 20,
                Filename = "custom.wav"
            }
        };

        // Act
        List<HitSoundLayer> layers =
            beatmap.GetSliderBodyHitSoundLayers(slider);

        // Assert
        HitSoundLayer layer = Assert.Single(layers);

        Assert.Equal(HitSoundType.SliderSlide, layer.Type);
        Assert.Equal(4, layer.SampleIndex);
        Assert.Equal(70, layer.Volume);
    }

    [Fact]
    public void GetSliderTickHitSoundLayers_BeforeFirstTimingPoint_UsesFirstTimingPointSamples()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 3000,
            SampleSet = 1,
            SampleIndex = 4,
            Volume = 70
        });

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 5000,
            SampleSet = 2,
            SampleIndex = 6,
            Volume = 60
        });

        HitObject slider = new HitObject
        {
            Time = 1000,
            HitSample = new HitSample
            {
                NormalSet = 1,
                Index = 9,
                Volume = 20,
                Filename = "custom.wav"
            }
        };

        // Act
        List<HitSoundLayer> layers =
            beatmap.GetSliderTickHitSoundLayers(slider);

        // Assert
        HitSoundLayer layer = Assert.Single(layers);

        Assert.Equal(HitSoundType.SliderTick, layer.Type);
        Assert.Equal(4, layer.SampleIndex);
        Assert.Equal(70, layer.Volume);
    }
    [Fact]
    public void GetHitSoundLayers_SliderEdgeWithoutTimingPoints_UsesDefaultSampleValues()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        SliderEdge sliderEdge = new SliderEdge
        {
            HitSound = 0,
            NormalSet = 1
        };

        HitSample hitSample = new HitSample
        {
            Index = 9,
            Volume = 20,
            Filename = "custom.wav"
        };

        // Act
        List<HitSoundLayer> layers =
            beatmap.GetHitSoundLayers(sliderEdge, hitSample, 1000);

        // Assert
        HitSoundLayer layer = Assert.Single(layers);

        Assert.Equal(0, layer.SampleIndex);
        Assert.Equal(100, layer.Volume);
    }

    [Fact]
    public void GetSliderBodyHitSoundLayers_WithoutTimingPoints_UsesDefaultSampleValues()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        HitObject slider = new HitObject
        {
            Time = 1000,
            HitSample = new HitSample
            {
                NormalSet = 1,
                Index = 9,
                Volume = 20,
                Filename = "custom.wav"
            }
        };

        // Act
        List<HitSoundLayer> layers =
            beatmap.GetSliderBodyHitSoundLayers(slider);

        // Assert
        HitSoundLayer layer = Assert.Single(layers);

        Assert.Equal(0, layer.SampleIndex);
        Assert.Equal(100, layer.Volume);
    }

    [Fact]
    public void GetSliderTickHitSoundLayers_WithoutTimingPoints_UsesDefaultSampleValues()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        HitObject slider = new HitObject
        {
            Time = 1000,
            HitSample = new HitSample
            {
                NormalSet = 1,
                Index = 9,
                Volume = 20,
                Filename = "custom.wav"
            }
        };

        // Act
        List<HitSoundLayer> layers =
            beatmap.GetSliderTickHitSoundLayers(slider);

        // Assert
        HitSoundLayer layer = Assert.Single(layers);

        Assert.Equal(0, layer.SampleIndex);
        Assert.Equal(100, layer.Volume);
    }
    [Fact]
    public void GetEffectiveNormalSet_SliderEdgeBeforeFirstTimingPoint_UsesFirstTimingPointSet()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 3000,
            SampleSet = 2
        });

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 5000,
            SampleSet = 3
        });

        SliderEdge sliderEdge = new SliderEdge
        {
            NormalSet = 0
        };

        // Act
        int result = beatmap.GetEffectiveNormalSet(sliderEdge, 1000);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void GetEffectiveAdditionSet_SliderEdgeBeforeFirstTimingPoint_UsesFirstTimingPointNormalSet()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 3000,
            SampleSet = 2
        });

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 5000,
            SampleSet = 3
        });

        SliderEdge sliderEdge = new SliderEdge
        {
            NormalSet = 0,
            AdditionSet = 0
        };

        // Act
        int result = beatmap.GetEffectiveAdditionSet(sliderEdge, 1000);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void GetHitSoundLayers_SliderEdgeZeroSetsBeforeFirstTimingPoint_UsesFirstTimingPointSet()
    {
        // Arrange
        Beatmap beatmap = new Beatmap();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 3000,
            SampleSet = 2,
            SampleIndex = 4,
            Volume = 70
        });

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 5000,
            SampleSet = 3,
            SampleIndex = 6,
            Volume = 60
        });

        SliderEdge sliderEdge = new SliderEdge
        {
            HitSound = 8,
            NormalSet = 0,
            AdditionSet = 0
        };

        HitSample hitSample = new HitSample
        {
            NormalSet = 3,
            AdditionSet = 3,
            Index = 9,
            Volume = 20,
            Filename = "custom.wav"
        };

        // Act
        List<HitSoundLayer> layers =
            beatmap.GetHitSoundLayers(sliderEdge, hitSample, 1000);

        // Assert
        Assert.Equal(2, layers.Count);

        Assert.Equal(HitSoundType.Normal, layers[0].Type);
        Assert.Equal(SampleSetType.Soft, layers[0].SampleSet);
        Assert.Equal(4, layers[0].SampleIndex);
        Assert.Equal(70, layers[0].Volume);

        Assert.Equal(HitSoundType.Clap, layers[1].Type);
        Assert.Equal(SampleSetType.Soft, layers[1].SampleSet);
        Assert.Equal(4, layers[1].SampleIndex);
        Assert.Equal(70, layers[1].Volume);
    }

    [Fact]
    public void GetHitCirclesInTimeRange_WhenBeatmapIsEmpty_ReturnsEmptyList()
    {
        Beatmap beatmap = new Beatmap();

        List<HitObject> result = beatmap.GetHitCirclesInTimeRange(0, 1000);

        Assert.Empty(result);
    }

    [Fact]
    public void GetHitCirclesInTimeRange_IncludesEndAndExcludesStart()
    {
        Beatmap beatmap = new Beatmap();

        HitObject atStart = new() { ObjectType = 1, Time = 1000 };
        HitObject first = new() { ObjectType = 1, Time = 1200 };
        HitObject second = new() { ObjectType = 1, Time = 1400 };
        HitObject atEnd = new() { ObjectType = 1, Time = 1600 };
        HitObject afterEnd = new() { ObjectType = 1, Time = 1800 };

        beatmap.HitObjects.AddRange(
            new[] { atStart, first, second, atEnd, afterEnd });

        List<HitObject> result =
            beatmap.GetHitCirclesInTimeRange(1000, 1600);

        Assert.Equal(3, result.Count);
        Assert.Same(first, result[0]);
        Assert.Same(second, result[1]);
        Assert.Same(atEnd, result[2]);
    }

    [Fact]
    public void GetHitCirclesInTimeRange_WhenStartEqualsEnd_ReturnsEmptyList()
    {
        Beatmap beatmap = new Beatmap();

        beatmap.HitObjects.Add(
            new HitObject { ObjectType = 1, Time = 1000 });

        List<HitObject> result =
            beatmap.GetHitCirclesInTimeRange(1000, 1000);

        Assert.Empty(result);
    }

    [Fact]
    public void GetHitCirclesInTimeRange_SkipsNonCircleObjects()
    {
        Beatmap beatmap = new Beatmap();

        HitObject slider = new() { ObjectType = 2, Time = 1100 };
        HitObject spinner = new() { ObjectType = 8, Time = 1200 };
        HitObject unknown = new() { ObjectType = 0, Time = 1300 };
        HitObject circle = new() { ObjectType = 1, Time = 1400 };
        HitObject newComboCircle = new() { ObjectType = 5, Time = 1500 };

        beatmap.HitObjects.AddRange(
            new[] { slider, spinner, unknown, circle, newComboCircle });

        List<HitObject> result =
            beatmap.GetHitCirclesInTimeRange(1000, 1600);

        Assert.Equal(2, result.Count);
        Assert.Same(circle, result[0]);
        Assert.Same(newComboCircle, result[1]);
    }

    [Fact]
    public void GetHitCirclesInTimeRange_PreservesBeatmapOrder()
    {
        Beatmap beatmap = new Beatmap();

        HitObject first = new() { ObjectType = 1, Time = 1500 };
        HitObject second = new() { ObjectType = 1, Time = 1000 };
        HitObject third = new() { ObjectType = 1, Time = 1200 };

        beatmap.HitObjects.AddRange(new[] { first, second, third });

        List<HitObject> result =
            beatmap.GetHitCirclesInTimeRange(0, 2000);

        Assert.Equal(3, result.Count);
        Assert.Same(first, result[0]);
        Assert.Same(second, result[1]);
        Assert.Same(third, result[2]);
    }

    [Fact]
    public void GetHitCirclesInTimeRange_SupportsFractionalRangeBounds()
    {
        Beatmap beatmap = new Beatmap();

        HitObject circle = new() { ObjectType = 1, Time = 1200 };
        beatmap.HitObjects.Add(circle);

        List<HitObject> result =
            beatmap.GetHitCirclesInTimeRange(1199.5, 1200.25);

        Assert.Same(circle, Assert.Single(result));
    }

    [Fact]
    public void GetHitCirclesInTimeRange_WhenStartIsNegative_IncludesZeroTime()
    {
        Beatmap beatmap = new Beatmap();

        HitObject circle = new() { ObjectType = 1, Time = 0 };
        beatmap.HitObjects.Add(circle);

        List<HitObject> result =
            beatmap.GetHitCirclesInTimeRange(-1, 0);

        Assert.Same(circle, Assert.Single(result));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void GetHitCirclesInTimeRange_WhenStartIsNotFinite_ThrowsArgumentOutOfRangeException(
        double invalidStart)
    {
        Beatmap beatmap = new Beatmap();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                beatmap.GetHitCirclesInTimeRange(invalidStart, 1000));

        Assert.Equal(
            "startExclusiveMilliseconds",
            exception.ParamName);

        Assert.Contains(
            "The start time must be finite.",
            exception.Message);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void GetHitCirclesInTimeRange_WhenEndIsNotFinite_ThrowsArgumentOutOfRangeException(
        double invalidEnd)
    {
        Beatmap beatmap = new Beatmap();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                beatmap.GetHitCirclesInTimeRange(0, invalidEnd));

        Assert.Equal(
            "endInclusiveMilliseconds",
            exception.ParamName);

        Assert.Contains(
            "The end time must be finite.",
            exception.Message);
    }

    [Fact]
    public void GetHitCirclesInTimeRange_WhenEndPrecedesStart_ThrowsArgumentOutOfRangeException()
    {
        Beatmap beatmap = new Beatmap();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                beatmap.GetHitCirclesInTimeRange(2000, 1000));

        Assert.Equal(
            "endInclusiveMilliseconds",
            exception.ParamName);

        Assert.Contains(
            "The end time must be greater than or equal to the start time.",
            exception.Message);
    }

    [Fact]
    public void GetHitCirclesInTimeRange_WhenCirclesShareTime_ReturnsAll()
    {
        Beatmap beatmap = new Beatmap();

        HitObject first = new() { ObjectType = 1, Time = 1000 };
        HitObject second = new() { ObjectType = 1, Time = 1000 };

        beatmap.HitObjects.AddRange(new[] { first, second });

        List<HitObject> result =
            beatmap.GetHitCirclesInTimeRange(999, 1000);

        Assert.Equal(2, result.Count);
        Assert.Same(first, result[0]);
        Assert.Same(second, result[1]);
    }

    [Fact]
    public void GetSliderTickTimesInRange_WhenRangeCoversAllTicks_ReturnsAllInOrder()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        List<double> result =
            beatmap.GetSliderTickTimesInRange(slider, 999, 2000);

        Assert.Equal(
            new double[] { 1125, 1250, 1375, 1625, 1750, 1875 },
            result);
    }

    [Fact]
    public void GetSliderTickTimesInRange_WhenRangeContainsSeveralTicks_ReturnsOnlyMatching()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        List<double> result =
            beatmap.GetSliderTickTimesInRange(slider, 1200, 1700);

        Assert.Equal(new double[] { 1250, 1375, 1625 }, result);
    }

    [Theory]
    [InlineData(1125, 1250, 1250)]
    [InlineData(1375, 1625, 1625)]
    [InlineData(1750, 1875, 1875)]
    public void GetSliderTickTimesInRange_ExcludesStartAndIncludesEnd(
        double start,
        double end,
        double expected)
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        List<double> result =
            beatmap.GetSliderTickTimesInRange(slider, start, end);

        Assert.Equal(expected, Assert.Single(result));
    }

    [Fact]
    public void GetSliderTickTimesInRange_ConsecutiveIntervals_DoNotRepeatTicks()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        List<double> first =
            beatmap.GetSliderTickTimesInRange(slider, 1000, 1250);

        List<double> second =
            beatmap.GetSliderTickTimesInRange(slider, 1250, 1750);

        List<double> third =
            beatmap.GetSliderTickTimesInRange(slider, 1750, 2000);

        Assert.Equal(new double[] { 1125, 1250 }, first);
        Assert.Equal(new double[] { 1375, 1625, 1750 }, second);
        Assert.Equal(new double[] { 1875 }, third);
    }

    [Fact]
    public void GetSliderTickTimesInRange_WhenRangePrecedesTicks_ReturnsEmpty()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        List<double> result =
            beatmap.GetSliderTickTimesInRange(slider, 0, 1100);

        Assert.Empty(result);
    }

    [Fact]
    public void GetSliderTickTimesInRange_WhenRangeFollowsTicks_ReturnsEmpty()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        List<double> result =
            beatmap.GetSliderTickTimesInRange(slider, 1875, 2200);

        Assert.Empty(result);
    }

    [Fact]
    public void GetSliderTickTimesInRange_WhenBoundsAreEqual_ReturnsEmpty()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        List<double> result =
            beatmap.GetSliderTickTimesInRange(slider, 1250, 1250);

        Assert.Empty(result);
    }

    [Fact]
    public void GetSliderTickTimesInRange_WhenBoundsAreFractional_FiltersCorrectly()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        List<double> result =
            beatmap.GetSliderTickTimesInRange(slider, 1124.5, 1125.25);

        Assert.Equal(1125.0, Assert.Single(result));
    }

    [Fact]
    public void GetSliderTickTimesInRange_WhenSliderHasNoTicks_ReturnsEmpty()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        beatmap.SliderTickRate = 1;

        List<double> result =
            beatmap.GetSliderTickTimesInRange(slider, 0, 2500);

        Assert.Empty(result);
    }

    [Fact]
    public void GetSliderTickTimesInRange_WhenSliderIsNull_ThrowsArgumentNullException()
    {
        var beatmap = new Beatmap();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                beatmap.GetSliderTickTimesInRange(null!, 0, 1000));

        Assert.Equal("slider", exception.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void GetSliderTickTimesInRange_WhenObjectIsNotSlider_ThrowsArgumentException(
        int objectType)
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        slider.ObjectType = objectType;

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                beatmap.GetSliderTickTimesInRange(slider, 0, 2000));

        Assert.Equal("slider", exception.ParamName);

        Assert.Contains(
            "The hit object must be a slider.",
            exception.Message);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void GetSliderTickTimesInRange_WhenStartIsNotFinite_ThrowsArgumentOutOfRangeException(
        double invalidStart)
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                beatmap.GetSliderTickTimesInRange(
                    slider, invalidStart, 2000));

        Assert.Equal(
            "startExclusiveMilliseconds",
            exception.ParamName);

        Assert.Contains(
            "The start time must be finite.",
            exception.Message);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void GetSliderTickTimesInRange_WhenEndIsNotFinite_ThrowsArgumentOutOfRangeException(
        double invalidEnd)
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                beatmap.GetSliderTickTimesInRange(
                    slider, 1000, invalidEnd));

        Assert.Equal(
            "endInclusiveMilliseconds",
            exception.ParamName);

        Assert.Contains(
            "The end time must be finite.",
            exception.Message);
    }

    [Fact]
    public void GetSliderTickTimesInRange_WhenEndPrecedesStart_ThrowsArgumentOutOfRangeException()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                beatmap.GetSliderTickTimesInRange(
                    slider, 2000, 1000));

        Assert.Equal(
            "endInclusiveMilliseconds",
            exception.ParamName);

        Assert.Contains(
            "The end time must be greater than or equal to the start time.",
            exception.Message);
    }

    [Fact]
    public void GetSliderTickTimesInRange_WhenSliderHasNoSpans_PropagatesException()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        slider.Slides = 0;

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                beatmap.GetSliderTickTimesInRange(
                    slider, 0, 2000));

        Assert.Equal(
            "It is not possible to calculate slider tick times because the slider must have at least one span.",
            exception.Message);
    }

    // Test helper for slider tick range calculations
    private static (Beatmap Beatmap, HitObject Slider) CreateSliderForTickRangeTests()
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

        var slider = new HitObject
        {
            ObjectType = 2,
            Time = 1000,
            Length = 140,
            Slides = 2
        };

        return (beatmap, slider);
    }

    [Fact]
    public void GetSliderEdgeTimesInRange_WhenRangeCoversAllEdges_ReturnsAllInOrder()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        var result = beatmap.GetSliderEdgeTimesInRange(
            slider, 999, 2000);

        Assert.Equal(
            new[]
            {
                (EdgeIndex: 0, EdgeTime: 1000.0),
                (EdgeIndex: 1, EdgeTime: 1500.0),
                (EdgeIndex: 2, EdgeTime: 2000.0)
            },
            result);
    }

    [Fact]
    public void GetSliderEdgeTimesInRange_WhenRangeContainsRepeatAndTail_ReturnsBoth()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        var result = beatmap.GetSliderEdgeTimesInRange(
            slider, 1200, 2000);

        Assert.Equal(
            new[]
            {
                (EdgeIndex: 1, EdgeTime: 1500.0),
                (EdgeIndex: 2, EdgeTime: 2000.0)
            },
            result);
    }

    [Fact]
    public void GetSliderEdgeTimesInRange_ExcludesStartAndIncludesEnd()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        var result = beatmap.GetSliderEdgeTimesInRange(
            slider, 1000, 1500);

        var edge = Assert.Single(result);

        Assert.Equal(1, edge.EdgeIndex);
        Assert.Equal(1500.0, edge.EdgeTime);
    }

    [Fact]
    public void GetSliderEdgeTimesInRange_AdjacentIntervals_DoNotRepeatEdges()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        var first = beatmap.GetSliderEdgeTimesInRange(
            slider, 999, 1000);

        var second = beatmap.GetSliderEdgeTimesInRange(
            slider, 1000, 1500);

        var third = beatmap.GetSliderEdgeTimesInRange(
            slider, 1500, 2000);

        Assert.Equal(0, Assert.Single(first).EdgeIndex);
        Assert.Equal(1, Assert.Single(second).EdgeIndex);
        Assert.Equal(2, Assert.Single(third).EdgeIndex);
    }

    [Fact]
    public void GetSliderEdgeTimesInRange_WhenRangePrecedesSlider_ReturnsEmpty()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        var result = beatmap.GetSliderEdgeTimesInRange(
            slider, 0, 999);

        Assert.Empty(result);
    }

    [Fact]
    public void GetSliderEdgeTimesInRange_WhenRangeFollowsSlider_ReturnsEmpty()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        var result = beatmap.GetSliderEdgeTimesInRange(
            slider, 2000, 3000);

        Assert.Empty(result);
    }

    [Fact]
    public void GetSliderEdgeTimesInRange_WhenBoundsAreEqual_ReturnsEmpty()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        var result = beatmap.GetSliderEdgeTimesInRange(
            slider, 1500, 1500);

        Assert.Empty(result);
    }

    [Fact]
    public void GetSliderEdgeTimesInRange_WhenBoundsAreFractional_FiltersCorrectly()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        var result = beatmap.GetSliderEdgeTimesInRange(
            slider, 1499.5, 1500.25);

        var edge = Assert.Single(result);

        Assert.Equal(1, edge.EdgeIndex);
        Assert.Equal(1500.0, edge.EdgeTime);
    }

    [Fact]
    public void GetSliderEdgeTimesInRange_WhenSliderHasOneSpan_ReturnsHeadAndTail()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        slider.Slides = 1;

        var result = beatmap.GetSliderEdgeTimesInRange(
            slider, 999, 2000);

        Assert.Equal(
            new[]
            {
                (EdgeIndex: 0, EdgeTime: 1000.0),
                (EdgeIndex: 1, EdgeTime: 1500.0)
            },
            result);
    }

    [Fact]
    public void GetSliderEdgeTimesInRange_WhenSliderHasThreeSpans_IncludesFinalTail()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        slider.Slides = 3;

        var result = beatmap.GetSliderEdgeTimesInRange(
            slider, 1900, 2600);

        Assert.Equal(
            new[]
            {
                (EdgeIndex: 2, EdgeTime: 2000.0),
                (EdgeIndex: 3, EdgeTime: 2500.0)
            },
            result);
    }

    [Fact]
    public void GetSliderEdgeTimesInRange_WhenVelocityChanges_UsesEffectiveSpanDuration()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 500,
            BeatLength = -50,
            IsUninherited = false
        });

        var result = beatmap.GetSliderEdgeTimesInRange(
            slider, 999, 1600);

        // SV = 2, so each span lasts 250 ms.
        Assert.Equal(
            new[]
            {
                (EdgeIndex: 0, EdgeTime: 1000.0),
                (EdgeIndex: 1, EdgeTime: 1250.0),
                (EdgeIndex: 2, EdgeTime: 1500.0)
            },
            result);
    }

    [Fact]
    public void GetSliderEdgeTimesInRange_WhenSliderIsNull_ThrowsArgumentNullException()
    {
        var beatmap = new Beatmap();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                beatmap.GetSliderEdgeTimesInRange(
                    null!, 0, 2000));

        Assert.Equal("slider", exception.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void GetSliderEdgeTimesInRange_WhenObjectIsNotSlider_ThrowsArgumentException(
        int objectType)
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        slider.ObjectType = objectType;

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                beatmap.GetSliderEdgeTimesInRange(
                    slider, 0, 2000));

        Assert.Equal("slider", exception.ParamName);

        Assert.Contains(
            "The hit object must be a slider.",
            exception.Message);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void GetSliderEdgeTimesInRange_WhenStartIsNotFinite_ThrowsArgumentOutOfRangeException(
        double invalidStart)
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                beatmap.GetSliderEdgeTimesInRange(
                    slider, invalidStart, 2000));

        Assert.Equal(
            "startExclusiveMilliseconds",
            exception.ParamName);

        Assert.Contains(
            "The start time must be finite.",
            exception.Message);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void GetSliderEdgeTimesInRange_WhenEndIsNotFinite_ThrowsArgumentOutOfRangeException(
        double invalidEnd)
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                beatmap.GetSliderEdgeTimesInRange(
                    slider, 0, invalidEnd));

        Assert.Equal(
            "endInclusiveMilliseconds",
            exception.ParamName);

        Assert.Contains(
            "The end time must be finite.",
            exception.Message);
    }

    [Fact]
    public void GetSliderEdgeTimesInRange_WhenEndPrecedesStart_ThrowsArgumentOutOfRangeException()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                beatmap.GetSliderEdgeTimesInRange(
                    slider, 2000, 1000));

        Assert.Equal(
            "endInclusiveMilliseconds",
            exception.ParamName);

        Assert.Contains(
            "The end time must be greater than or equal to the start time.",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetSliderEdgeTimesInRange_WhenSlidesIsInvalid_ThrowsInvalidOperationException(
        int slides)
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        slider.Slides = slides;

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                beatmap.GetSliderEdgeTimesInRange(
                    slider, 0, 2000));

        Assert.Equal(
            "It is not possible to calculate a slider edge time because the slider must have at least one span.",
            exception.Message);
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_WhenBeatmapIsEmpty_ReturnsEmpty()
    {
        var beatmap = new Beatmap();

        List<TimedHitSoundEvent> result =
            beatmap.GetTimedHitSoundEventsInRange(0, 2000);

        Assert.Empty(result);
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_MixedObjects_ReturnsChronologicalEvents()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        HitObject laterCircle = new()
        {
            ObjectType = 1,
            Time = 1800
        };

        HitObject earlierCircle = new()
        {
            ObjectType = 1,
            Time = 1250
        };

        beatmap.HitObjects.AddRange(
            new[] { laterCircle, slider, earlierCircle });

        List<TimedHitSoundEvent> result =
            beatmap.GetTimedHitSoundEventsInRange(999, 2000);

        Assert.Equal(11, result.Count);

        Assert.Equal(
            new double[]
            {
                1000, 1125, 1250, 1250, 1375,
                1500, 1625, 1750, 1800, 1875, 2000
            },
            result.Select(item => item.Time));

        Assert.Equal(
            new[]
            {
                HitSoundEventType.SliderEdge,
                HitSoundEventType.SliderTick,
                HitSoundEventType.HitCircle,
                HitSoundEventType.SliderTick,
                HitSoundEventType.SliderTick,
                HitSoundEventType.SliderEdge,
                HitSoundEventType.SliderTick,
                HitSoundEventType.SliderTick,
                HitSoundEventType.HitCircle,
                HitSoundEventType.SliderTick,
                HitSoundEventType.SliderEdge
            },
            result.Select(item => item.EventType));

        Assert.Same(slider, result[0].HitObject);
        Assert.Same(earlierCircle, result[2].HitObject);
        Assert.Same(laterCircle, result[8].HitObject);
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_WhenCircleAndTickShareTime_PreservesStableOrder()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        HitObject firstCircle = new()
        {
            ObjectType = 1,
            Time = 1250
        };

        HitObject secondCircle = new()
        {
            ObjectType = 1,
            Time = 1250
        };

        beatmap.HitObjects.AddRange(
            new[] { firstCircle, slider, secondCircle });

        List<TimedHitSoundEvent> result =
            beatmap.GetTimedHitSoundEventsInRange(1249, 1250);

        Assert.Equal(3, result.Count);

        Assert.Same(firstCircle, result[0].HitObject);
        Assert.Same(secondCircle, result[1].HitObject);
        Assert.Same(slider, result[2].HitObject);

        Assert.Equal(HitSoundEventType.HitCircle, result[0].EventType);
        Assert.Equal(HitSoundEventType.HitCircle, result[1].EventType);
        Assert.Equal(HitSoundEventType.SliderTick, result[2].EventType);

        Assert.All(result, item => Assert.Equal(1250.0, item.Time));
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_WhenSliderHeadsShareTime_PreservesSliderOrder()
    {
        var (beatmap, firstSlider) = CreateSliderForTickRangeTests();

        HitObject secondSlider = new()
        {
            ObjectType = 2,
            Time = 1000,
            Length = 140,
            Slides = 2
        };

        beatmap.HitObjects.AddRange(
            new[] { firstSlider, secondSlider });

        List<TimedHitSoundEvent> result =
            beatmap.GetTimedHitSoundEventsInRange(999, 1000);

        Assert.Equal(2, result.Count);

        Assert.Same(firstSlider, result[0].HitObject);
        Assert.Same(secondSlider, result[1].HitObject);

        Assert.All(result, item =>
        {
            Assert.Equal(HitSoundEventType.SliderEdge, item.EventType);
            Assert.Equal(0, item.EdgeIndex);
            Assert.Equal(1000.0, item.Time);
        });
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_SliderEvents_PreserveEdgeIndices()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();
        beatmap.HitObjects.Add(slider);

        List<TimedHitSoundEvent> result =
            beatmap.GetTimedHitSoundEventsInRange(999, 2000);

        List<TimedHitSoundEvent> edges = result
            .Where(item => item.EventType == HitSoundEventType.SliderEdge)
            .ToList();

        List<TimedHitSoundEvent> ticks = result
            .Where(item => item.EventType == HitSoundEventType.SliderTick)
            .ToList();

        Assert.Equal(3, edges.Count);
        Assert.Equal(6, ticks.Count);

        Assert.Equal(
            new int?[] { 0, 1, 2 },
            edges.Select(item => item.EdgeIndex));

        Assert.Equal(
            new double[] { 1000, 1500, 2000 },
            edges.Select(item => item.Time));

        Assert.All(ticks, item =>
        {
            Assert.Null(item.EdgeIndex);
            Assert.Same(slider, item.HitObject);
        });
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_WhenOnlyCircleExists_ReturnsCircleEvent()
    {
        var beatmap = new Beatmap();

        HitObject circle = new()
        {
            ObjectType = 1,
            Time = 500
        };

        beatmap.HitObjects.Add(circle);

        TimedHitSoundEvent result = Assert.Single(
            beatmap.GetTimedHitSoundEventsInRange(499, 500));

        Assert.Equal(500.0, result.Time);
        Assert.Same(circle, result.HitObject);
        Assert.Equal(HitSoundEventType.HitCircle, result.EventType);
        Assert.Null(result.EdgeIndex);
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_IgnoresNonDiscreteHitObjects()
    {
        var beatmap = new Beatmap();

        beatmap.HitObjects.Add(new HitObject
        {
            ObjectType = 8,
            Time = 500
        });

        beatmap.HitObjects.Add(new HitObject
        {
            ObjectType = 0,
            Time = 1000
        });

        List<TimedHitSoundEvent> result =
            beatmap.GetTimedHitSoundEventsInRange(0, 2000);

        Assert.Empty(result);
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_WhenNoEventsMatch_ReturnsEmpty()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();
        beatmap.HitObjects.Add(slider);

        List<TimedHitSoundEvent> result =
            beatmap.GetTimedHitSoundEventsInRange(0, 900);

        Assert.Empty(result);
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_ExcludesStartAndIncludesEnd()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();
        beatmap.HitObjects.Add(slider);

        List<TimedHitSoundEvent> result =
            beatmap.GetTimedHitSoundEventsInRange(1000, 1500);

        Assert.Equal(
            new double[] { 1125, 1250, 1375, 1500 },
            result.Select(item => item.Time));

        TimedHitSoundEvent last = result[^1];

        Assert.Equal(HitSoundEventType.SliderEdge, last.EventType);
        Assert.Equal(1, last.EdgeIndex);
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_AdjacentIntervals_DoNotRepeatEvents()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();
        beatmap.HitObjects.Add(slider);

        var first = beatmap.GetTimedHitSoundEventsInRange(
            999, 1500);

        var second = beatmap.GetTimedHitSoundEventsInRange(
            1500, 2000);

        Assert.Equal(
            new double[] { 1000, 1125, 1250, 1375, 1500 },
            first.Select(item => item.Time));

        Assert.Equal(
            new double[] { 1625, 1750, 1875, 2000 },
            second.Select(item => item.Time));

        Assert.Equal(9, first.Count + second.Count);
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_WhenBoundsAreEqual_ReturnsEmpty()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();
        beatmap.HitObjects.Add(slider);

        List<TimedHitSoundEvent> result =
            beatmap.GetTimedHitSoundEventsInRange(1500, 1500);

        Assert.Empty(result);
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_FractionalBounds_SelectCorrectTick()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();
        beatmap.HitObjects.Add(slider);

        TimedHitSoundEvent result = Assert.Single(
            beatmap.GetTimedHitSoundEventsInRange(
                1124.5, 1125.25));

        Assert.Equal(1125.0, result.Time);
        Assert.Equal(HitSoundEventType.SliderTick, result.EventType);
        Assert.Same(slider, result.HitObject);
        Assert.Null(result.EdgeIndex);
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_WhenSliderHasNoTicks_ReturnsEdgesOnly()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        beatmap.SliderTickRate = 1;
        beatmap.HitObjects.Add(slider);

        List<TimedHitSoundEvent> result =
            beatmap.GetTimedHitSoundEventsInRange(999, 2000);

        Assert.Equal(3, result.Count);

        Assert.All(result, item =>
            Assert.Equal(
                HitSoundEventType.SliderEdge,
                item.EventType));

        Assert.Equal(
            new double[] { 1000, 1500, 2000 },
            result.Select(item => item.Time));
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_WhenCircleIsAtZero_IncludesIt()
    {
        var beatmap = new Beatmap();

        HitObject circle = new()
        {
            ObjectType = 1,
            Time = 0
        };

        beatmap.HitObjects.Add(circle);

        TimedHitSoundEvent result = Assert.Single(
            beatmap.GetTimedHitSoundEventsInRange(-1, 0));

        Assert.Same(circle, result.HitObject);
        Assert.Equal(0.0, result.Time);
        Assert.Equal(HitSoundEventType.HitCircle, result.EventType);
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_ComboFlags_PreserveEventTypes()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        slider.ObjectType = 2 | 4;

        HitObject circle = new()
        {
            ObjectType = 1 | 4,
            Time = 1200
        };

        beatmap.HitObjects.AddRange(new[] { slider, circle });

        List<TimedHitSoundEvent> result =
            beatmap.GetTimedHitSoundEventsInRange(999, 1200);

        Assert.Equal(3, result.Count);

        Assert.Equal(HitSoundEventType.SliderEdge, result[0].EventType);
        Assert.Equal(HitSoundEventType.SliderTick, result[1].EventType);
        Assert.Equal(HitSoundEventType.HitCircle, result[2].EventType);

        Assert.Same(circle, result[2].HitObject);
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_WithoutSliderTimingPoint_PropagatesException()
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        beatmap.TimingPoints.Clear();
        beatmap.HitObjects.Add(slider);

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                beatmap.GetTimedHitSoundEventsInRange(0, 2000));

        Assert.Equal(
            "It is not possible to calculate the slider span duration without an active uninherited timing point.",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetTimedHitSoundEventsInRange_WhenSliderHasInvalidSlides_PropagatesException(
        int slides)
    {
        var (beatmap, slider) = CreateSliderForTickRangeTests();

        slider.Slides = slides;
        beatmap.HitObjects.Add(slider);

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                beatmap.GetTimedHitSoundEventsInRange(0, 2000));

        Assert.Equal(
            "It is not possible to calculate a slider edge time because the slider must have at least one span.",
            exception.Message);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void GetTimedHitSoundEventsInRange_WhenStartIsNotFinite_ThrowsArgumentOutOfRangeException(
        double invalidStart)
    {
        var beatmap = new Beatmap();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                beatmap.GetTimedHitSoundEventsInRange(
                    invalidStart, 2000));

        Assert.Equal(
            "startExclusiveMilliseconds",
            exception.ParamName);

        Assert.Equal(invalidStart, exception.ActualValue);

        Assert.Contains(
            "The start time must be finite.",
            exception.Message);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void GetTimedHitSoundEventsInRange_WhenEndIsNotFinite_ThrowsArgumentOutOfRangeException(
        double invalidEnd)
    {
        var beatmap = new Beatmap();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                beatmap.GetTimedHitSoundEventsInRange(
                    0, invalidEnd));

        Assert.Equal(
            "endInclusiveMilliseconds",
            exception.ParamName);

        Assert.Equal(invalidEnd, exception.ActualValue);

        Assert.Contains(
            "The end time must be finite.",
            exception.Message);
    }

    [Fact]
    public void GetTimedHitSoundEventsInRange_WhenEndPrecedesStart_ThrowsArgumentOutOfRangeException()
    {
        var beatmap = new Beatmap();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                beatmap.GetTimedHitSoundEventsInRange(
                    2000, 1000));

        Assert.Equal(
            "endInclusiveMilliseconds",
            exception.ParamName);

        Assert.Equal(1000.0, exception.ActualValue);

        Assert.Contains(
            "The end time must be greater than or equal to the start time.",
            exception.Message);
    }

}