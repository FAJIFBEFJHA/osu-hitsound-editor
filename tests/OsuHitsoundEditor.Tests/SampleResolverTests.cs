using OsuHitsoundEditor;

namespace OsuHitsoundEditor.Tests;

public class SampleResolverTests
{
    [Theory]
    [InlineData(
        SampleSetType.Normal,
        HitSoundType.Normal,
        0,
        null,
        "normal-hitnormal.wav")]
    [InlineData(
        SampleSetType.Normal,
        HitSoundType.Normal,
        1,
        "normal-hitnormal.wav",
        "normal-hitnormal.wav")]
    [InlineData(
        SampleSetType.Normal,
        HitSoundType.Normal,
        2,
        "normal-hitnormal2.wav",
        "normal-hitnormal.wav")]
    [InlineData(
        SampleSetType.Soft,
        HitSoundType.Whistle,
        3,
        "soft-hitwhistle3.wav",
        "soft-hitwhistle.wav")]
    [InlineData(
        SampleSetType.Drum,
        HitSoundType.Finish,
        4,
        "drum-hitfinish4.wav",
        "drum-hitfinish.wav")]
    [InlineData(
        SampleSetType.Normal,
        HitSoundType.Clap,
        10,
        "normal-hitclap10.wav",
        "normal-hitclap.wav")]
    public void GetStandardSampleLookup_ReturnsExpectedFilenames(
        SampleSetType sampleSet,
        HitSoundType type,
        int sampleIndex,
        string? expectedBeatmapFilename,
        string expectedFallbackFilename)
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            SampleSet = sampleSet,
            Type = type,
            SampleIndex = sampleIndex
        };

        var result = resolver.GetStandardSampleLookup(layer);

        Assert.Equal(expectedBeatmapFilename, result.BeatmapFilename);
        Assert.Equal(expectedFallbackFilename, result.FallbackFilename);
    }

    [Fact]
    public void GetStandardSampleLookup_ThrowsWhenSampleIndexIsNegative()
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            SampleSet = SampleSetType.Normal,
            Type = HitSoundType.Normal,
            SampleIndex = -1
        };

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => resolver.GetStandardSampleLookup(layer));

        Assert.Equal("SampleIndex", exception.ParamName);
        Assert.Equal(-1, exception.ActualValue);
    }

    [Fact]
    public void GetStandardSampleLookup_ThrowsWhenSampleSetIsDefault()
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            SampleSet = SampleSetType.Default,
            Type = HitSoundType.Normal,
            SampleIndex = 1
        };

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => resolver.GetStandardSampleLookup(layer));

        Assert.Equal(
            "A standard sample lookup requires a resolved sample set.",
            exception.Message);
    }

    [Fact]
    public void GetStandardSampleLookup_ThrowsWhenSampleSetIsUnsupported()
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            SampleSet = (SampleSetType)99,
            Type = HitSoundType.Normal,
            SampleIndex = 1
        };

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => resolver.GetStandardSampleLookup(layer));

        Assert.Equal("SampleSet", exception.ParamName);
        Assert.Equal((SampleSetType)99, exception.ActualValue);
    }

    [Theory]
    [InlineData(HitSoundType.Custom)]
    [InlineData(HitSoundType.SliderSlide)]
    [InlineData(HitSoundType.SliderWhistle)]
    [InlineData(HitSoundType.SliderTick)]
    public void GetStandardSampleLookup_ThrowsWhenHitSoundTypeIsUnsupported(
        HitSoundType type)
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            SampleSet = SampleSetType.Normal,
            Type = type,
            SampleIndex = 1
        };

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => resolver.GetStandardSampleLookup(layer));

        Assert.Equal("Type", exception.ParamName);
        Assert.Equal(type, exception.ActualValue);
    }

    [Fact]
    public void GetStandardSampleLookup_ThrowsWhenLayerIsNull()
    {
        SampleResolver resolver = new SampleResolver();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(
                () => resolver.GetStandardSampleLookup(null!));

        Assert.Equal("layer", exception.ParamName);
    }
    [Theory]
    [InlineData(
    SampleSetType.Normal,
    HitSoundType.SliderSlide,
    0,
    null,
    "normal-sliderslide.wav")]
    [InlineData(
    SampleSetType.Normal,
    HitSoundType.SliderSlide,
    1,
    "normal-sliderslide.wav",
    "normal-sliderslide.wav")]
    [InlineData(
    SampleSetType.Normal,
    HitSoundType.SliderSlide,
    2,
    "normal-sliderslide2.wav",
    "normal-sliderslide.wav")]
    [InlineData(
    SampleSetType.Soft,
    HitSoundType.SliderWhistle,
    3,
    "soft-sliderwhistle3.wav",
    "soft-sliderwhistle.wav")]
    [InlineData(
    SampleSetType.Drum,
    HitSoundType.SliderSlide,
    4,
    "drum-sliderslide4.wav",
    "drum-sliderslide.wav")]
    public void GetSliderBodySampleLookup_ReturnsExpectedFilenames(
    SampleSetType sampleSet,
    HitSoundType type,
    int sampleIndex,
    string? expectedBeatmapFilename,
    string expectedFallbackFilename)
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            SampleSet = sampleSet,
            Type = type,
            SampleIndex = sampleIndex
        };

        var result = resolver.GetSliderBodySampleLookup(layer);

        Assert.Equal(expectedBeatmapFilename, result.BeatmapFilename);
        Assert.Equal(expectedFallbackFilename, result.FallbackFilename);
    }

    [Fact]
    public void GetSliderBodySampleLookup_ThrowsWhenSampleIndexIsNegative()
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            SampleSet = SampleSetType.Normal,
            Type = HitSoundType.SliderSlide,
            SampleIndex = -1
        };

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => resolver.GetSliderBodySampleLookup(layer));

        Assert.Equal("SampleIndex", exception.ParamName);
        Assert.Equal(-1, exception.ActualValue);
    }

    [Fact]
    public void GetSliderBodySampleLookup_ThrowsWhenSampleSetIsDefault()
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            SampleSet = SampleSetType.Default,
            Type = HitSoundType.SliderSlide,
            SampleIndex = 1
        };

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => resolver.GetSliderBodySampleLookup(layer));

        Assert.Equal(
            "A slider body sample lookup requires a resolved sample set.",
            exception.Message);
    }

    [Fact]
    public void GetSliderBodySampleLookup_ThrowsWhenSampleSetIsUnsupported()
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            SampleSet = (SampleSetType)99,
            Type = HitSoundType.SliderSlide,
            SampleIndex = 1
        };

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => resolver.GetSliderBodySampleLookup(layer));

        Assert.Equal("SampleSet", exception.ParamName);
        Assert.Equal((SampleSetType)99, exception.ActualValue);
    }

    [Theory]
    [InlineData(HitSoundType.Normal)]
    [InlineData(HitSoundType.Custom)]
    [InlineData(HitSoundType.SliderTick)]
    public void GetSliderBodySampleLookup_ThrowsWhenHitSoundTypeIsUnsupported(
        HitSoundType type)
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            SampleSet = SampleSetType.Normal,
            Type = type,
            SampleIndex = 1
        };

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => resolver.GetSliderBodySampleLookup(layer));

        Assert.Equal("Type", exception.ParamName);
        Assert.Equal(type, exception.ActualValue);
    }

    [Fact]
    public void GetSliderBodySampleLookup_ThrowsWhenLayerIsNull()
    {
        SampleResolver resolver = new SampleResolver();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(
                () => resolver.GetSliderBodySampleLookup(null!));

        Assert.Equal("layer", exception.ParamName);
    }
    [Theory]
    [InlineData(
    SampleSetType.Normal,
    0,
    null,
    "normal-slidertick.wav")]
    [InlineData(
    SampleSetType.Normal,
    1,
    "normal-slidertick.wav",
    "normal-slidertick.wav")]
    [InlineData(
    SampleSetType.Normal,
    2,
    "normal-slidertick2.wav",
    "normal-slidertick.wav")]
    [InlineData(
    SampleSetType.Soft,
    3,
    "soft-slidertick3.wav",
    "soft-slidertick.wav")]
    [InlineData(
    SampleSetType.Drum,
    4,
    "drum-slidertick4.wav",
    "drum-slidertick.wav")]
    public void GetSliderTickSampleLookup_ReturnsExpectedFilenames(
    SampleSetType sampleSet,
    int sampleIndex,
    string? expectedBeatmapFilename,
    string expectedFallbackFilename)
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            SampleSet = sampleSet,
            Type = HitSoundType.SliderTick,
            SampleIndex = sampleIndex
        };

        var result = resolver.GetSliderTickSampleLookup(layer);

        Assert.Equal(expectedBeatmapFilename, result.BeatmapFilename);
        Assert.Equal(expectedFallbackFilename, result.FallbackFilename);
    }

    [Fact]
    public void GetSliderTickSampleLookup_ThrowsWhenSampleIndexIsNegative()
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            SampleSet = SampleSetType.Normal,
            Type = HitSoundType.SliderTick,
            SampleIndex = -1
        };

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => resolver.GetSliderTickSampleLookup(layer));

        Assert.Equal("SampleIndex", exception.ParamName);
        Assert.Equal(-1, exception.ActualValue);
    }

    [Fact]
    public void GetSliderTickSampleLookup_ThrowsWhenSampleSetIsDefault()
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            SampleSet = SampleSetType.Default,
            Type = HitSoundType.SliderTick,
            SampleIndex = 1
        };

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => resolver.GetSliderTickSampleLookup(layer));

        Assert.Equal(
            "A slider tick sample lookup requires a resolved sample set.",
            exception.Message);
    }

    [Fact]
    public void GetSliderTickSampleLookup_ThrowsWhenSampleSetIsUnsupported()
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            SampleSet = (SampleSetType)99,
            Type = HitSoundType.SliderTick,
            SampleIndex = 1
        };

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => resolver.GetSliderTickSampleLookup(layer));

        Assert.Equal("SampleSet", exception.ParamName);
        Assert.Equal((SampleSetType)99, exception.ActualValue);
    }
    [Theory]
    [InlineData(HitSoundType.Normal)]
    [InlineData(HitSoundType.Custom)]
    [InlineData(HitSoundType.SliderSlide)]
    public void GetSliderTickSampleLookup_ThrowsWhenHitSoundTypeIsUnsupported(
        HitSoundType type)
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            SampleSet = SampleSetType.Normal,
            Type = type,
            SampleIndex = 1
        };

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => resolver.GetSliderTickSampleLookup(layer));

        Assert.Equal("Type", exception.ParamName);
        Assert.Equal(type, exception.ActualValue);
    }
    [Fact]
    public void GetSliderTickSampleLookup_ThrowsWhenLayerIsNull()
    {
        SampleResolver resolver = new SampleResolver();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(
                () => resolver.GetSliderTickSampleLookup(null!));

        Assert.Equal("layer", exception.ParamName);
    }
    [Fact]
    public void ResolveCustomSamplePath_ReturnsFullPathWhenFileExists()
    {
        SampleResolver resolver = new SampleResolver();

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        try
        {
            string filename = "custom-hit.wav";
            string expectedPath = Path.Combine(tempDirectory, filename);

            File.WriteAllBytes(expectedPath, Array.Empty<byte>());

            HitSoundLayer layer = new HitSoundLayer
            {
                Type = HitSoundType.Custom,
                Filename = filename
            };

            string? result = resolver.ResolveCustomSamplePath(
                layer,
                tempDirectory);

            Assert.Equal(expectedPath, result);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void ResolveCustomSamplePath_ReturnsNullWhenFileDoesNotExist()
    {
        SampleResolver resolver = new SampleResolver();

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        try
        {
            HitSoundLayer layer = new HitSoundLayer
            {
                Type = HitSoundType.Custom,
                Filename = "missing.wav"
            };

            string? result = resolver.ResolveCustomSamplePath(
                layer,
                tempDirectory);

            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void ResolveCustomSamplePath_ThrowsWhenLayerIsNull()
    {
        SampleResolver resolver = new SampleResolver();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(
                () => resolver.ResolveCustomSamplePath(
                    null!,
                    "C:\\Beatmaps"));

        Assert.Equal("layer", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveCustomSamplePath_ThrowsWhenBeatmapDirectoryIsEmpty(
        string beatmapDirectory)
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            Type = HitSoundType.Custom,
            Filename = "custom.wav"
        };

        ArgumentException exception =
            Assert.Throws<ArgumentException>(
                () => resolver.ResolveCustomSamplePath(
                    layer,
                    beatmapDirectory));

        Assert.Equal("beatmapDirectory", exception.ParamName);
    }

    [Theory]
    [InlineData(HitSoundType.Normal)]
    [InlineData(HitSoundType.SliderSlide)]
    [InlineData(HitSoundType.SliderTick)]
    public void ResolveCustomSamplePath_ThrowsWhenHitSoundTypeIsNotCustom(
        HitSoundType type)
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            Type = type,
            Filename = "custom.wav"
        };

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => resolver.ResolveCustomSamplePath(
                    layer,
                    "C:\\Beatmaps"));

        Assert.Equal("Type", exception.ParamName);
        Assert.Equal(type, exception.ActualValue);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveCustomSamplePath_ThrowsWhenFilenameIsEmpty(
        string filename)
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            Type = HitSoundType.Custom,
            Filename = filename
        };

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => resolver.ResolveCustomSamplePath(
                    layer,
                    "C:\\Beatmaps"));

        Assert.Equal(
            "A custom sample lookup requires a filename.",
            exception.Message);
    }
    [Fact]
    public void ResolveBeatmapSamplePath_ReturnsNullWhenBeatmapFilenameIsNull()
    {
        SampleResolver resolver = new SampleResolver();

        string? result = resolver.ResolveBeatmapSamplePath(
            null,
            "unused");

        Assert.Null(result);
    }

    [Fact]
    public void ResolveBeatmapSamplePath_ReturnsWavPathWhenWavExists()
    {
        SampleResolver resolver = new SampleResolver();

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        try
        {
            string expectedPath = Path.Combine(
                tempDirectory,
                "normal-hitnormal2.wav");

            File.WriteAllBytes(expectedPath, Array.Empty<byte>());

            string? result = resolver.ResolveBeatmapSamplePath(
                "normal-hitnormal2.wav",
                tempDirectory);

            Assert.Equal(expectedPath, result);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void ResolveBeatmapSamplePath_ReturnsMp3PathWhenWavDoesNotExist()
    {
        SampleResolver resolver = new SampleResolver();

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        try
        {
            string expectedPath = Path.Combine(
                tempDirectory,
                "normal-hitnormal2.mp3");

            File.WriteAllBytes(expectedPath, Array.Empty<byte>());

            string? result = resolver.ResolveBeatmapSamplePath(
                "normal-hitnormal2.wav",
                tempDirectory);

            Assert.Equal(expectedPath, result);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void ResolveBeatmapSamplePath_ReturnsOggPathWhenWavAndMp3DoNotExist()
    {
        SampleResolver resolver = new SampleResolver();

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        try
        {
            string expectedPath = Path.Combine(
                tempDirectory,
                "normal-hitnormal2.ogg");

            File.WriteAllBytes(expectedPath, Array.Empty<byte>());

            string? result = resolver.ResolveBeatmapSamplePath(
                "normal-hitnormal2.wav",
                tempDirectory);

            Assert.Equal(expectedPath, result);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void ResolveBeatmapSamplePath_PrefersWavOverMp3AndOgg()
    {
        SampleResolver resolver = new SampleResolver();

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        try
        {
            string wavPath = Path.Combine(tempDirectory, "soft-hitclap3.wav");
            string mp3Path = Path.Combine(tempDirectory, "soft-hitclap3.mp3");
            string oggPath = Path.Combine(tempDirectory, "soft-hitclap3.ogg");

            File.WriteAllBytes(wavPath, Array.Empty<byte>());
            File.WriteAllBytes(mp3Path, Array.Empty<byte>());
            File.WriteAllBytes(oggPath, Array.Empty<byte>());

            string? result = resolver.ResolveBeatmapSamplePath(
                "soft-hitclap3.wav",
                tempDirectory);

            Assert.Equal(wavPath, result);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void ResolveBeatmapSamplePath_PrefersMp3OverOgg()
    {
        SampleResolver resolver = new SampleResolver();

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        try
        {
            string mp3Path = Path.Combine(tempDirectory, "drum-slidertick4.mp3");
            string oggPath = Path.Combine(tempDirectory, "drum-slidertick4.ogg");

            File.WriteAllBytes(mp3Path, Array.Empty<byte>());
            File.WriteAllBytes(oggPath, Array.Empty<byte>());

            string? result = resolver.ResolveBeatmapSamplePath(
                "drum-slidertick4.wav",
                tempDirectory);

            Assert.Equal(mp3Path, result);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void ResolveBeatmapSamplePath_ReturnsNullWhenNoSupportedFileExists()
    {
        SampleResolver resolver = new SampleResolver();

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        try
        {
            string? result = resolver.ResolveBeatmapSamplePath(
                "normal-hitnormal2.wav",
                tempDirectory);

            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveBeatmapSamplePath_ThrowsWhenBeatmapDirectoryIsEmpty(
        string beatmapDirectory)
    {
        SampleResolver resolver = new SampleResolver();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(
                () => resolver.ResolveBeatmapSamplePath(
                    "normal-hitnormal.wav",
                    beatmapDirectory));

        Assert.Equal("beatmapDirectory", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveBeatmapSamplePath_ThrowsWhenBeatmapFilenameIsEmpty(
        string beatmapFilename)
    {
        SampleResolver resolver = new SampleResolver();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(
                () => resolver.ResolveBeatmapSamplePath(
                    beatmapFilename,
                    "C:\\Beatmaps"));

        Assert.Equal("beatmapFilename", exception.ParamName);
    }
    [Fact]
    public void ResolveSample_ReturnsCustomSampleFoundWhenCustomFileExists()
    {
        SampleResolver resolver = new SampleResolver();

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        try
        {
            string filename = "custom-hit.wav";
            string expectedPath = Path.Combine(tempDirectory, filename);

            File.WriteAllBytes(expectedPath, Array.Empty<byte>());

            HitSoundLayer layer = new HitSoundLayer
            {
                Type = HitSoundType.Custom,
                Filename = filename
            };

            SampleResolutionResult result =
                resolver.ResolveSample(layer, tempDirectory);

            Assert.Equal(
                SampleResolutionOutcome.CustomSampleFound,
                result.Outcome);

            Assert.Equal(expectedPath, result.ResolvedPath);
            Assert.Null(result.FallbackFilename);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void ResolveSample_ReturnsCustomSampleMissingWhenCustomFileDoesNotExist()
    {
        SampleResolver resolver = new SampleResolver();

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        try
        {
            HitSoundLayer layer = new HitSoundLayer
            {
                Type = HitSoundType.Custom,
                Filename = "missing.wav"
            };

            SampleResolutionResult result =
                resolver.ResolveSample(layer, tempDirectory);

            Assert.Equal(
                SampleResolutionOutcome.CustomSampleMissing,
                result.Outcome);

            Assert.Null(result.ResolvedPath);
            Assert.Null(result.FallbackFilename);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void ResolveSample_ReturnsBeatmapSampleFoundForStandardSample()
    {
        SampleResolver resolver = new SampleResolver();

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        try
        {
            string expectedPath = Path.Combine(
                tempDirectory,
                "soft-hitclap3.wav");

            File.WriteAllBytes(expectedPath, Array.Empty<byte>());

            HitSoundLayer layer = new HitSoundLayer
            {
                Type = HitSoundType.Clap,
                SampleSet = SampleSetType.Soft,
                SampleIndex = 3
            };

            SampleResolutionResult result =
                resolver.ResolveSample(layer, tempDirectory);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);

            Assert.Equal(expectedPath, result.ResolvedPath);
            Assert.Equal("soft-hitclap.wav", result.FallbackFilename);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void ResolveSample_ReturnsExternalFallbackRequiredWhenStandardSampleIsMissing()
    {
        SampleResolver resolver = new SampleResolver();

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        try
        {
            HitSoundLayer layer = new HitSoundLayer
            {
                Type = HitSoundType.Whistle,
                SampleSet = SampleSetType.Soft,
                SampleIndex = 4
            };

            SampleResolutionResult result =
                resolver.ResolveSample(layer, tempDirectory);

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                result.Outcome);

            Assert.Null(result.ResolvedPath);
            Assert.Equal(
                "soft-hitwhistle.wav",
                result.FallbackFilename);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void ResolveSample_DoesNotUseBeatmapSampleWhenSampleIndexIsZero()
    {
        SampleResolver resolver = new SampleResolver();

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        try
        {
            string localPath = Path.Combine(
                tempDirectory,
                "normal-hitnormal.wav");

            File.WriteAllBytes(localPath, Array.Empty<byte>());

            HitSoundLayer layer = new HitSoundLayer
            {
                Type = HitSoundType.Normal,
                SampleSet = SampleSetType.Normal,
                SampleIndex = 0
            };

            SampleResolutionResult result =
                resolver.ResolveSample(layer, tempDirectory);

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                result.Outcome);

            Assert.Null(result.ResolvedPath);
            Assert.Equal(
                "normal-hitnormal.wav",
                result.FallbackFilename);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void ResolveSample_ReturnsBeatmapSampleFoundForSliderBody()
    {
        SampleResolver resolver = new SampleResolver();

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        try
        {
            string expectedPath = Path.Combine(
                tempDirectory,
                "drum-sliderwhistle2.wav");

            File.WriteAllBytes(expectedPath, Array.Empty<byte>());

            HitSoundLayer layer = new HitSoundLayer
            {
                Type = HitSoundType.SliderWhistle,
                SampleSet = SampleSetType.Drum,
                SampleIndex = 2
            };

            SampleResolutionResult result =
                resolver.ResolveSample(layer, tempDirectory);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);

            Assert.Equal(expectedPath, result.ResolvedPath);
            Assert.Equal(
                "drum-sliderwhistle.wav",
                result.FallbackFilename);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void ResolveSample_ReturnsBeatmapSampleFoundForSliderTick()
    {
        SampleResolver resolver = new SampleResolver();

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempDirectory);

        try
        {
            string expectedPath = Path.Combine(
                tempDirectory,
                "normal-slidertick.wav");

            File.WriteAllBytes(expectedPath, Array.Empty<byte>());

            HitSoundLayer layer = new HitSoundLayer
            {
                Type = HitSoundType.SliderTick,
                SampleSet = SampleSetType.Normal,
                SampleIndex = 1
            };

            SampleResolutionResult result =
                resolver.ResolveSample(layer, tempDirectory);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);

            Assert.Equal(expectedPath, result.ResolvedPath);
            Assert.Equal(
                "normal-slidertick.wav",
                result.FallbackFilename);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void ResolveSample_ThrowsWhenLayerIsNull()
    {
        SampleResolver resolver = new SampleResolver();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(
                () => resolver.ResolveSample(
                    null!,
                    "unused"));

        Assert.Equal("layer", exception.ParamName);
    }

    [Fact]
    public void ResolveSample_ThrowsWhenHitSoundTypeIsUnsupported()
    {
        SampleResolver resolver = new SampleResolver();

        HitSoundLayer layer = new HitSoundLayer
        {
            Type = (HitSoundType)999,
            SampleSet = SampleSetType.Normal,
            SampleIndex = 1
        };

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => resolver.ResolveSample(
                    layer,
                    "unused"));

        Assert.Equal("Type", exception.ParamName);
        Assert.Equal(layer.Type, exception.ActualValue);
        Assert.Contains(
            "The hitsound type is not supported for sample resolution.",
            exception.Message);
    }
}