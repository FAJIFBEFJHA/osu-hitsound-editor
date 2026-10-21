using System.Reflection;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OsuHitsoundEditor;

namespace OsuHitsoundEditor.Tests;

public class HitSoundPlaybackServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlayHitSoundLayer_WhenSampleExists_PlaysAtLayerVolume(bool custom)
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(directory);

        try
        {
            string filename = custom
                ? "custom.wav"
                : "normal-hitnormal.wav";

            string path = Path.Combine(directory, filename);
            var format = new WaveFormat(44100, 16, 1);

            using (var writer = new WaveFileWriter(path, format))
            {
                byte[] audio = new byte[format.BlockAlign * 100];

                for (int i = 0; i < audio.Length; i += 2)
                {
                    audio[i] = 0;
                    audio[i + 1] = 64;
                }

                writer.Write(audio, 0, audio.Length);
            }

            HitSoundLayer layer = new HitSoundLayer
            {
                Type = custom ? HitSoundType.Custom : HitSoundType.Normal,
                Filename = custom ? filename : string.Empty,
                SampleSet = SampleSetType.Normal,
                SampleIndex = 1,
                Volume = 50
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();
            using var engine = new AudioPlaybackEngine();

            var mixer = new MixingSampleProvider(
                WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));

            FieldInfo? mixerField = typeof(AudioPlaybackEngine).GetField(
                "mixer",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(mixerField);
            mixerField.SetValue(engine, mixer);

            SampleResolutionResult result = service.PlayHitSoundLayer(
                layer, directory, resolver, engine);

            Assert.Equal(
                custom
                    ? SampleResolutionOutcome.CustomSampleFound
                    : SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);

            Assert.Equal(path, result.ResolvedPath);

            float[] output = new float[2];
            int samplesRead = mixer.Read(output.AsSpan());

            Assert.Equal(2, samplesRead);
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlayHitSoundLayer_WhenSampleIsMissing_DoesNotAttemptPlayback(bool custom)
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(directory);

        try
        {
            HitSoundLayer layer = new HitSoundLayer
            {
                Type = custom ? HitSoundType.Custom : HitSoundType.Normal,
                Filename = custom ? "missing.wav" : string.Empty,
                SampleSet = SampleSetType.Normal,
                SampleIndex = 1,
                Volume = 50
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();
            using var engine = new AudioPlaybackEngine();

            SampleResolutionResult result = service.PlayHitSoundLayer(
                layer, directory, resolver, engine);

            Assert.Equal(
                custom
                    ? SampleResolutionOutcome.CustomSampleMissing
                    : SampleResolutionOutcome.ExternalFallbackRequired,
                result.Outcome);

            Assert.Null(result.ResolvedPath);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayHitSoundLayer_WhenLayerIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayHitSoundLayer(
                    null!, "beatmap", resolver, engine));

        Assert.Equal("layer", exception.ParamName);
    }

    [Fact]
    public void PlayHitSoundLayer_WhenResolverIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var layer = new HitSoundLayer();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayHitSoundLayer(
                    layer, "beatmap", null!, engine));

        Assert.Equal("resolver", exception.ParamName);
    }

    [Fact]
    public void PlayHitSoundLayer_WhenEngineIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var layer = new HitSoundLayer();
        var resolver = new SampleResolver();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayHitSoundLayer(
                    layer, "beatmap", resolver, null!));

        Assert.Equal("engine", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlayHitSoundLayer_WhenDirectoryIsInvalid_ThrowsArgumentException(
        string? directory)
    {
        var service = new HitSoundPlaybackService();
        var layer = new HitSoundLayer();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlayHitSoundLayer(
                    layer, directory!, resolver, engine));

        Assert.Equal("beatmapDirectory", exception.ParamName);
        Assert.Contains(
            "The beatmap directory must not be empty.",
            exception.Message);
    }

    // Multiple-layer playback
    [Fact]
    public void PlayHitSoundLayers_WhenListIsEmpty_ReturnsEmptyList()
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        List<SampleResolutionResult> results =
            service.PlayHitSoundLayers(
                Array.Empty<HitSoundLayer>(),
                "beatmap",
                resolver,
                engine);

        Assert.Empty(results);
    }

    [Fact]
    public void PlayHitSoundLayers_WhenTwoSamplesExist_MixesIndividualVolumes()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(
                directory, "normal-hitnormal.wav");

            string clapPath = Path.Combine(
                directory, "normal-hitclap.wav");

            WriteHalfAmplitudeWav(normalPath);
            WriteHalfAmplitudeWav(clapPath);

            HitSoundLayer[] layers =
            {
                new()
                {
                    Type = HitSoundType.Normal,
                    SampleSet = SampleSetType.Normal,
                    SampleIndex = 1,
                    Volume = 50
                },
                new()
                {
                    Type = HitSoundType.Clap,
                    SampleSet = SampleSetType.Normal,
                    SampleIndex = 1,
                    Volume = 25
                }
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlayHitSoundLayers(
                    layers, directory, resolver, engine);

            Assert.Equal(2, results.Count);

            Assert.All(results, result =>
                Assert.Equal(
                    SampleResolutionOutcome.BeatmapSampleFound,
                    result.Outcome));

            Assert.Equal(normalPath, results[0].ResolvedPath);
            Assert.Equal(clapPath, results[1].ResolvedPath);

            float[] output = new float[2];
            int samplesRead = mixer.Read(output.AsSpan());

            Assert.Equal(2, samplesRead);

            // 0.5 * 0.50 + 0.5 * 0.25 = 0.375
            Assert.InRange(output[0], 0.365f, 0.385f);
            Assert.InRange(output[1], 0.365f, 0.385f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayHitSoundLayers_WhenSamplesAreMissing_ContinuesInOrder()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(
                directory, "normal-hitnormal.wav");

            WriteHalfAmplitudeWav(normalPath);

            HitSoundLayer[] layers =
            {
                new()
                {
                    Type = HitSoundType.Custom,
                    Filename = "missing.wav",
                    Volume = 50
                },
                new()
                {
                    Type = HitSoundType.Normal,
                    SampleSet = SampleSetType.Normal,
                    SampleIndex = 1,
                    Volume = 75
                },
                new()
                {
                    Type = HitSoundType.Normal,
                    SampleSet = SampleSetType.Normal,
                    SampleIndex = 0,
                    Volume = 50
                }
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlayHitSoundLayers(
                    layers, directory, resolver, engine);

            Assert.Equal(3, results.Count);

            Assert.Equal(
                SampleResolutionOutcome.CustomSampleMissing,
                results[0].Outcome);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                results[1].Outcome);

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                results[2].Outcome);

            Assert.Null(results[0].ResolvedPath);
            Assert.Equal(normalPath, results[1].ResolvedPath);
            Assert.Null(results[2].ResolvedPath);

            float[] output = new float[2];
            int samplesRead = mixer.Read(output.AsSpan());

            Assert.Equal(2, samplesRead);

            // Solo se reproduce la capa encontrada.
            // 0.5 * 0.75 = 0.375
            Assert.InRange(output[0], 0.365f, 0.385f);
            Assert.InRange(output[1], 0.365f, 0.385f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayHitSoundLayers_WhenLaterLayerIsNull_PropagatesException()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(
                directory, "normal-hitnormal.wav");

            WriteHalfAmplitudeWav(normalPath);

            HitSoundLayer[] layers =
            {
                new()
                {
                    Type = HitSoundType.Normal,
                    SampleSet = SampleSetType.Normal,
                    SampleIndex = 1,
                    Volume = 100
                },
                null!
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            ArgumentNullException exception =
                Assert.Throws<ArgumentNullException>(() =>
                    service.PlayHitSoundLayers(
                        layers, directory, resolver, engine));

            Assert.Equal("layer", exception.ParamName);

            // La primera capa ya fue agregada al mixer.
            float[] output = new float[2];
            int samplesRead = mixer.Read(output.AsSpan());

            Assert.Equal(2, samplesRead);
            Assert.InRange(output[0], 0.49f, 0.51f);
            Assert.InRange(output[1], 0.49f, 0.51f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayHitSoundLayers_WhenLayersIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayHitSoundLayers(
                    null!, "beatmap", resolver, engine));

        Assert.Equal("layers", exception.ParamName);
    }

    [Fact]
    public void PlayHitSoundLayers_WhenResolverIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayHitSoundLayers(
                    Array.Empty<HitSoundLayer>(),
                    "beatmap",
                    null!,
                    engine));

        Assert.Equal("resolver", exception.ParamName);
    }

    [Fact]
    public void PlayHitSoundLayers_WhenEngineIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayHitSoundLayers(
                    Array.Empty<HitSoundLayer>(),
                    "beatmap",
                    resolver,
                    null!));

        Assert.Equal("engine", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlayHitSoundLayers_WhenDirectoryIsInvalid_ThrowsArgumentException(
        string? directory)
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlayHitSoundLayers(
                    Array.Empty<HitSoundLayer>(),
                    directory!,
                    resolver,
                    engine));

        Assert.Equal("beatmapDirectory", exception.ParamName);

        Assert.Contains(
            "The beatmap directory must not be empty.",
            exception.Message);
    }

    // Hit-circle playback
    [Fact]
    public void PlayHitCircle_WhenNormalAndClapExist_PlaysBothLayers()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(directory, "normal-hitnormal.wav");
            string clapPath = Path.Combine(directory, "normal-hitclap.wav");

            WriteHalfAmplitudeWav(normalPath);
            WriteHalfAmplitudeWav(clapPath);

            var beatmap = new Beatmap();
            var hitObject = new HitObject
            {
                ObjectType = 1,
                HitSound = 8,
                HitSample = new HitSample
                {
                    NormalSet = 1,
                    AdditionSet = 1,
                    Index = 1,
                    Volume = 50
                }
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlayHitCircle(
                    beatmap, hitObject, directory, resolver, engine);

            Assert.Equal(2, results.Count);
            Assert.Equal(normalPath, results[0].ResolvedPath);
            Assert.Equal(clapPath, results[1].ResolvedPath);

            Assert.All(results, result =>
                Assert.Equal(
                    SampleResolutionOutcome.BeatmapSampleFound,
                    result.Outcome));

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // Two samples: 0.5 * 0.5 + 0.5 * 0.5 = 0.5
            Assert.InRange(output[0], 0.49f, 0.51f);
            Assert.InRange(output[1], 0.49f, 0.51f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayHitCircle_WhenCustomFilenameExists_PlaysOnlyCustomSample()
    {
        string directory = CreateTempDirectory();

        try
        {
            string customPath = Path.Combine(directory, "custom.wav");
            WriteHalfAmplitudeWav(customPath);

            var beatmap = new Beatmap();
            var hitObject = new HitObject
            {
                ObjectType = 1,
                HitSound = 8,
                HitSample = new HitSample
                {
                    Filename = "custom.wav",
                    Volume = 40
                }
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlayHitCircle(
                    beatmap, hitObject, directory, resolver, engine);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.CustomSampleFound,
                result.Outcome);

            Assert.Equal(customPath, result.ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // One custom sample: 0.5 * 0.4 = 0.2
            Assert.InRange(output[0], 0.19f, 0.21f);
            Assert.InRange(output[1], 0.19f, 0.21f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayHitCircle_WhenClapIsMissing_PlaysNormalAndReturnsFallback()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(directory, "normal-hitnormal.wav");
            WriteHalfAmplitudeWav(normalPath);

            var beatmap = new Beatmap();
            var hitObject = new HitObject
            {
                ObjectType = 1,
                HitSound = 8,
                HitSample = new HitSample
                {
                    NormalSet = 1,
                    AdditionSet = 1,
                    Index = 1,
                    Volume = 50
                }
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlayHitCircle(
                    beatmap, hitObject, directory, resolver, engine);

            Assert.Equal(2, results.Count);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                results[0].Outcome);

            Assert.Equal(normalPath, results[0].ResolvedPath);

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                results[1].Outcome);

            Assert.Null(results[1].ResolvedPath);
            Assert.Equal(
                "normal-hitclap.wav",
                results[1].FallbackFilename);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // Only normal is played: 0.5 * 0.5 = 0.25
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(8)]
    public void PlayHitCircle_WhenObjectIsNotHitCircle_ThrowsArgumentException(
        int objectType)
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var hitObject = new HitObject { ObjectType = objectType };
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlayHitCircle(
                    beatmap, hitObject, "beatmap", resolver, engine));

        Assert.Equal("hitObject", exception.ParamName);
        Assert.Contains(
            "The hit object must be a hit circle.",
            exception.Message);
    }

    [Fact]
    public void PlayHitCircle_WhenBeatmapIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var hitObject = new HitObject { ObjectType = 1 };
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayHitCircle(
                    null!, hitObject, "beatmap", resolver, engine));

        Assert.Equal("beatmap", exception.ParamName);
    }

    [Fact]
    public void PlayHitCircle_WhenHitObjectIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayHitCircle(
                    beatmap, null!, "beatmap", resolver, engine));

        Assert.Equal("hitObject", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlayHitCircle_WhenDirectoryIsInvalid_ThrowsArgumentException(
        string? directory)
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();

        var hitObject = new HitObject
        {
            ObjectType = 1,
            HitSample = new HitSample
            {
                NormalSet = 1,
                Index = 1,
                Volume = 50
            }
        };

        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlayHitCircle(
                    beatmap, hitObject, directory!, resolver, engine));

        Assert.Equal("beatmapDirectory", exception.ParamName);
    }

    // Shared test helpers
    private static string CreateTempDirectory()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"osu-hitsound-layers-{Guid.NewGuid():N}");

        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteHalfAmplitudeWav(string path)
    {
        var format = new WaveFormat(44100, 16, 1);

        using var writer = new WaveFileWriter(path, format);

        byte[] audio = new byte[format.BlockAlign * 100];

        for (int i = 0; i < audio.Length; i += 2)
        {
            audio[i] = 0;
            audio[i + 1] = 64;
        }

        writer.Write(audio, 0, audio.Length);
    }

    private static AudioPlaybackEngine CreateEngineWithMixer(
        out MixingSampleProvider mixer)
    {
        var engine = new AudioPlaybackEngine();

        mixer = new MixingSampleProvider(
            WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));

        FieldInfo? mixerField =
            typeof(AudioPlaybackEngine).GetField(
                "mixer",
                BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(mixerField);
        mixerField.SetValue(engine, mixer);

        return engine;
    }

    // Time-range playback

    [Fact]
    public void PlayHitCirclesInTimeRange_WhenBeatmapIsEmpty_ReturnsEmptyList()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        List<(HitObject HitObject, List<SampleResolutionResult> Results)> results =
            service.PlayHitCirclesInTimeRange(
                beatmap, 1000, 1500, "beatmap", resolver, engine);

        Assert.Empty(results);
    }

    [Fact]
    public void PlayHitCirclesInTimeRange_WhenTwoCirclesMatch_PlaysBothAndPreservesBeatmapOrder()
    {
        string directory = CreateTempDirectory();

        try
        {
            string latePath = Path.Combine(directory, "late.wav");
            string earlyPath = Path.Combine(directory, "early.wav");
            WriteHalfAmplitudeWav(latePath);
            WriteHalfAmplitudeWav(earlyPath);

            HitObject lateCircle = new()
            {
                ObjectType = 1,
                Time = 1400,
                HitSample = new HitSample { Filename = "late.wav", Volume = 25 }
            };

            HitObject earlyCircle = new()
            {
                ObjectType = 1,
                Time = 1200,
                HitSample = new HitSample { Filename = "early.wav", Volume = 50 }
            };

            var beatmap = new Beatmap();
            beatmap.HitObjects.AddRange(new[] { lateCircle, earlyCircle });

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();
            using var engine = CreateEngineWithMixer(out MixingSampleProvider mixer);

            var results = service.PlayHitCirclesInTimeRange(
                beatmap, 1000, 1500, directory, resolver, engine);

            Assert.Equal(2, results.Count);
            Assert.Same(lateCircle, results[0].HitObject);
            Assert.Same(earlyCircle, results[1].HitObject);

            SampleResolutionResult lateResult = Assert.Single(results[0].Results);
            SampleResolutionResult earlyResult = Assert.Single(results[1].Results);

            Assert.Equal(
                SampleResolutionOutcome.CustomSampleFound,
                lateResult.Outcome);

            Assert.Equal(
                SampleResolutionOutcome.CustomSampleFound,
                earlyResult.Outcome);

            Assert.Equal(latePath, lateResult.ResolvedPath);
            Assert.Equal(earlyPath, earlyResult.ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // 0.5 * 0.25 + 0.5 * 0.50 = 0.375
            Assert.InRange(output[0], 0.365f, 0.385f);
            Assert.InRange(output[1], 0.365f, 0.385f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayHitCirclesInTimeRange_ExcludesStartAndNonCircles_IncludesEnd()
    {
        string directory = CreateTempDirectory();

        try
        {
            HitObject atStart = new()
            {
                ObjectType = 1,
                Time = 1000,
                HitSample = new HitSample { Filename = "missing.wav" }
            };

            HitObject slider = new() { ObjectType = 2, Time = 1200 };

            HitObject inside = new()
            {
                ObjectType = 1,
                Time = 1300,
                HitSample = new HitSample { Filename = "missing.wav" }
            };

            HitObject atEnd = new()
            {
                ObjectType = 1,
                Time = 1500,
                HitSample = new HitSample { Filename = "missing.wav" }
            };

            HitObject afterEnd = new()
            {
                ObjectType = 1,
                Time = 1501,
                HitSample = new HitSample { Filename = "missing.wav" }
            };

            var beatmap = new Beatmap();

            beatmap.HitObjects.AddRange(
                new[] { atStart, slider, inside, atEnd, afterEnd });

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();
            using var engine = new AudioPlaybackEngine();

            var results = service.PlayHitCirclesInTimeRange(
                beatmap, 1000, 1500, directory, resolver, engine);

            Assert.Equal(2, results.Count);
            Assert.Same(inside, results[0].HitObject);
            Assert.Same(atEnd, results[1].HitObject);

            Assert.All(results, item =>
                Assert.Equal(
                    SampleResolutionOutcome.CustomSampleMissing,
                    Assert.Single(item.Results).Outcome));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayHitCirclesInTimeRange_WhenSamplesAreMissing_ContinuesAndKeepsOutcomes()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(
                directory, "normal-hitnormal.wav");

            WriteHalfAmplitudeWav(normalPath);

            HitObject missingCustom = new()
            {
                ObjectType = 1,
                Time = 1000,
                HitSample = new HitSample
                {
                    Filename = "missing.wav",
                    Volume = 50
                }
            };

            HitObject found = new()
            {
                ObjectType = 1,
                Time = 1200,
                HitSample = new HitSample
                {
                    NormalSet = 1,
                    Index = 1,
                    Volume = 50
                }
            };

            HitObject externalFallback = new()
            {
                ObjectType = 1,
                Time = 1400,
                HitSample = new HitSample
                {
                    NormalSet = 1,
                    Index = 0,
                    Volume = 50
                }
            };

            var beatmap = new Beatmap();

            beatmap.HitObjects.AddRange(
                new[] { missingCustom, found, externalFallback });

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();
            using var engine = CreateEngineWithMixer(out MixingSampleProvider mixer);

            var results = service.PlayHitCirclesInTimeRange(
                beatmap, 999, 1500, directory, resolver, engine);

            Assert.Equal(3, results.Count);

            Assert.Same(missingCustom, results[0].HitObject);
            Assert.Same(found, results[1].HitObject);
            Assert.Same(externalFallback, results[2].HitObject);

            Assert.Equal(
                SampleResolutionOutcome.CustomSampleMissing,
                Assert.Single(results[0].Results).Outcome);

            SampleResolutionResult foundResult =
                Assert.Single(results[1].Results);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                foundResult.Outcome);

            Assert.Equal(normalPath, foundResult.ResolvedPath);

            SampleResolutionResult fallbackResult =
                Assert.Single(results[2].Results);

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                fallbackResult.Outcome);

            Assert.Null(fallbackResult.ResolvedPath);

            Assert.Equal(
                "normal-hitnormal.wav",
                fallbackResult.FallbackFilename);

            float[] output = new float[2];

            Assert.Equal(2, mixer.Read(output.AsSpan()));
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayHitCirclesInTimeRange_WhenStartIsNegative_IncludesZeroTime()
    {
        string directory = CreateTempDirectory();

        try
        {
            HitObject zeroTimeCircle = new()
            {
                ObjectType = 1,
                Time = 0,
                HitSample = new HitSample { Filename = "missing.wav" }
            };

            var beatmap = new Beatmap();
            beatmap.HitObjects.Add(zeroTimeCircle);

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();
            using var engine = new AudioPlaybackEngine();

            var results = service.PlayHitCirclesInTimeRange(
                beatmap, -1, 0, directory, resolver, engine);

            var item = Assert.Single(results);

            Assert.Same(zeroTimeCircle, item.HitObject);

            Assert.Equal(
                SampleResolutionOutcome.CustomSampleMissing,
                Assert.Single(item.Results).Outcome);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayHitCirclesInTimeRange_WhenLaterCircleFails_LeavesEarlierInputInMixer()
    {
        string directory = CreateTempDirectory();

        try
        {
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "first.wav"));

            WriteHalfAmplitudeWav(
                Path.Combine(directory, "second.wav"));

            var beatmap = new Beatmap();

            beatmap.HitObjects.AddRange(new[]
            {
                new HitObject
                {
                    ObjectType = 1,
                    Time = 1000,
                    HitSample = new HitSample
                    {
                        Filename = "first.wav",
                        Volume = 50
                    }
                },
                new HitObject
                {
                    ObjectType = 1,
                    Time = 1200,
                    HitSample = new HitSample
                    {
                        Filename = "second.wav",
                        Volume = 101
                    }
                }
            });

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();
            using var engine = CreateEngineWithMixer(out MixingSampleProvider mixer);

            ArgumentOutOfRangeException exception =
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    service.PlayHitCirclesInTimeRange(
                        beatmap, 999, 1500, directory, resolver, engine));

            Assert.Equal("volume", exception.ParamName);
            Assert.Equal(101, exception.ActualValue);

            float[] output = new float[2];

            Assert.Equal(2, mixer.Read(output.AsSpan()));
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayHitCirclesInTimeRange_WhenBeatmapIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayHitCirclesInTimeRange(
                    null!, 0, 1000, "beatmap", resolver, engine));

        Assert.Equal("beatmap", exception.ParamName);
    }

    [Fact]
    public void PlayHitCirclesInTimeRange_WhenResolverIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayHitCirclesInTimeRange(
                    beatmap, 0, 1000, "beatmap", null!, engine));

        Assert.Equal("resolver", exception.ParamName);
    }

    [Fact]
    public void PlayHitCirclesInTimeRange_WhenEngineIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var resolver = new SampleResolver();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayHitCirclesInTimeRange(
                    beatmap, 0, 1000, "beatmap", resolver, null!));

        Assert.Equal("engine", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlayHitCirclesInTimeRange_WhenDirectoryIsInvalid_ThrowsArgumentException(
        string? directory)
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlayHitCirclesInTimeRange(
                    beatmap, 0, 1000, directory!, resolver, engine));

        Assert.Equal("beatmapDirectory", exception.ParamName);

        Assert.Contains(
            "The beatmap directory must not be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PlayHitCirclesInTimeRange_WhenStartIsNotFinite_ThrowsArgumentOutOfRangeException(
        double invalidStart)
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlayHitCirclesInTimeRange(
                    beatmap,
                    invalidStart,
                    1000,
                    "beatmap",
                    resolver,
                    engine));

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
    public void PlayHitCirclesInTimeRange_WhenEndIsNotFinite_ThrowsArgumentOutOfRangeException(
        double invalidEnd)
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlayHitCirclesInTimeRange(
                    beatmap,
                    0,
                    invalidEnd,
                    "beatmap",
                    resolver,
                    engine));

        Assert.Equal(
            "endInclusiveMilliseconds",
            exception.ParamName);

        Assert.Contains(
            "The end time must be finite.",
            exception.Message);
    }
    [Fact]
    public void PlayHitCirclesInTimeRange_WhenEndPrecedesStart_ThrowsArgumentOutOfRangeException()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlayHitCirclesInTimeRange(
                    beatmap, 2000, 1000, "beatmap", resolver, engine));

        Assert.Equal(
            "endInclusiveMilliseconds",
            exception.ParamName);

        Assert.Contains(
            "The end time must be greater than or equal to the start time.",
            exception.Message);
    }

    [Fact]
    public void PlayPendingHitCircles_WhenBeatmapIsEmpty_ReturnsEmptyList()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var cursor = new HitCirclePlaybackCursor();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        var results = service.PlayPendingHitCircles(
            beatmap, cursor, 1000, "beatmap", resolver, engine);

        Assert.Empty(results);
    }

    [Fact]
    public void PlayPendingHitCircles_ConsecutiveAdvances_DoNotRepeatEvents()
    {
        string directory = CreateTempDirectory();

        try
        {
            string firstPath = Path.Combine(directory, "first.wav");
            string secondPath = Path.Combine(directory, "second.wav");

            WriteHalfAmplitudeWav(firstPath);
            WriteHalfAmplitudeWav(secondPath);

            HitObject first = new()
            {
                ObjectType = 1,
                Time = 1000,
                HitSample = new HitSample
                {
                    Filename = "first.wav",
                    Volume = 50
                }
            };

            HitObject second = new()
            {
                ObjectType = 1,
                Time = 1500,
                HitSample = new HitSample
                {
                    Filename = "second.wav",
                    Volume = 25
                }
            };

            var beatmap = new Beatmap();
            beatmap.HitObjects.AddRange(new[] { first, second });

            var service = new HitSoundPlaybackService();
            var cursor = new HitCirclePlaybackCursor();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            var firstResults = service.PlayPendingHitCircles(
                beatmap, cursor, 1000, directory, resolver, engine);

            var secondResults = service.PlayPendingHitCircles(
                beatmap, cursor, 1500, directory, resolver, engine);

            var repeatedResults = service.PlayPendingHitCircles(
                beatmap, cursor, 1500, directory, resolver, engine);

            Assert.Same(first, Assert.Single(firstResults).HitObject);
            Assert.Same(second, Assert.Single(secondResults).HitObject);
            Assert.Empty(repeatedResults);

            Assert.Equal(
                firstPath,
                Assert.Single(firstResults[0].Results).ResolvedPath);

            Assert.Equal(
                secondPath,
                Assert.Single(secondResults[0].Results).ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // 0.5 * 0.50 + 0.5 * 0.25 = 0.375
            Assert.InRange(output[0], 0.365f, 0.385f);
            Assert.InRange(output[1], 0.365f, 0.385f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayPendingHitCircles_WhenCirclesShareTime_ProcessesBoth()
    {
        string directory = CreateTempDirectory();

        try
        {
            HitObject first = new()
            {
                ObjectType = 1,
                Time = 1000,
                HitSample = new HitSample { Filename = "missing-a.wav" }
            };

            HitObject second = new()
            {
                ObjectType = 1,
                Time = 1000,
                HitSample = new HitSample { Filename = "missing-b.wav" }
            };

            var beatmap = new Beatmap();
            beatmap.HitObjects.AddRange(new[] { first, second });

            var service = new HitSoundPlaybackService();
            var cursor = new HitCirclePlaybackCursor();
            var resolver = new SampleResolver();
            using var engine = new AudioPlaybackEngine();

            var results = service.PlayPendingHitCircles(
                beatmap, cursor, 1000, directory, resolver, engine);

            Assert.Equal(2, results.Count);
            Assert.Same(first, results[0].HitObject);
            Assert.Same(second, results[1].HitObject);

            Assert.All(results, item =>
                Assert.Equal(
                    SampleResolutionOutcome.CustomSampleMissing,
                    Assert.Single(item.Results).Outcome));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayPendingHitCircles_WhenSampleIsMissing_ContinuesToNextCircle()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(
                directory, "normal-hitnormal.wav");

            WriteHalfAmplitudeWav(normalPath);

            var beatmap = new Beatmap();

            beatmap.HitObjects.AddRange(new[]
            {
                new HitObject
                {
                    ObjectType = 1,
                    Time = 500,
                    HitSample = new HitSample
                    {
                        Filename = "missing.wav"
                    }
                },
                new HitObject
                {
                    ObjectType = 1,
                    Time = 1000,
                    HitSample = new HitSample
                    {
                        NormalSet = 1,
                        Index = 1,
                        Volume = 50
                    }
                }
            });

            var service = new HitSoundPlaybackService();
            var cursor = new HitCirclePlaybackCursor();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            var results = service.PlayPendingHitCircles(
                beatmap, cursor, 1000, directory, resolver, engine);

            Assert.Equal(2, results.Count);

            Assert.Equal(
                SampleResolutionOutcome.CustomSampleMissing,
                Assert.Single(results[0].Results).Outcome);

            SampleResolutionResult found =
                Assert.Single(results[1].Results);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                found.Outcome);

            Assert.Equal(normalPath, found.ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayPendingHitCircles_AfterSeekToZero_ReprocessesZeroTimeCircle()
    {
        string directory = CreateTempDirectory();

        try
        {
            HitObject circle = new()
            {
                ObjectType = 1,
                Time = 0,
                HitSample = new HitSample
                {
                    Filename = "missing.wav"
                }
            };

            var beatmap = new Beatmap();
            beatmap.HitObjects.Add(circle);

            var service = new HitSoundPlaybackService();
            var cursor = new HitCirclePlaybackCursor();
            var resolver = new SampleResolver();
            using var engine = new AudioPlaybackEngine();

            var firstResults = service.PlayPendingHitCircles(
                beatmap, cursor, 0, directory, resolver, engine);

            Assert.Same(circle, Assert.Single(firstResults).HitObject);

            cursor.SeekTo(0);

            var secondResults = service.PlayPendingHitCircles(
                beatmap, cursor, 0, directory, resolver, engine);

            Assert.Same(circle, Assert.Single(secondResults).HitObject);

            Assert.Empty(service.PlayPendingHitCircles(
                beatmap, cursor, 0, directory, resolver, engine));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayPendingHitCircles_AfterSeekForward_SkipsEarlierCircles()
    {
        string directory = CreateTempDirectory();

        try
        {
            HitObject skipped = new()
            {
                ObjectType = 1,
                Time = 500,
                HitSample = new HitSample { Filename = "missing-a.wav" }
            };

            HitObject atSeekPosition = new()
            {
                ObjectType = 1,
                Time = 1000,
                HitSample = new HitSample { Filename = "missing-b.wav" }
            };

            HitObject pending = new()
            {
                ObjectType = 1,
                Time = 1200,
                HitSample = new HitSample { Filename = "missing-c.wav" }
            };

            var beatmap = new Beatmap();

            beatmap.HitObjects.AddRange(
                new[] { skipped, atSeekPosition, pending });

            var service = new HitSoundPlaybackService();
            var cursor = new HitCirclePlaybackCursor();
            var resolver = new SampleResolver();
            using var engine = new AudioPlaybackEngine();

            cursor.SeekTo(1000);

            var results = service.PlayPendingHitCircles(
                beatmap, cursor, 1200, directory, resolver, engine);

            Assert.Same(pending, Assert.Single(results).HitObject);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayPendingHitCircles_SkipsNonCircleObjects()
    {
        string directory = CreateTempDirectory();

        try
        {
            HitObject slider = new() { ObjectType = 2, Time = 500 };
            HitObject spinner = new() { ObjectType = 8, Time = 750 };

            HitObject circle = new()
            {
                ObjectType = 1,
                Time = 1000,
                HitSample = new HitSample
                {
                    Filename = "missing.wav"
                }
            };

            var beatmap = new Beatmap();

            beatmap.HitObjects.AddRange(
                new[] { slider, spinner, circle });

            var service = new HitSoundPlaybackService();
            var cursor = new HitCirclePlaybackCursor();
            var resolver = new SampleResolver();
            using var engine = new AudioPlaybackEngine();

            var results = service.PlayPendingHitCircles(
                beatmap, cursor, 1000, directory, resolver, engine);

            Assert.Same(circle, Assert.Single(results).HitObject);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayPendingHitCircles_WhenLaterCircleFails_CursorRemainsAdvanced()
    {
        string directory = CreateTempDirectory();

        try
        {
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "first.wav"));

            WriteHalfAmplitudeWav(
                Path.Combine(directory, "second.wav"));

            var beatmap = new Beatmap();

            beatmap.HitObjects.AddRange(new[]
            {
                new HitObject
                {
                    ObjectType = 1,
                    Time = 500,
                    HitSample = new HitSample
                    {
                        Filename = "first.wav",
                        Volume = 50
                    }
                },
                new HitObject
                {
                    ObjectType = 1,
                    Time = 1000,
                    HitSample = new HitSample
                    {
                        Filename = "second.wav",
                        Volume = 101
                    }
                }
            });

            var service = new HitSoundPlaybackService();
            var cursor = new HitCirclePlaybackCursor();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            ArgumentOutOfRangeException exception =
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    service.PlayPendingHitCircles(
                        beatmap, cursor, 1000, directory, resolver, engine));

            Assert.Equal("volume", exception.ParamName);
            Assert.Equal(101, exception.ActualValue);

            // The cursor has already advanced even though playback failed.
            Assert.Empty(service.PlayPendingHitCircles(
                beatmap, cursor, 1000, directory, resolver, engine));

            // The successfully added first sample remains in the mixer.
            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayPendingHitCircles_WhenBeatmapIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var cursor = new HitCirclePlaybackCursor();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayPendingHitCircles(
                    null!, cursor, 1000, "beatmap", resolver, engine));

        Assert.Equal("beatmap", exception.ParamName);
    }

    [Fact]
    public void PlayPendingHitCircles_WhenCursorIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayPendingHitCircles(
                    beatmap, null!, 1000, "beatmap", resolver, engine));

        Assert.Equal("cursor", exception.ParamName);
    }

    [Fact]
    public void PlayPendingHitCircles_WhenResolverIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var cursor = new HitCirclePlaybackCursor();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayPendingHitCircles(
                    beatmap, cursor, 1000, "beatmap", null!, engine));

        Assert.Equal("resolver", exception.ParamName);
    }

    [Fact]
    public void PlayPendingHitCircles_WhenEngineIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var cursor = new HitCirclePlaybackCursor();
        var resolver = new SampleResolver();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayPendingHitCircles(
                    beatmap, cursor, 1000, "beatmap", resolver, null!));

        Assert.Equal("engine", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlayPendingHitCircles_WhenDirectoryIsInvalid_ThrowsArgumentException(
        string? directory)
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var cursor = new HitCirclePlaybackCursor();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlayPendingHitCircles(
                    beatmap, cursor, 1000, directory!, resolver, engine));

        Assert.Equal("beatmapDirectory", exception.ParamName);

        Assert.Contains(
            "The beatmap directory must not be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PlayPendingHitCircles_WhenPositionIsInvalid_ThrowsArgumentOutOfRangeException(
        double invalidPosition)
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var cursor = new HitCirclePlaybackCursor();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlayPendingHitCircles(
                    beatmap,
                    cursor,
                    invalidPosition,
                    "beatmap",
                    resolver,
                    engine));

        Assert.Equal(
            "currentPositionMilliseconds",
            exception.ParamName);

        Assert.Contains(
            "The timeline position must be finite and non-negative.",
            exception.Message);
    }

    [Fact]
    public void PlayPendingHitCircles_WhenPositionMovesBackward_ThrowsInvalidOperationException()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var cursor = new HitCirclePlaybackCursor();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        service.PlayPendingHitCircles(
            beatmap, cursor, 1500, "beatmap", resolver, engine);

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                service.PlayPendingHitCircles(
                    beatmap, cursor, 1000, "beatmap", resolver, engine));

        Assert.Equal(
            "The timeline position moved backwards. Reset the playback cursor after seeking.",
            exception.Message);
    }

    [Fact]
    public void PlaySliderEdge_Head_PlaysNormalAtTimingPointVolume()
    {
        string directory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(directory, "normal-hitnormal.wav");
            WriteHalfAmplitudeWav(path);

            var (beatmap, slider) = CreateTwoSpanSlider();

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlaySliderEdge(
                    beatmap, slider, 0, directory, resolver, engine);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);
            Assert.Equal(path, result.ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // 0.5 amplitude * 50% volume = 0.25
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderEdge_Repeat_UsesTimingPointAtEdgeAndMixesLayers()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(
                directory, "soft-hitnormal2.wav");

            string clapPath = Path.Combine(
                directory, "soft-hitclap2.wav");

            WriteHalfAmplitudeWav(normalPath);
            WriteHalfAmplitudeWav(clapPath);

            var (beatmap, slider) = CreateTwoSpanSlider();

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlaySliderEdge(
                    beatmap, slider, 1, directory, resolver, engine);

            Assert.Equal(2, results.Count);
            Assert.Equal(normalPath, results[0].ResolvedPath);
            Assert.Equal(clapPath, results[1].ResolvedPath);

            Assert.All(results, result =>
                Assert.Equal(
                    SampleResolutionOutcome.BeatmapSampleFound,
                    result.Outcome));

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // Two sources: 0.5 * 0.25 + 0.5 * 0.25 = 0.25
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderEdge_Tail_UsesExplicitEdgeSampleSet()
    {
        string directory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(
                directory, "normal-hitnormal2.wav");

            WriteHalfAmplitudeWav(path);

            var (beatmap, slider) = CreateTwoSpanSlider();

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlaySliderEdge(
                    beatmap, slider, 2, directory, resolver, engine);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);

            Assert.Equal(path, result.ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // SampleSet comes from the edge; volume comes from timing.
            Assert.InRange(output[0], 0.115f, 0.135f);
            Assert.InRange(output[1], 0.115f, 0.135f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderEdge_WhenClapIsMissing_PlaysNormalAndReturnsFallback()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(
                directory, "soft-hitnormal2.wav");

            WriteHalfAmplitudeWav(normalPath);

            var (beatmap, slider) = CreateTwoSpanSlider();

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlaySliderEdge(
                    beatmap, slider, 1, directory, resolver, engine);

            Assert.Equal(2, results.Count);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                results[0].Outcome);
            Assert.Equal(normalPath, results[0].ResolvedPath);

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                results[1].Outcome);
            Assert.Null(results[1].ResolvedPath);
            Assert.Equal(
                "soft-hitclap.wav",
                results[1].FallbackFilename);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // Only normal: 0.5 * 0.25 = 0.125
            Assert.InRange(output[0], 0.115f, 0.135f);
            Assert.InRange(output[1], 0.115f, 0.135f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderEdge_WhenSampleIndexIsZero_DoesNotUseBeatmapFile()
    {
        string directory = CreateTempDirectory();

        try
        {
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "normal-hitnormal.wav"));

            var (beatmap, slider) = CreateTwoSpanSlider();

            beatmap.TimingPoints[0].SampleIndex = 0;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = new AudioPlaybackEngine();

            List<SampleResolutionResult> results =
                service.PlaySliderEdge(
                    beatmap, slider, 0, directory, resolver, engine);

            SampleResolutionResult result = Assert.Single(results);

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
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderEdge_IgnoresTrailingCustomFilenameAndVolume()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(
                directory, "normal-hitnormal.wav");

            WriteHalfAmplitudeWav(normalPath);
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "custom.wav"));

            var (beatmap, slider) = CreateTwoSpanSlider();

            slider.HitSample = new HitSample
            {
                Filename = "custom.wav",
                Index = 9,
                Volume = 100
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlaySliderEdge(
                    beatmap, slider, 0, directory, resolver, engine);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);
            Assert.Equal(normalPath, result.ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // Timing-point volume is 50%, not trailing volume 100%.
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderEdge_WhenBankedSampleIsMissing_UsesUniversalSample()
    {
        string directory = CreateTempDirectory();

        try
        {
            string universalPath = Path.Combine(
                directory, "hitnormal.wav");

            WriteHalfAmplitudeWav(universalPath);

            var (beatmap, slider) = CreateTwoSpanSlider();

            beatmap.TimingPoints[0].SampleIndex = 2;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlaySliderEdge(
                    beatmap, slider, 0, directory, resolver, engine);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);
            Assert.Equal(universalPath, result.ResolvedPath);
            Assert.Equal(
                "normal-hitnormal.wav",
                result.FallbackFilename);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderEdge_WhenEdgeCountIsInvalid_ThrowsInvalidOperationException()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();

        slider.SliderEdges.RemoveAt(2);

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                service.PlaySliderEdge(
                    beatmap, slider, 1, "beatmap", resolver, engine));

        Assert.Equal(
            "It is not possible to resolve slider edge hitsounds because the slider has 2 edges but requires 3.",
            exception.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void PlaySliderEdge_WhenObjectIsNotSlider_ThrowsArgumentException(
        int objectType)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();

        slider.ObjectType = objectType;

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlaySliderEdge(
                    beatmap, slider, 0, "beatmap", resolver, engine));

        Assert.Equal("slider", exception.ParamName);
        Assert.Contains(
            "The hit object must be a slider.",
            exception.Message);
    }

    [Fact]
    public void PlaySliderEdge_WhenBeatmapIsNull_ThrowsArgumentNullException()
    {
        var (_, slider) = CreateTwoSpanSlider();

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlaySliderEdge(
                    null!, slider, 0, "beatmap", resolver, engine));

        Assert.Equal("beatmap", exception.ParamName);
    }

    [Fact]
    public void PlaySliderEdge_WhenSliderIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlaySliderEdge(
                    beatmap, null!, 0, "beatmap", resolver, engine));

        Assert.Equal("slider", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void PlaySliderEdge_WhenEdgeIndexIsInvalid_ThrowsArgumentOutOfRangeException(
        int edgeIndex)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlaySliderEdge(
                    beatmap, slider, edgeIndex, "beatmap", resolver, engine));

        Assert.Equal("edgeIndex", exception.ParamName);
        Assert.Equal(edgeIndex, exception.ActualValue);
        Assert.Contains(
            "The slider edge index must be between 0 and 2.",
            exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlaySliderEdge_WhenDirectoryIsInvalid_ThrowsArgumentException(
        string? directory)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlaySliderEdge(
                    beatmap, slider, 0, directory!, resolver, engine));

        Assert.Equal("beatmapDirectory", exception.ParamName);
        Assert.Contains(
            "The beatmap directory must not be empty.",
            exception.Message);
    }

    [Fact]
    public void PlaySliderEdge_WhenResolverIsNull_ThrowsArgumentNullException()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();

        var service = new HitSoundPlaybackService();

        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlaySliderEdge(
                    beatmap, slider, 0, "beatmap", null!, engine));

        Assert.Equal("resolver", exception.ParamName);
    }

    [Fact]
    public void PlaySliderEdge_WhenEngineIsNull_ThrowsArgumentNullException()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlaySliderEdge(
                    beatmap, slider, 0, "beatmap", resolver, null!));

        Assert.Equal("engine", exception.ParamName);
    }

    // Shared slider test fixture
    private static (Beatmap Beatmap, HitObject Slider) CreateTwoSpanSlider()
    {
        var beatmap = new Beatmap
        {
            SliderMultiplier = 1.4
        };

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 0,
            BeatLength = 500,
            IsUninherited = true,
            SampleSet = 1,
            SampleIndex = 1,
            Volume = 50
        });

        beatmap.TimingPoints.Add(new TimingPoint
        {
            Time = 1500,
            BeatLength = -100,
            IsUninherited = false,
            SampleSet = 2,
            SampleIndex = 2,
            Volume = 25
        });

        var slider = new HitObject
        {
            ObjectType = 2,
            Time = 1000,
            Length = 140,
            Slides = 2
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

        slider.SliderEdges.Add(new SliderEdge
        {
            HitSound = 0,
            NormalSet = 1,
            AdditionSet = 0
        });

        return (beatmap, slider);
    }

    [Fact]
    public void PlaySliderTick_WhenSampleExists_PlaysAtStartTimingPointVolume()
    {
        string directory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(
                directory, "normal-slidertick.wav");

            WriteHalfAmplitudeWav(path);

            var (beatmap, slider) = CreateTwoSpanSlider();
            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlaySliderTick(
                    beatmap, slider, directory, resolver, engine);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);

            Assert.Equal(path, result.ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // Amplitude 0.5 * volume 50% = 0.25
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderTick_WhenSliderHasWhistle_DoesNotPlayBodySounds()
    {
        string directory = CreateTempDirectory();

        try
        {
            string tickPath = Path.Combine(
                directory, "normal-slidertick.wav");

            WriteHalfAmplitudeWav(tickPath);
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "normal-sliderslide.wav"));
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "normal-sliderwhistle.wav"));

            var (beatmap, slider) = CreateTwoSpanSlider();
            slider.HitSound = 2;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlaySliderTick(
                    beatmap, slider, directory, resolver, engine);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);

            Assert.Equal(tickPath, result.ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // Only the tick is played.
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderTick_IgnoresTrailingIndexVolumeAndCustomFilename()
    {
        string directory = CreateTempDirectory();

        try
        {
            string tickPath = Path.Combine(
                directory, "normal-slidertick.wav");

            WriteHalfAmplitudeWav(tickPath);
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "custom.wav"));

            var (beatmap, slider) = CreateTwoSpanSlider();

            slider.HitSample = new HitSample
            {
                Index = 9,
                Volume = 100,
                Filename = "custom.wav"
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlaySliderTick(
                    beatmap, slider, directory, resolver, engine);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);

            Assert.Equal(tickPath, result.ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // Timing-point volume 50%, not HitSample.Volume 100%.
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderTick_WhenNormalSetIsExplicit_UsesSpecifiedSampleSet()
    {
        string directory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(
                directory, "drum-slidertick.wav");

            WriteHalfAmplitudeWav(path);

            var (beatmap, slider) = CreateTwoSpanSlider();

            slider.HitSample.NormalSet = 3;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlaySliderTick(
                    beatmap, slider, directory, resolver, engine);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);

            Assert.Equal(path, result.ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderTick_WhenSliderStartsAfterTimingChange_UsesNewTimingPoint()
    {
        string directory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(
                directory, "soft-slidertick2.wav");

            WriteHalfAmplitudeWav(path);

            var (beatmap, slider) = CreateTwoSpanSlider();

            // The fixture has a timing change at 1500 ms.
            slider.Time = 1800;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlaySliderTick(
                    beatmap, slider, directory, resolver, engine);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);

            Assert.Equal(path, result.ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // New timing point: volume 25%.
            // Amplitude 0.5 * 0.25 = 0.125
            Assert.InRange(output[0], 0.115f, 0.135f);
            Assert.InRange(output[1], 0.115f, 0.135f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderTick_WhenSampleIsMissing_ReturnsExternalFallback()
    {
        string directory = CreateTempDirectory();

        try
        {
            var (beatmap, slider) = CreateTwoSpanSlider();
            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = new AudioPlaybackEngine();

            List<SampleResolutionResult> results =
                service.PlaySliderTick(
                    beatmap, slider, directory, resolver, engine);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                result.Outcome);

            Assert.Null(result.ResolvedPath);

            Assert.Equal(
                "normal-slidertick.wav",
                result.FallbackFilename);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderTick_WhenSampleIndexIsZero_IgnoresExistingBeatmapSample()
    {
        string directory = CreateTempDirectory();

        try
        {
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "normal-slidertick.wav"));

            var (beatmap, slider) = CreateTwoSpanSlider();

            beatmap.TimingPoints[0].SampleIndex = 0;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = new AudioPlaybackEngine();

            List<SampleResolutionResult> results =
                service.PlaySliderTick(
                    beatmap, slider, directory, resolver, engine);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                result.Outcome);

            Assert.Null(result.ResolvedPath);
            Assert.Equal(
                "normal-slidertick.wav",
                result.FallbackFilename);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderTick_WhenBankedSampleIsMissing_UsesUniversalSample()
    {
        string directory = CreateTempDirectory();

        try
        {
            string universalPath = Path.Combine(
                directory, "slidertick.wav");

            WriteHalfAmplitudeWav(universalPath);

            var (beatmap, slider) = CreateTwoSpanSlider();

            beatmap.TimingPoints[0].SampleIndex = 2;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlaySliderTick(
                    beatmap, slider, directory, resolver, engine);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);

            Assert.Equal(universalPath, result.ResolvedPath);

            Assert.Equal(
                "normal-slidertick.wav",
                result.FallbackFilename);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void PlaySliderTick_WhenObjectIsNotSlider_ThrowsArgumentException(
        int objectType)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        slider.ObjectType = objectType;

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlaySliderTick(
                    beatmap, slider, "beatmap", resolver, engine));

        Assert.Equal("slider", exception.ParamName);
        Assert.Contains(
            "The hit object must be a slider.",
            exception.Message);
    }

    [Fact]
    public void PlaySliderTick_WhenBeatmapIsNull_ThrowsArgumentNullException()
    {
        var (_, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlaySliderTick(
                    null!, slider, "beatmap", resolver, engine));

        Assert.Equal("beatmap", exception.ParamName);
    }

    [Fact]
    public void PlaySliderTick_WhenSliderIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlaySliderTick(
                    beatmap, null!, "beatmap", resolver, engine));

        Assert.Equal("slider", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlaySliderTick_WhenDirectoryIsInvalid_ThrowsArgumentException(
        string? directory)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlaySliderTick(
                    beatmap, slider, directory!, resolver, engine));

        Assert.Equal("beatmapDirectory", exception.ParamName);
        Assert.Contains(
            "The beatmap directory must not be empty.",
            exception.Message);
    }

    [Fact]
    public void PlaySliderTick_WhenResolverIsNull_ThrowsArgumentNullException()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlaySliderTick(
                    beatmap, slider, "beatmap", null!, engine));

        Assert.Equal("resolver", exception.ParamName);
    }

    [Fact]
    public void PlaySliderTick_WhenEngineIsNull_ThrowsArgumentNullException()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlaySliderTick(
                    beatmap, slider, "beatmap", resolver, null!));

        Assert.Equal("engine", exception.ParamName);
    }

    [Fact]
    public void PlaySliderTick_WhenSampleExistsButOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        string directory = CreateTempDirectory();

        try
        {
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "normal-slidertick.wav"));

            var (beatmap, slider) = CreateTwoSpanSlider();
            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = new AudioPlaybackEngine();

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() =>
                    service.PlaySliderTick(
                        beatmap, slider, directory, resolver, engine));

            Assert.Equal(
                "The audio output has not been initialized.",
                exception.Message);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderTicksInTimeRange_WhenRangeContainsTicks_PlaysEachAndReturnsTimes()
    {
        string directory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(
                directory, "normal-slidertick.wav");

            WriteHalfAmplitudeWav(path);

            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.BeatmapVersion = 14;
            beatmap.SliderTickRate = 4;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            var results = service.PlaySliderTicksInTimeRange(
                beatmap, slider, 1200, 1400,
                directory, resolver, engine);

            Assert.Equal(2, results.Count);
            Assert.Equal(1250, results[0].TickTime);
            Assert.Equal(1375, results[1].TickTime);

            foreach (var item in results)
            {
                SampleResolutionResult result =
                    Assert.Single(item.Results);

                Assert.Equal(
                    SampleResolutionOutcome.BeatmapSampleFound,
                    result.Outcome);

                Assert.Equal(path, result.ResolvedPath);
            }

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // Two ticks: 2 * (0.5 * 0.5) = 0.5
            Assert.InRange(output[0], 0.49f, 0.51f);
            Assert.InRange(output[1], 0.49f, 0.51f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderTicksInTimeRange_WhenTickIsOnReverseSpan_UsesSliderStartSample()
    {
        string directory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(
                directory, "normal-slidertick.wav");

            WriteHalfAmplitudeWav(path);

            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.BeatmapVersion = 14;
            beatmap.SliderTickRate = 4;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            var results = service.PlaySliderTicksInTimeRange(
                beatmap, slider, 1600, 1700,
                directory, resolver, engine);

            var item = Assert.Single(results);
            Assert.Equal(1625, item.TickTime);

            SampleResolutionResult result =
                Assert.Single(item.Results);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);

            Assert.Equal(path, result.ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // Slider start volume: 50%.
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderTicksInTimeRange_AdjacentIntervals_DoNotRepeatBoundaryTick()
    {
        string directory = CreateTempDirectory();

        try
        {
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "normal-slidertick.wav"));

            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.BeatmapVersion = 14;
            beatmap.SliderTickRate = 4;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            var first = service.PlaySliderTicksInTimeRange(
                beatmap, slider, 1000, 1250,
                directory, resolver, engine);

            var second = service.PlaySliderTicksInTimeRange(
                beatmap, slider, 1250, 1375,
                directory, resolver, engine);

            Assert.Equal(
                new double[] { 1125, 1250 },
                first.Select(item => item.TickTime));

            Assert.Equal(
                new double[] { 1375 },
                second.Select(item => item.TickTime));

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // Three occurrences: 3 * 0.25 = 0.75
            Assert.InRange(output[0], 0.74f, 0.76f);
            Assert.InRange(output[1], 0.74f, 0.76f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderTicksInTimeRange_WhenNoTicksMatch_ReturnsEmpty()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        beatmap.BeatmapVersion = 14;
        beatmap.SliderTickRate = 4;

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        var results = service.PlaySliderTicksInTimeRange(
            beatmap, slider, 0, 1000,
            "beatmap", resolver, engine);

        Assert.Empty(results);
    }

    [Fact]
    public void PlaySliderTicksInTimeRange_WhenSampleIsMissing_ReturnsFallbackForEveryTick()
    {
        string directory = CreateTempDirectory();

        try
        {
            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.BeatmapVersion = 14;
            beatmap.SliderTickRate = 4;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = new AudioPlaybackEngine();

            var results = service.PlaySliderTicksInTimeRange(
                beatmap, slider, 1200, 1400,
                directory, resolver, engine);

            Assert.Equal(2, results.Count);
            Assert.Equal(1250, results[0].TickTime);
            Assert.Equal(1375, results[1].TickTime);

            foreach (var item in results)
            {
                SampleResolutionResult result =
                    Assert.Single(item.Results);

                Assert.Equal(
                    SampleResolutionOutcome.ExternalFallbackRequired,
                    result.Outcome);

                Assert.Null(result.ResolvedPath);

                Assert.Equal(
                    "normal-slidertick.wav",
                    result.FallbackFilename);
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderTicksInTimeRange_WhenIndexedSampleExists_ReusesSampleForEachTick()
    {
        string directory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(
                directory, "normal-slidertick2.wav");

            WriteHalfAmplitudeWav(path);

            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.BeatmapVersion = 14;
            beatmap.SliderTickRate = 4;
            beatmap.TimingPoints[0].SampleIndex = 2;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            var results = service.PlaySliderTicksInTimeRange(
                beatmap, slider, 1200, 1400,
                directory, resolver, engine);

            Assert.Equal(2, results.Count);

            foreach (var item in results)
            {
                SampleResolutionResult result =
                    Assert.Single(item.Results);

                Assert.Equal(
                    SampleResolutionOutcome.BeatmapSampleFound,
                    result.Outcome);

                Assert.Equal(path, result.ResolvedPath);
            }

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            Assert.InRange(output[0], 0.49f, 0.51f);
            Assert.InRange(output[1], 0.49f, 0.51f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderTicksInTimeRange_WhenOutputIsNotInitialized_PropagatesException()
    {
        string directory = CreateTempDirectory();

        try
        {
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "normal-slidertick.wav"));

            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.BeatmapVersion = 14;
            beatmap.SliderTickRate = 4;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = new AudioPlaybackEngine();

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() =>
                    service.PlaySliderTicksInTimeRange(
                        beatmap, slider, 1200, 1400,
                        directory, resolver, engine));

            Assert.Equal(
                "The audio output has not been initialized.",
                exception.Message);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void PlaySliderTicksInTimeRange_WhenObjectIsNotSlider_ThrowsArgumentException(
        int objectType)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        slider.ObjectType = objectType;

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlaySliderTicksInTimeRange(
                    beatmap, slider, 1000, 1400,
                    "beatmap", resolver, engine));

        Assert.Equal("slider", exception.ParamName);

        Assert.Contains(
            "The hit object must be a slider.",
            exception.Message);
    }

    [Fact]
    public void PlaySliderTicksInTimeRange_WhenBeatmapIsNull_ThrowsArgumentNullException()
    {
        var (_, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlaySliderTicksInTimeRange(
                    null!, slider, 0, 2000,
                    "beatmap", resolver, engine));

        Assert.Equal("beatmap", exception.ParamName);
    }

    [Fact]
    public void PlaySliderTicksInTimeRange_WhenSliderIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlaySliderTicksInTimeRange(
                    beatmap, null!, 0, 2000,
                    "beatmap", resolver, engine));

        Assert.Equal("slider", exception.ParamName);
    }

    [Fact]
    public void PlaySliderTicksInTimeRange_WhenResolverIsNull_ThrowsArgumentNullException()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();

        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlaySliderTicksInTimeRange(
                    beatmap, slider, 0, 2000,
                    "beatmap", null!, engine));

        Assert.Equal("resolver", exception.ParamName);
    }

    [Fact]
    public void PlaySliderTicksInTimeRange_WhenEngineIsNull_ThrowsArgumentNullException()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlaySliderTicksInTimeRange(
                    beatmap, slider, 0, 2000,
                    "beatmap", resolver, null!));

        Assert.Equal("engine", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlaySliderTicksInTimeRange_WhenDirectoryIsInvalid_ThrowsArgumentException(
        string? directory)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlaySliderTicksInTimeRange(
                    beatmap, slider, 0, 2000,
                    directory!, resolver, engine));

        Assert.Equal(
            "beatmapDirectory",
            exception.ParamName);

        Assert.Contains(
            "The beatmap directory must not be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PlaySliderTicksInTimeRange_WhenStartIsNotFinite_ThrowsArgumentOutOfRangeException(
        double invalidStart)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlaySliderTicksInTimeRange(
                    beatmap, slider, invalidStart, 2000,
                    "beatmap", resolver, engine));

        Assert.Equal(
            "startExclusiveMilliseconds",
            exception.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PlaySliderTicksInTimeRange_WhenEndIsNotFinite_ThrowsArgumentOutOfRangeException(
        double invalidEnd)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlaySliderTicksInTimeRange(
                    beatmap, slider, 1000, invalidEnd,
                    "beatmap", resolver, engine));

        Assert.Equal(
            "endInclusiveMilliseconds",
            exception.ParamName);
    }

    [Fact]
    public void PlaySliderTicksInTimeRange_WhenEndPrecedesStart_ThrowsArgumentOutOfRangeException()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlaySliderTicksInTimeRange(
                    beatmap, slider, 2000, 1000,
                    "beatmap", resolver, engine));

        Assert.Equal(
            "endInclusiveMilliseconds",
            exception.ParamName);
    }

    [Fact]
    public void PlaySliderTicksInTimeRange_WhenRangeHasEqualBounds_ReturnsEmpty()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        beatmap.BeatmapVersion = 14;
        beatmap.SliderTickRate = 4;

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        var results = service.PlaySliderTicksInTimeRange(
            beatmap, slider, 1250, 1250,
            "beatmap", resolver, engine);

        Assert.Empty(results);
    }

    [Fact]
    public void PlaySliderTicksInTimeRange_WhenSliderHasNoSpans_PropagatesException()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        slider.Slides = 0;

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                service.PlaySliderTicksInTimeRange(
                    beatmap, slider, 0, 2000,
                    "beatmap", resolver, engine));

        Assert.Equal(
            "It is not possible to calculate slider tick times because the slider must have at least one span.",
            exception.Message);
    }

    [Fact]
    public void PlaySliderEdgesInTimeRange_WhenAllEdgesMatch_PlaysEveryEdgeInOrder()
    {
        string directory = CreateTempDirectory();

        try
        {
            string headPath = Path.Combine(
                directory, "normal-hitnormal.wav");
            string repeatNormalPath = Path.Combine(
                directory, "soft-hitnormal2.wav");
            string repeatClapPath = Path.Combine(
                directory, "soft-hitclap2.wav");
            string tailPath = Path.Combine(
                directory, "normal-hitnormal2.wav");

            WriteHalfAmplitudeWav(headPath);
            WriteHalfAmplitudeWav(repeatNormalPath);
            WriteHalfAmplitudeWav(repeatClapPath);
            WriteHalfAmplitudeWav(tailPath);

            var (beatmap, slider) = CreateTwoSpanSlider();
            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            var results = service.PlaySliderEdgesInTimeRange(
                beatmap, slider, 999, 2000,
                directory, resolver, engine);

            Assert.Equal(3, results.Count);

            Assert.Equal(0, results[0].EdgeIndex);
            Assert.Equal(1000.0, results[0].EdgeTime);

            Assert.Equal(1, results[1].EdgeIndex);
            Assert.Equal(1500.0, results[1].EdgeTime);

            Assert.Equal(2, results[2].EdgeIndex);
            Assert.Equal(2000.0, results[2].EdgeTime);

            Assert.Equal(
                headPath,
                Assert.Single(results[0].Results).ResolvedPath);

            Assert.Equal(2, results[1].Results.Count);
            Assert.Equal(
                repeatNormalPath,
                results[1].Results[0].ResolvedPath);
            Assert.Equal(
                repeatClapPath,
                results[1].Results[1].ResolvedPath);

            Assert.Equal(
                tailPath,
                Assert.Single(results[2].Results).ResolvedPath);

            Assert.All(
                results.SelectMany(item => item.Results),
                result => Assert.Equal(
                    SampleResolutionOutcome.BeatmapSampleFound,
                    result.Outcome));

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // Head: 0.25
            // Repeat normal + clap: 0.125 + 0.125
            // Tail: 0.125
            // Total: 0.625
            Assert.InRange(output[0], 0.615f, 0.635f);
            Assert.InRange(output[1], 0.615f, 0.635f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderEdgesInTimeRange_WhenRangeContainsRepeatAndTail_SkipsHead()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(
                directory, "soft-hitnormal2.wav");
            string clapPath = Path.Combine(
                directory, "soft-hitclap2.wav");
            string tailPath = Path.Combine(
                directory, "normal-hitnormal2.wav");

            WriteHalfAmplitudeWav(normalPath);
            WriteHalfAmplitudeWav(clapPath);
            WriteHalfAmplitudeWav(tailPath);

            var (beatmap, slider) = CreateTwoSpanSlider();
            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            var results = service.PlaySliderEdgesInTimeRange(
                beatmap, slider, 1200, 2000,
                directory, resolver, engine);

            Assert.Equal(2, results.Count);

            Assert.Equal(1, results[0].EdgeIndex);
            Assert.Equal(1500.0, results[0].EdgeTime);

            Assert.Equal(2, results[1].EdgeIndex);
            Assert.Equal(2000.0, results[1].EdgeTime);

            Assert.Equal(2, results[0].Results.Count);
            Assert.Single(results[1].Results);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // Repeat: 0.25; tail: 0.125
            Assert.InRange(output[0], 0.365f, 0.385f);
            Assert.InRange(output[1], 0.365f, 0.385f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderEdgesInTimeRange_AdjacentIntervals_DoNotRepeatEdges()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        var first = service.PlaySliderEdgesInTimeRange(
            beatmap, slider, 999, 1000,
            "beatmap", resolver, engine);

        var second = service.PlaySliderEdgesInTimeRange(
            beatmap, slider, 1000, 1500,
            "beatmap", resolver, engine);

        var third = service.PlaySliderEdgesInTimeRange(
            beatmap, slider, 1500, 2000,
            "beatmap", resolver, engine);

        Assert.Equal(0, Assert.Single(first).EdgeIndex);
        Assert.Equal(1, Assert.Single(second).EdgeIndex);
        Assert.Equal(2, Assert.Single(third).EdgeIndex);

        Assert.Equal(1000.0, first[0].EdgeTime);
        Assert.Equal(1500.0, second[0].EdgeTime);
        Assert.Equal(2000.0, third[0].EdgeTime);
    }

    [Fact]
    public void PlaySliderEdgesInTimeRange_WhenNoEdgesMatch_ReturnsEmpty()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        var results = service.PlaySliderEdgesInTimeRange(
            beatmap, slider, 0, 999,
            "beatmap", resolver, engine);

        Assert.Empty(results);
    }

    [Fact]
    public void PlaySliderEdgesInTimeRange_WhenClapIsMissing_ContinuesWithOtherLayers()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(
                directory, "soft-hitnormal2.wav");

            WriteHalfAmplitudeWav(normalPath);

            var (beatmap, slider) = CreateTwoSpanSlider();
            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            var results = service.PlaySliderEdgesInTimeRange(
                beatmap, slider, 1000, 1500,
                directory, resolver, engine);

            var repeat = Assert.Single(results);

            Assert.Equal(1, repeat.EdgeIndex);
            Assert.Equal(1500.0, repeat.EdgeTime);
            Assert.Equal(2, repeat.Results.Count);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                repeat.Results[0].Outcome);
            Assert.Equal(
                normalPath,
                repeat.Results[0].ResolvedPath);

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                repeat.Results[1].Outcome);
            Assert.Null(repeat.Results[1].ResolvedPath);
            Assert.Equal(
                "soft-hitclap.wav",
                repeat.Results[1].FallbackFilename);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            Assert.InRange(output[0], 0.115f, 0.135f);
            Assert.InRange(output[1], 0.115f, 0.135f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderEdgesInTimeRange_WhenAllSamplesAreMissing_ReturnsFallbackForEachLayer()
    {
        string directory = CreateTempDirectory();

        try
        {
            var (beatmap, slider) = CreateTwoSpanSlider();
            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = new AudioPlaybackEngine();

            var results = service.PlaySliderEdgesInTimeRange(
                beatmap, slider, 999, 2000,
                directory, resolver, engine);

            Assert.Equal(3, results.Count);
            Assert.Single(results[0].Results);
            Assert.Equal(2, results[1].Results.Count);
            Assert.Single(results[2].Results);

            Assert.All(
                results.SelectMany(item => item.Results),
                result =>
                {
                    Assert.Equal(
                        SampleResolutionOutcome.ExternalFallbackRequired,
                        result.Outcome);

                    Assert.Null(result.ResolvedPath);
                });

            Assert.Equal(
                "normal-hitnormal.wav",
                results[0].Results[0].FallbackFilename);

            Assert.Equal(
                "soft-hitnormal.wav",
                results[1].Results[0].FallbackFilename);

            Assert.Equal(
                "soft-hitclap.wav",
                results[1].Results[1].FallbackFilename);

            Assert.Equal(
                "normal-hitnormal.wav",
                results[2].Results[0].FallbackFilename);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderEdgesInTimeRange_WhenOutputIsNotInitialized_PropagatesException()
    {
        string directory = CreateTempDirectory();

        try
        {
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "normal-hitnormal.wav"));

            var (beatmap, slider) = CreateTwoSpanSlider();
            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = new AudioPlaybackEngine();

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() =>
                    service.PlaySliderEdgesInTimeRange(
                        beatmap, slider, 999, 1000,
                        directory, resolver, engine));

            Assert.Equal(
                "The audio output has not been initialized.",
                exception.Message);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderEdgesInTimeRange_WhenLaterEdgeFails_EarlierAudioRemains()
    {
        string directory = CreateTempDirectory();

        try
        {
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "normal-hitnormal.wav"));

            WriteHalfAmplitudeWav(
                Path.Combine(directory, "soft-hitnormal2.wav"));

            var (beatmap, slider) = CreateTwoSpanSlider();

            beatmap.TimingPoints[1].Volume = 101;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            ArgumentOutOfRangeException exception =
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    service.PlaySliderEdgesInTimeRange(
                        beatmap, slider, 999, 2000,
                        directory, resolver, engine));

            Assert.Equal("volume", exception.ParamName);
            Assert.Equal(101, exception.ActualValue);

            // The head was added before the repeat failed.
            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderEdgesInTimeRange_WhenEdgeCountIsInvalid_PropagatesException()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();

        slider.SliderEdges.RemoveAt(2);

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                service.PlaySliderEdgesInTimeRange(
                    beatmap, slider, 999, 1000,
                    "beatmap", resolver, engine));

        Assert.Equal(
            "It is not possible to resolve slider edge hitsounds because the slider has 2 edges but requires 3.",
            exception.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void PlaySliderEdgesInTimeRange_WhenObjectIsNotSlider_ThrowsArgumentException(
        int objectType)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();

        slider.ObjectType = objectType;

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlaySliderEdgesInTimeRange(
                    beatmap, slider, 0, 2000,
                    "beatmap", resolver, engine));

        Assert.Equal("slider", exception.ParamName);

        Assert.Contains(
            "The hit object must be a slider.",
            exception.Message);
    }

    [Fact]
    public void PlaySliderEdgesInTimeRange_WhenBeatmapIsNull_ThrowsArgumentNullException()
    {
        var (_, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlaySliderEdgesInTimeRange(
                    null!, slider, 0, 2000,
                    "beatmap", resolver, engine));

        Assert.Equal("beatmap", exception.ParamName);
    }

    [Fact]
    public void PlaySliderEdgesInTimeRange_WhenSliderIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlaySliderEdgesInTimeRange(
                    beatmap, null!, 0, 2000,
                    "beatmap", resolver, engine));

        Assert.Equal("slider", exception.ParamName);
    }

    [Fact]
    public void PlaySliderEdgesInTimeRange_WhenResolverIsNull_ThrowsArgumentNullException()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();

        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlaySliderEdgesInTimeRange(
                    beatmap, slider, 0, 2000,
                    "beatmap", null!, engine));

        Assert.Equal("resolver", exception.ParamName);
    }

    [Fact]
    public void PlaySliderEdgesInTimeRange_WhenEngineIsNull_ThrowsArgumentNullException()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlaySliderEdgesInTimeRange(
                    beatmap, slider, 0, 2000,
                    "beatmap", resolver, null!));

        Assert.Equal("engine", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlaySliderEdgesInTimeRange_WhenDirectoryIsInvalid_ThrowsArgumentException(
        string? directory)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlaySliderEdgesInTimeRange(
                    beatmap, slider, 0, 2000,
                    directory!, resolver, engine));

        Assert.Equal("beatmapDirectory", exception.ParamName);

        Assert.Contains(
            "The beatmap directory must not be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PlaySliderEdgesInTimeRange_WhenStartIsNotFinite_ThrowsArgumentOutOfRangeException(
        double invalidStart)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlaySliderEdgesInTimeRange(
                    beatmap, slider, invalidStart, 2000,
                    "beatmap", resolver, engine));

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
    public void PlaySliderEdgesInTimeRange_WhenEndIsNotFinite_ThrowsArgumentOutOfRangeException(
        double invalidEnd)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlaySliderEdgesInTimeRange(
                    beatmap, slider, 0, invalidEnd,
                    "beatmap", resolver, engine));

        Assert.Equal(
            "endInclusiveMilliseconds",
            exception.ParamName);

        Assert.Contains(
            "The end time must be finite.",
            exception.Message);
    }

    [Fact]
    public void PlaySliderEdgesInTimeRange_WhenEndPrecedesStart_ThrowsArgumentOutOfRangeException()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlaySliderEdgesInTimeRange(
                    beatmap, slider, 2000, 1000,
                    "beatmap", resolver, engine));

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
    public void PlaySliderEdgesInTimeRange_WhenSlidesIsInvalid_ThrowsInvalidOperationException(
        int slides)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();

        slider.Slides = slides;

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                service.PlaySliderEdgesInTimeRange(
                    beatmap, slider, 0, 2000,
                    "beatmap", resolver, engine));

        Assert.Equal(
            "It is not possible to calculate a slider edge time because the slider must have at least one span.",
            exception.Message);
    }

    [Fact]
    public void PlayTimedHitSoundEvent_HitCircle_PlaysCustomSample()
    {
        string directory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(directory, "custom.wav");
            WriteHalfAmplitudeWav(path);

            HitObject circle = new()
            {
                ObjectType = 1,
                Time = 1000,
                HitSample = new HitSample
                {
                    Filename = "custom.wav",
                    Volume = 50
                }
            };

            TimedHitSoundEvent soundEvent = new()
            {
                Time = 1000,
                HitObject = circle,
                EventType = HitSoundEventType.HitCircle
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlayTimedHitSoundEvent(
                    soundEvent, new Beatmap(),
                    directory, resolver, engine);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.CustomSampleFound,
                result.Outcome);
            Assert.Equal(path, result.ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayTimedHitSoundEvent_HitCircle_WhenCustomSampleIsMissing_ReturnsMissing()
    {
        string directory = CreateTempDirectory();

        try
        {
            HitObject circle = new()
            {
                ObjectType = 1,
                Time = 1000,
                HitSample = new HitSample
                {
                    Filename = "missing.wav"
                }
            };

            TimedHitSoundEvent soundEvent = new()
            {
                Time = 1000,
                HitObject = circle,
                EventType = HitSoundEventType.HitCircle
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = new AudioPlaybackEngine();

            SampleResolutionResult result = Assert.Single(
                service.PlayTimedHitSoundEvent(
                    soundEvent, new Beatmap(),
                    directory, resolver, engine));

            Assert.Equal(
                SampleResolutionOutcome.CustomSampleMissing,
                result.Outcome);
            Assert.Null(result.ResolvedPath);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayTimedHitSoundEvent_SliderEdge_Head_PlaysCorrectSample()
    {
        string directory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(
                directory, "normal-hitnormal.wav");

            WriteHalfAmplitudeWav(path);

            var (beatmap, slider) = CreateTwoSpanSlider();

            TimedHitSoundEvent soundEvent = new()
            {
                Time = 1000,
                HitObject = slider,
                EventType = HitSoundEventType.SliderEdge,
                EdgeIndex = 0
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            SampleResolutionResult result = Assert.Single(
                service.PlayTimedHitSoundEvent(
                    soundEvent, beatmap,
                    directory, resolver, engine));

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);
            Assert.Equal(path, result.ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayTimedHitSoundEvent_SliderEdge_Repeat_UsesEdgeIndex()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(
                directory, "soft-hitnormal2.wav");
            string clapPath = Path.Combine(
                directory, "soft-hitclap2.wav");

            WriteHalfAmplitudeWav(normalPath);
            WriteHalfAmplitudeWav(clapPath);

            var (beatmap, slider) = CreateTwoSpanSlider();

            TimedHitSoundEvent soundEvent = new()
            {
                Time = 1500,
                HitObject = slider,
                EventType = HitSoundEventType.SliderEdge,
                EdgeIndex = 1
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlayTimedHitSoundEvent(
                    soundEvent, beatmap,
                    directory, resolver, engine);

            Assert.Equal(2, results.Count);

            Assert.Equal(normalPath, results[0].ResolvedPath);
            Assert.Equal(clapPath, results[1].ResolvedPath);

            Assert.All(results, result =>
                Assert.Equal(
                    SampleResolutionOutcome.BeatmapSampleFound,
                    result.Outcome));

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // Two layers at 25% volume each.
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayTimedHitSoundEvent_SliderTick_DoesNotPlaySliderBody()
    {
        string directory = CreateTempDirectory();

        try
        {
            string tickPath = Path.Combine(
                directory, "normal-slidertick.wav");

            WriteHalfAmplitudeWav(tickPath);
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "normal-sliderslide.wav"));

            var (beatmap, slider) = CreateTwoSpanSlider();
            slider.HitSound = 2;

            TimedHitSoundEvent soundEvent = new()
            {
                Time = 1250,
                HitObject = slider,
                EventType = HitSoundEventType.SliderTick
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            SampleResolutionResult result = Assert.Single(
                service.PlayTimedHitSoundEvent(
                    soundEvent, beatmap,
                    directory, resolver, engine));

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);
            Assert.Equal(tickPath, result.ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayTimedHitSoundEvent_SliderTick_WhenSampleIsMissing_ReturnsFallback()
    {
        string directory = CreateTempDirectory();

        try
        {
            var (beatmap, slider) = CreateTwoSpanSlider();

            TimedHitSoundEvent soundEvent = new()
            {
                Time = 1250,
                HitObject = slider,
                EventType = HitSoundEventType.SliderTick
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = new AudioPlaybackEngine();

            SampleResolutionResult result = Assert.Single(
                service.PlayTimedHitSoundEvent(
                    soundEvent, beatmap,
                    directory, resolver, engine));

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                result.Outcome);
            Assert.Null(result.ResolvedPath);
            Assert.Equal(
                "normal-slidertick.wav",
                result.FallbackFilename);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayTimedHitSoundEvent_SliderEdge_WhenSampleIsMissing_ReturnsFallback()
    {
        string directory = CreateTempDirectory();

        try
        {
            var (beatmap, slider) = CreateTwoSpanSlider();

            TimedHitSoundEvent soundEvent = new()
            {
                Time = 1000,
                HitObject = slider,
                EventType = HitSoundEventType.SliderEdge,
                EdgeIndex = 0
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = new AudioPlaybackEngine();

            SampleResolutionResult result = Assert.Single(
                service.PlayTimedHitSoundEvent(
                    soundEvent, beatmap,
                    directory, resolver, engine));

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
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayTimedHitSoundEvent_SliderEdge_WithoutEdgeIndex_ThrowsArgumentException()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();

        TimedHitSoundEvent soundEvent = new()
        {
            Time = 1500,
            HitObject = slider,
            EventType = HitSoundEventType.SliderEdge
        };

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlayTimedHitSoundEvent(
                    soundEvent, beatmap,
                    "beatmap", resolver, engine));

        Assert.Equal("soundEvent", exception.ParamName);
        Assert.Contains(
            "A slider edge event requires an edge index.",
            exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void PlayTimedHitSoundEvent_SliderEdge_WhenIndexIsInvalid_PropagatesException(
        int edgeIndex)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();

        TimedHitSoundEvent soundEvent = new()
        {
            Time = 1500,
            HitObject = slider,
            EventType = HitSoundEventType.SliderEdge,
            EdgeIndex = edgeIndex
        };

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlayTimedHitSoundEvent(
                    soundEvent, beatmap,
                    "beatmap", resolver, engine));

        Assert.Equal("edgeIndex", exception.ParamName);
        Assert.Equal(edgeIndex, exception.ActualValue);
    }

    [Fact]
    public void PlayTimedHitSoundEvent_WhenEventTypeIsUnsupported_ThrowsArgumentOutOfRangeException()
    {
        TimedHitSoundEvent soundEvent = new()
        {
            Time = 1000,
            HitObject = new HitObject
            {
                ObjectType = 1,
                Time = 1000
            },
            EventType = (HitSoundEventType)999
        };

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlayTimedHitSoundEvent(
                    soundEvent, new Beatmap(),
                    "beatmap", resolver, engine));

        Assert.Equal("EventType", exception.ParamName);

        Assert.Equal(
            (HitSoundEventType)999,
            exception.ActualValue);

        Assert.Contains(
            "The hitsound event type is not supported for playback.",
            exception.Message);
    }

    [Fact]
    public void PlayTimedHitSoundEvent_HitCircle_WhenObjectIsSlider_PropagatesException()
    {
        var (beatmap, slider) = CreateTwoSpanSlider();

        TimedHitSoundEvent soundEvent = new()
        {
            Time = 1000,
            HitObject = slider,
            EventType = HitSoundEventType.HitCircle
        };

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        Assert.Throws<ArgumentException>(() =>
            service.PlayTimedHitSoundEvent(
                soundEvent, beatmap,
                "beatmap", resolver, engine));
    }

    [Fact]
    public void PlayTimedHitSoundEvent_SliderTick_WhenObjectIsCircle_PropagatesException()
    {
        TimedHitSoundEvent soundEvent = new()
        {
            Time = 1000,
            HitObject = new HitObject
            {
                ObjectType = 1,
                Time = 1000
            },
            EventType = HitSoundEventType.SliderTick
        };

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlayTimedHitSoundEvent(
                    soundEvent, new Beatmap(),
                    "beatmap", resolver, engine));

        Assert.Equal("slider", exception.ParamName);
    }

    [Fact]
    public void PlayTimedHitSoundEvent_SliderEdge_WhenObjectIsCircle_PropagatesException()
    {
        TimedHitSoundEvent soundEvent = new()
        {
            Time = 1000,
            HitObject = new HitObject
            {
                ObjectType = 1,
                Time = 1000
            },
            EventType = HitSoundEventType.SliderEdge,
            EdgeIndex = 0
        };

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlayTimedHitSoundEvent(
                    soundEvent, new Beatmap(),
                    "beatmap", resolver, engine));

        Assert.Equal("slider", exception.ParamName);
    }

    [Fact]
    public void PlayTimedHitSoundEvent_WhenEventIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayTimedHitSoundEvent(
                    null!, new Beatmap(),
                    "beatmap", resolver, engine));

        Assert.Equal("soundEvent", exception.ParamName);
    }

    [Fact]
    public void PlayTimedHitSoundEvent_WhenBeatmapIsNull_ThrowsArgumentNullException()
    {
        TimedHitSoundEvent soundEvent = new()
        {
            Time = 1000,
            HitObject = new HitObject
            {
                ObjectType = 1,
                Time = 1000
            },
            EventType = HitSoundEventType.HitCircle
        };

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayTimedHitSoundEvent(
                    soundEvent, null!,
                    "beatmap", resolver, engine));

        Assert.Equal("beatmap", exception.ParamName);
    }

    [Fact]
    public void PlayTimedHitSoundEvent_WhenResolverIsNull_ThrowsArgumentNullException()
    {
        TimedHitSoundEvent soundEvent = new()
        {
            Time = 1000,
            HitObject = new HitObject
            {
                ObjectType = 1,
                Time = 1000
            },
            EventType = HitSoundEventType.HitCircle
        };

        var service = new HitSoundPlaybackService();

        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayTimedHitSoundEvent(
                    soundEvent, new Beatmap(),
                    "beatmap", null!, engine));

        Assert.Equal("resolver", exception.ParamName);
    }

    [Fact]
    public void PlayTimedHitSoundEvent_WhenEngineIsNull_ThrowsArgumentNullException()
    {
        TimedHitSoundEvent soundEvent = new()
        {
            Time = 1000,
            HitObject = new HitObject
            {
                ObjectType = 1,
                Time = 1000
            },
            EventType = HitSoundEventType.HitCircle
        };

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayTimedHitSoundEvent(
                    soundEvent, new Beatmap(),
                    "beatmap", resolver, null!));

        Assert.Equal("engine", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlayTimedHitSoundEvent_WhenDirectoryIsInvalid_ThrowsArgumentException(
        string? directory)
    {
        TimedHitSoundEvent soundEvent = new()
        {
            Time = 1000,
            HitObject = new HitObject
            {
                ObjectType = 1,
                Time = 1000
            },
            EventType = HitSoundEventType.HitCircle
        };

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlayTimedHitSoundEvent(
                    soundEvent, new Beatmap(),
                    directory!, resolver, engine));

        Assert.Equal("beatmapDirectory", exception.ParamName);

        Assert.Contains(
            "The beatmap directory must not be empty.",
            exception.Message);
    }

    [Fact]
    public void PlayTimedHitSoundEvent_WhenSampleExistsButOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        string directory = CreateTempDirectory();

        try
        {
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "normal-hitnormal.wav"));

            var (beatmap, slider) = CreateTwoSpanSlider();

            TimedHitSoundEvent soundEvent = new()
            {
                Time = 1000,
                HitObject = slider,
                EventType = HitSoundEventType.SliderEdge,
                EdgeIndex = 0
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = new AudioPlaybackEngine();

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() =>
                    service.PlayTimedHitSoundEvent(
                        soundEvent, beatmap,
                        directory, resolver, engine));

            Assert.Equal(
                "The audio output has not been initialized.",
                exception.Message);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayPendingTimedHitSoundEvents_WhenBeatmapIsEmpty_ReturnsEmpty()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var cursor = new TimedHitSoundPlaybackCursor();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        var result = service.PlayPendingTimedHitSoundEvents(
            beatmap, cursor, 1000, "beatmap", resolver, engine);

        Assert.Empty(result);
    }

    [Fact]
    public void PlayPendingTimedHitSoundEvents_ConsecutiveAdvances_DoNotRepeatEvents()
    {
        string directory = CreateTempDirectory();

        try
        {
            var beatmap = new Beatmap();

            HitObject atZero = new()
            {
                ObjectType = 1,
                Time = 0,
                HitSample = new HitSample { Filename = "missing-a.wav" }
            };

            HitObject at500 = new()
            {
                ObjectType = 1,
                Time = 500,
                HitSample = new HitSample { Filename = "missing-b.wav" }
            };

            HitObject at1000 = new()
            {
                ObjectType = 1,
                Time = 1000,
                HitSample = new HitSample { Filename = "missing-c.wav" }
            };

            beatmap.HitObjects.AddRange(new[] { atZero, at500, at1000 });

            var service = new HitSoundPlaybackService();
            var cursor = new TimedHitSoundPlaybackCursor();
            var resolver = new SampleResolver();
            using var engine = new AudioPlaybackEngine();

            var first = service.PlayPendingTimedHitSoundEvents(
                beatmap, cursor, 500, directory, resolver, engine);

            var second = service.PlayPendingTimedHitSoundEvents(
                beatmap, cursor, 1000, directory, resolver, engine);

            var repeated = service.PlayPendingTimedHitSoundEvents(
                beatmap, cursor, 1000, directory, resolver, engine);

            Assert.Equal(
                new double[] { 0, 500 },
                first.Select(item => item.Event.Time));

            Assert.Equal(
                new double[] { 1000 },
                second.Select(item => item.Event.Time));

            Assert.Same(atZero, first[0].Event.HitObject);
            Assert.Same(at500, first[1].Event.HitObject);
            Assert.Same(at1000, second[0].Event.HitObject);

            Assert.Empty(repeated);

            Assert.All(first.Concat(second), item =>
                Assert.Equal(
                    SampleResolutionOutcome.CustomSampleMissing,
                    Assert.Single(item.Results).Outcome));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayPendingTimedHitSoundEvents_MixedEvents_PreservesChronologicalResults()
    {
        string directory = CreateTempDirectory();

        try
        {
            var (beatmap, slider) = CreateTwoSpanSlider();

            beatmap.BeatmapVersion = 14;
            beatmap.SliderTickRate = 4;

            HitObject circle = new()
            {
                ObjectType = 1,
                Time = 1250,
                HitSample = new HitSample { Filename = "missing.wav" }
            };

            beatmap.HitObjects.AddRange(new[] { slider, circle });

            var service = new HitSoundPlaybackService();
            var cursor = new TimedHitSoundPlaybackCursor();
            var resolver = new SampleResolver();
            using var engine = new AudioPlaybackEngine();

            var result = service.PlayPendingTimedHitSoundEvents(
                beatmap, cursor, 2000, directory, resolver, engine);

            Assert.Equal(10, result.Count);

            Assert.Equal(
                new double[]
                {
                1000, 1125, 1250, 1250, 1375,
                1500, 1625, 1750, 1875, 2000
                },
                result.Select(item => item.Event.Time));

            Assert.Equal(
                HitSoundEventType.SliderEdge,
                result[0].Event.EventType);

            Assert.Equal(0, result[0].Event.EdgeIndex);
            Assert.Same(slider, result[0].Event.HitObject);

            Assert.Equal(
                HitSoundEventType.SliderTick,
                result[1].Event.EventType);

            Assert.Equal(
                HitSoundEventType.HitCircle,
                result[2].Event.EventType);

            Assert.Same(circle, result[2].Event.HitObject);

            Assert.Equal(
                SampleResolutionOutcome.CustomSampleMissing,
                Assert.Single(result[2].Results).Outcome);

            Assert.Equal(
                HitSoundEventType.SliderEdge,
                result[5].Event.EventType);

            Assert.Equal(1, result[5].Event.EdgeIndex);
            Assert.Equal(2, result[5].Results.Count);

            Assert.Equal(
                HitSoundEventType.SliderEdge,
                result[^1].Event.EventType);

            Assert.Equal(2, result[^1].Event.EdgeIndex);

            Assert.All(result, item =>
                Assert.NotEmpty(item.Results));

            Assert.All(
                result.Where(item =>
                    item.Event.EventType != HitSoundEventType.HitCircle),
                item =>
                    Assert.All(item.Results, sample =>
                        Assert.Equal(
                            SampleResolutionOutcome.ExternalFallbackRequired,
                            sample.Outcome)));

            Assert.Empty(service.PlayPendingTimedHitSoundEvents(
                beatmap, cursor, 2000, directory, resolver, engine));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayPendingTimedHitSoundEvents_AfterSeekToZero_ReplaysZeroTimeEvent()
    {
        var beatmap = new Beatmap();

        HitObject atZero = new()
        {
            ObjectType = 1,
            Time = 0,
            HitSample = new HitSample { Filename = "missing.wav" }
        };

        beatmap.HitObjects.Add(atZero);

        var service = new HitSoundPlaybackService();
        var cursor = new TimedHitSoundPlaybackCursor();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        Assert.Single(service.PlayPendingTimedHitSoundEvents(
            beatmap, cursor, 100, "beatmap", resolver, engine));

        cursor.SeekTo(0);

        var result = service.PlayPendingTimedHitSoundEvents(
            beatmap, cursor, 0, "beatmap", resolver, engine);

        Assert.Same(atZero, Assert.Single(result).Event.HitObject);

        Assert.Empty(service.PlayPendingTimedHitSoundEvents(
            beatmap, cursor, 0, "beatmap", resolver, engine));
    }

    [Fact]
    public void PlayPendingTimedHitSoundEvents_AfterSeek_ExcludesSeekBoundary()
    {
        var beatmap = new Beatmap();

        HitObject atBoundary = new()
        {
            ObjectType = 1,
            Time = 1000,
            HitSample = new HitSample { Filename = "missing-a.wav" }
        };

        HitObject afterBoundary = new()
        {
            ObjectType = 1,
            Time = 1200,
            HitSample = new HitSample { Filename = "missing-b.wav" }
        };

        beatmap.HitObjects.AddRange(new[] { atBoundary, afterBoundary });

        var service = new HitSoundPlaybackService();
        var cursor = new TimedHitSoundPlaybackCursor();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        Assert.Equal(2, service.PlayPendingTimedHitSoundEvents(
            beatmap, cursor, 1200, "beatmap", resolver, engine).Count);

        cursor.SeekTo(1000);

        var result = service.PlayPendingTimedHitSoundEvents(
            beatmap, cursor, 1200, "beatmap", resolver, engine);

        Assert.Same(
            afterBoundary,
            Assert.Single(result).Event.HitObject);
    }

    [Fact]
    public void PlayPendingTimedHitSoundEvents_WhenPositionIsInvalid_PreservesCursor()
    {
        var beatmap = new Beatmap();

        HitObject circle = new()
        {
            ObjectType = 1,
            Time = 500,
            HitSample = new HitSample { Filename = "missing.wav" }
        };

        beatmap.HitObjects.Add(circle);

        var service = new HitSoundPlaybackService();
        var cursor = new TimedHitSoundPlaybackCursor();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlayPendingTimedHitSoundEvents(
                    beatmap, cursor, -1, "beatmap", resolver, engine));

        Assert.Equal("currentPositionMilliseconds", exception.ParamName);

        var result = service.PlayPendingTimedHitSoundEvents(
            beatmap, cursor, 500, "beatmap", resolver, engine);

        Assert.Same(circle, Assert.Single(result).Event.HitObject);
    }

    [Fact]
    public void PlayPendingTimedHitSoundEvents_WhenBeatmapIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var cursor = new TimedHitSoundPlaybackCursor();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayPendingTimedHitSoundEvents(
                    null!, cursor, 1000, "beatmap", resolver, engine));

        Assert.Equal("beatmap", exception.ParamName);
    }

    [Fact]
    public void PlayPendingTimedHitSoundEvents_WhenCursorIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayPendingTimedHitSoundEvents(
                    beatmap, null!, 1000, "beatmap", resolver, engine));

        Assert.Equal("cursor", exception.ParamName);
    }

    [Fact]
    public void PlayPendingTimedHitSoundEvents_WhenResolverIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var cursor = new TimedHitSoundPlaybackCursor();
        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayPendingTimedHitSoundEvents(
                    beatmap, cursor, 1000, "beatmap", null!, engine));

        Assert.Equal("resolver", exception.ParamName);
    }

    [Fact]
    public void PlayPendingTimedHitSoundEvents_WhenEngineIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var cursor = new TimedHitSoundPlaybackCursor();
        var resolver = new SampleResolver();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.PlayPendingTimedHitSoundEvents(
                    beatmap, cursor, 1000, "beatmap", resolver, null!));

        Assert.Equal("engine", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlayPendingTimedHitSoundEvents_WhenDirectoryIsInvalid_ThrowsArgumentException(
        string? invalidDirectory)
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var cursor = new TimedHitSoundPlaybackCursor();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlayPendingTimedHitSoundEvents(
                    beatmap, cursor, 1000,
                    invalidDirectory!, resolver, engine));

        Assert.Equal("beatmapDirectory", exception.ParamName);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PlayHitSoundLayer_WhenDelayIsInvalid_ThrowsArgumentOutOfRangeException(
        double invalidDelay)
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        var layer = new HitSoundLayer();
        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlayHitSoundLayer(
                    layer,
                    "beatmap",
                    resolver,
                    engine,
                    invalidDelay));

        Assert.Equal("delayMilliseconds", exception.ParamName);
        Assert.Equal(invalidDelay, exception.ActualValue);
        Assert.Contains(
            "The audio delay must be finite and non-negative.",
            exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlayHitSoundLayer_WhenDelayIsProvided_StartsAtExpectedFrame(
        bool custom)
    {
        string directory = CreateTempDirectory();

        try
        {
            string filename = custom
                ? "custom.wav"
                : "normal-hitnormal.wav";

            string path = Path.Combine(directory, filename);
            WriteHalfAmplitudeWav(path);

            var layer = new HitSoundLayer
            {
                Type = custom
                    ? HitSoundType.Custom
                    : HitSoundType.Normal,
                Filename = custom ? filename : string.Empty,
                SampleSet = SampleSetType.Normal,
                SampleIndex = 1,
                Volume = 50
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            SampleResolutionResult result =
                service.PlayHitSoundLayer(
                    layer,
                    directory,
                    resolver,
                    engine,
                    delayMilliseconds: 1);

            Assert.Equal(
                custom
                    ? SampleResolutionOutcome.CustomSampleFound
                    : SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);

            Assert.Equal(path, result.ResolvedPath);

            // 1 ms at 44100 Hz becomes 44 frames.
            // The mixer is stereo: 2 samples per frame.
            float[] output = new float[45 * 2];

            int samplesRead = mixer.Read(output.AsSpan());

            Assert.Equal(output.Length, samplesRead);

            // First 44 frames contain silence.
            Assert.All(output.Take(44 * 2), sample =>
                Assert.Equal(0f, sample));

            // Frame 44 begins playing the sample.
            Assert.InRange(output[44 * 2], 0.24f, 0.26f);
            Assert.InRange(output[44 * 2 + 1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlayHitSoundLayer_WhenSampleIsMissing_WithDelay_DoesNotAttemptPlayback(
        bool custom)
    {
        string directory = CreateTempDirectory();

        try
        {
            var layer = new HitSoundLayer
            {
                Type = custom
                    ? HitSoundType.Custom
                    : HitSoundType.Normal,
                Filename = custom ? "missing.wav" : string.Empty,
                SampleSet = SampleSetType.Normal,
                SampleIndex = 1,
                Volume = 50
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();
            using var engine = new AudioPlaybackEngine();

            SampleResolutionResult result =
                service.PlayHitSoundLayer(
                    layer,
                    directory,
                    resolver,
                    engine,
                    delayMilliseconds: 250);

            Assert.Equal(
                custom
                    ? SampleResolutionOutcome.CustomSampleMissing
                    : SampleResolutionOutcome.ExternalFallbackRequired,
                result.Outcome);

            Assert.Null(result.ResolvedPath);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PlayHitSoundLayers_WhenDelayIsInvalid_ThrowsArgumentOutOfRangeException(
        double invalidDelay)
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlayHitSoundLayers(
                    Array.Empty<HitSoundLayer>(),
                    "beatmap",
                    resolver,
                    engine,
                    invalidDelay));

        Assert.Equal("delayMilliseconds", exception.ParamName);
        Assert.Equal(invalidDelay, exception.ActualValue);
        Assert.Contains(
            "The audio delay must be finite and non-negative.",
            exception.Message);
    }

    [Fact]
    public void PlayHitSoundLayers_WhenDelayIsProvided_DelaysAllLayersEqually()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(
                directory, "normal-hitnormal.wav");

            string clapPath = Path.Combine(
                directory, "normal-hitclap.wav");

            WriteHalfAmplitudeWav(normalPath);
            WriteHalfAmplitudeWav(clapPath);

            HitSoundLayer[] layers =
            {
            new()
            {
                Type = HitSoundType.Normal,
                SampleSet = SampleSetType.Normal,
                SampleIndex = 1,
                Volume = 50
            },
            new()
            {
                Type = HitSoundType.Clap,
                SampleSet = SampleSetType.Normal,
                SampleIndex = 1,
                Volume = 25
            }
        };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlayHitSoundLayers(
                    layers,
                    directory,
                    resolver,
                    engine,
                    delayMilliseconds: 1);

            Assert.Equal(2, results.Count);

            Assert.All(results, result =>
                Assert.Equal(
                    SampleResolutionOutcome.BeatmapSampleFound,
                    result.Outcome));

            Assert.Equal(normalPath, results[0].ResolvedPath);
            Assert.Equal(clapPath, results[1].ResolvedPath);

            // 1 ms at 44100 Hz = 44 whole frames.
            // Stereo output = 2 samples per frame.
            float[] output = new float[46 * 2];

            int samplesRead = mixer.Read(output.AsSpan());

            Assert.Equal(output.Length, samplesRead);

            // Both layers must remain silent before frame 44.
            Assert.All(output.Take(44 * 2), sample =>
                Assert.Equal(0f, sample));

            // Normal: 0.5 * 0.50 = 0.25
            // Clap:   0.5 * 0.25 = 0.125
            // Combined expected amplitude = 0.375
            for (int frame = 44; frame < 46; frame++)
            {
                Assert.InRange(
                    output[frame * 2],
                    0.365f,
                    0.385f);

                Assert.InRange(
                    output[frame * 2 + 1],
                    0.365f,
                    0.385f);
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayHitSoundLayers_WhenDelayIsProvided_WithEmptyList_ReturnsEmpty()
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        List<SampleResolutionResult> results =
            service.PlayHitSoundLayers(
                Array.Empty<HitSoundLayer>(),
                "beatmap",
                resolver,
                engine,
                delayMilliseconds: 250);

        Assert.Empty(results);
    }

    [Fact]
    public void PlayHitSoundLayers_WhenDelayIsProvided_WithMissingSamples_PreservesOutcomes()
    {
        string directory = CreateTempDirectory();

        try
        {
            HitSoundLayer[] layers =
            {
            new()
            {
                Type = HitSoundType.Custom,
                Filename = "missing.wav",
                SampleSet = SampleSetType.Normal,
                SampleIndex = 1,
                Volume = 50
            },
            new()
            {
                Type = HitSoundType.Normal,
                SampleSet = SampleSetType.Normal,
                SampleIndex = 1,
                Volume = 50
            }
        };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            // No output device is initialized.
            // Missing samples must not reach the audio engine.
            using var engine = new AudioPlaybackEngine();

            List<SampleResolutionResult> results =
                service.PlayHitSoundLayers(
                    layers,
                    directory,
                    resolver,
                    engine,
                    delayMilliseconds: 250);

            Assert.Equal(2, results.Count);

            Assert.Equal(
                SampleResolutionOutcome.CustomSampleMissing,
                results[0].Outcome);

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                results[1].Outcome);

            Assert.All(results, result =>
                Assert.Null(result.ResolvedPath));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PlayHitCircle_WhenDelayIsInvalid_ThrowsArgumentOutOfRangeException(
        double invalidDelay)
    {
        var service = new HitSoundPlaybackService();
        var beatmap = new Beatmap();
        var hitObject = new HitObject { ObjectType = 1 };
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlayHitCircle(
                    beatmap,
                    hitObject,
                    "beatmap",
                    resolver,
                    engine,
                    invalidDelay));

        Assert.Equal("delayMilliseconds", exception.ParamName);
        Assert.Equal(invalidDelay, exception.ActualValue);
        Assert.Contains(
            "The audio delay must be finite and non-negative.",
            exception.Message);
    }

    [Fact]
    public void PlayHitCircle_WhenDelayed_PlaysNormalAndClapSimultaneously()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(
                directory, "normal-hitnormal.wav");

            string clapPath = Path.Combine(
                directory, "normal-hitclap.wav");

            WriteHalfAmplitudeWav(normalPath);
            WriteHalfAmplitudeWav(clapPath);

            var beatmap = new Beatmap();
            var hitObject = new HitObject
            {
                ObjectType = 1,
                HitSound = 8,
                HitSample = new HitSample
                {
                    NormalSet = 1,
                    AdditionSet = 1,
                    Index = 1,
                    Volume = 50
                }
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlayHitCircle(
                    beatmap,
                    hitObject,
                    directory,
                    resolver,
                    engine,
                    delayMilliseconds: 1);

            Assert.Equal(2, results.Count);
            Assert.Equal(normalPath, results[0].ResolvedPath);
            Assert.Equal(clapPath, results[1].ResolvedPath);

            Assert.All(results, result =>
                Assert.Equal(
                    SampleResolutionOutcome.BeatmapSampleFound,
                    result.Outcome));

            float[] output = new float[46 * 2];

            Assert.Equal(
                output.Length,
                mixer.Read(output.AsSpan()));

            // 1 ms = 44 frames at 44100 Hz.
            Assert.All(output.Take(44 * 2), sample =>
                Assert.Equal(0f, sample));

            // Both layers: 0.25 + 0.25 = 0.50.
            for (int frame = 44; frame < 46; frame++)
            {
                Assert.InRange(
                    output[frame * 2],
                    0.49f,
                    0.51f);

                Assert.InRange(
                    output[frame * 2 + 1],
                    0.49f,
                    0.51f);
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlayHitCircle_WhenDelayedAndClapIsMissing_PlaysNormalOnly()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(
                directory, "normal-hitnormal.wav");

            WriteHalfAmplitudeWav(normalPath);

            var beatmap = new Beatmap();
            var hitObject = new HitObject
            {
                ObjectType = 1,
                HitSound = 8,
                HitSample = new HitSample
                {
                    NormalSet = 1,
                    AdditionSet = 1,
                    Index = 1,
                    Volume = 50
                }
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlayHitCircle(
                    beatmap,
                    hitObject,
                    directory,
                    resolver,
                    engine,
                    delayMilliseconds: 1);

            Assert.Equal(2, results.Count);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                results[0].Outcome);

            Assert.Equal(normalPath, results[0].ResolvedPath);

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                results[1].Outcome);

            Assert.Null(results[1].ResolvedPath);

            float[] output = new float[45 * 2];

            Assert.Equal(
                output.Length,
                mixer.Read(output.AsSpan()));

            Assert.All(output.Take(44 * 2), sample =>
                Assert.Equal(0f, sample));

            // Only Normal: 0.5 * 0.5 = 0.25.
            Assert.InRange(output[44 * 2], 0.24f, 0.26f);
            Assert.InRange(output[44 * 2 + 1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PlaySliderEdge_WhenDelayIsInvalid_ThrowsArgumentOutOfRangeException(
        double invalidDelay)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlaySliderEdge(
                    beatmap,
                    slider,
                    1,
                    "beatmap",
                    resolver,
                    engine,
                    invalidDelay));

        Assert.Equal("delayMilliseconds", exception.ParamName);
        Assert.Equal(invalidDelay, exception.ActualValue);
        Assert.Contains(
            "The audio delay must be finite and non-negative.",
            exception.Message);
    }

    [Fact]
    public void PlaySliderEdge_WhenDelayed_RepeatPlaysBothLayersSimultaneously()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(
                directory, "soft-hitnormal2.wav");

            string clapPath = Path.Combine(
                directory, "soft-hitclap2.wav");

            WriteHalfAmplitudeWav(normalPath);
            WriteHalfAmplitudeWav(clapPath);

            var (beatmap, slider) = CreateTwoSpanSlider();
            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlaySliderEdge(
                    beatmap,
                    slider,
                    1,
                    directory,
                    resolver,
                    engine,
                    delayMilliseconds: 1);

            Assert.Equal(2, results.Count);
            Assert.Equal(normalPath, results[0].ResolvedPath);
            Assert.Equal(clapPath, results[1].ResolvedPath);

            Assert.All(results, result =>
                Assert.Equal(
                    SampleResolutionOutcome.BeatmapSampleFound,
                    result.Outcome));

            float[] output = new float[46 * 2];

            Assert.Equal(
                output.Length,
                mixer.Read(output.AsSpan()));

            // 1 ms at 44100 Hz = 44 frames.
            Assert.All(output.Take(44 * 2), sample =>
                Assert.Equal(0f, sample));

            // Repeat edge volume comes from the timing point at 1500 ms.
            // Each layer: 0.5 * 0.25 = 0.125.
            // Together: 0.25.
            for (int frame = 44; frame < 46; frame++)
            {
                Assert.InRange(output[frame * 2], 0.24f, 0.26f);
                Assert.InRange(output[frame * 2 + 1], 0.24f, 0.26f);
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderEdge_WhenDelayedAndClapIsMissing_PlaysNormalOnly()
    {
        string directory = CreateTempDirectory();

        try
        {
            string normalPath = Path.Combine(
                directory, "soft-hitnormal2.wav");

            WriteHalfAmplitudeWav(normalPath);

            var (beatmap, slider) = CreateTwoSpanSlider();
            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlaySliderEdge(
                    beatmap,
                    slider,
                    1,
                    directory,
                    resolver,
                    engine,
                    delayMilliseconds: 1);

            Assert.Equal(2, results.Count);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                results[0].Outcome);

            Assert.Equal(normalPath, results[0].ResolvedPath);

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                results[1].Outcome);

            Assert.Null(results[1].ResolvedPath);

            float[] output = new float[45 * 2];

            Assert.Equal(
                output.Length,
                mixer.Read(output.AsSpan()));

            Assert.All(output.Take(44 * 2), sample =>
                Assert.Equal(0f, sample));

            // Only Normal: 0.5 * 0.25 = 0.125.
            Assert.InRange(output[44 * 2], 0.115f, 0.135f);
            Assert.InRange(output[44 * 2 + 1], 0.115f, 0.135f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PlaySliderTick_WhenDelayIsInvalid_ThrowsArgumentOutOfRangeException(
        double invalidDelay)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlaySliderTick(
                    beatmap,
                    slider,
                    "beatmap",
                    resolver,
                    engine,
                    invalidDelay));

        Assert.Equal("delayMilliseconds", exception.ParamName);
        Assert.Equal(invalidDelay, exception.ActualValue);
        Assert.Contains(
            "The audio delay must be finite and non-negative.",
            exception.Message);
    }

    [Fact]
    public void PlaySliderTick_WhenDelayIsProvided_StartsAtExpectedFrame()
    {
        string directory = CreateTempDirectory();

        try
        {
            string tickPath = Path.Combine(
                directory, "normal-slidertick.wav");

            WriteHalfAmplitudeWav(tickPath);

            var (beatmap, slider) = CreateTwoSpanSlider();
            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlaySliderTick(
                    beatmap,
                    slider,
                    directory,
                    resolver,
                    engine,
                    delayMilliseconds: 1);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);

            Assert.Equal(tickPath, result.ResolvedPath);

            float[] output = new float[45 * 2];

            Assert.Equal(
                output.Length,
                mixer.Read(output.AsSpan()));

            // 1 ms at 44100 Hz = 44 whole frames.
            Assert.All(output.Take(44 * 2), sample =>
                Assert.Equal(0f, sample));

            // Tick volume comes from the slider start timing point:
            // amplitude 0.5 * volume 50% = 0.25.
            Assert.InRange(output[44 * 2], 0.24f, 0.26f);
            Assert.InRange(output[44 * 2 + 1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderTick_WhenDelayIsProvided_AndSampleMissing_ReturnsFallback()
    {
        string directory = CreateTempDirectory();

        try
        {
            var (beatmap, slider) = CreateTwoSpanSlider();
            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            // No audio output is initialized.
            using var engine = new AudioPlaybackEngine();

            List<SampleResolutionResult> results =
                service.PlaySliderTick(
                    beatmap,
                    slider,
                    directory,
                    resolver,
                    engine,
                    delayMilliseconds: 250);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                result.Outcome);

            Assert.Null(result.ResolvedPath);

            Assert.Equal(
                "normal-slidertick.wav",
                result.FallbackFilename);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PlayTimedHitSoundEvent_WhenDelayIsInvalid_ThrowsArgumentOutOfRangeException(
        double invalidDelay)
    {
        var soundEvent = new TimedHitSoundEvent
        {
            Time = 1000,
            HitObject = new HitObject { ObjectType = 1 },
            EventType = HitSoundEventType.HitCircle
        };

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlayTimedHitSoundEvent(
                    soundEvent,
                    new Beatmap(),
                    "beatmap",
                    resolver,
                    engine,
                    invalidDelay));

        Assert.Equal("delayMilliseconds", exception.ParamName);
        Assert.Equal(invalidDelay, exception.ActualValue);
        Assert.Contains(
            "The audio delay must be finite and non-negative.",
            exception.Message);
    }

    [Theory]
    [InlineData(HitSoundEventType.HitCircle, "custom.wav")]
    [InlineData(HitSoundEventType.SliderEdge, "normal-hitnormal.wav")]
    [InlineData(HitSoundEventType.SliderTick, "normal-slidertick.wav")]
    public void PlayTimedHitSoundEvent_WhenDelayed_PlaysCorrectEventAtExpectedFrame(
        HitSoundEventType eventType,
        string filename)
    {
        string directory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(directory, filename);
            WriteHalfAmplitudeWav(path);

            var (beatmap, slider) = CreateTwoSpanSlider();

            var circle = new HitObject
            {
                ObjectType = 1,
                Time = 1000,
                HitSample = new HitSample
                {
                    Filename = "custom.wav",
                    Volume = 50
                }
            };

            var soundEvent = new TimedHitSoundEvent
            {
                Time = eventType == HitSoundEventType.SliderTick
                    ? 1250
                    : 1000,

                HitObject = eventType == HitSoundEventType.HitCircle
                    ? circle
                    : slider,

                EventType = eventType,

                EdgeIndex = eventType == HitSoundEventType.SliderEdge
                    ? 0
                    : null
            };

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results =
                service.PlayTimedHitSoundEvent(
                    soundEvent,
                    beatmap,
                    directory,
                    resolver,
                    engine,
                    delayMilliseconds: 1);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                eventType == HitSoundEventType.HitCircle
                    ? SampleResolutionOutcome.CustomSampleFound
                    : SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);

            Assert.Equal(path, result.ResolvedPath);

            // 1 ms at 44100 Hz = 44 whole frames.
            float[] output = new float[45 * 2];

            Assert.Equal(
                output.Length,
                mixer.Read(output.AsSpan()));

            // Frames 0 through 43 must contain silence.
            Assert.All(output.Take(44 * 2), sample =>
                Assert.Equal(0f, sample));

            // The sample begins at frame 44.
            // Amplitude 0.5 * volume 50% = 0.25.
            Assert.InRange(output[44 * 2], 0.24f, 0.26f);
            Assert.InRange(output[44 * 2 + 1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(-1.0, 2.0, "startMilliseconds")]
    [InlineData(double.NaN, 2.0, "startMilliseconds")]
    [InlineData(double.PositiveInfinity, 2.0, "startMilliseconds")]
    [InlineData(double.NegativeInfinity, 2.0, "startMilliseconds")]
    [InlineData(0.0, -1.0, "endMilliseconds")]
    [InlineData(0.0, double.NaN, "endMilliseconds")]
    [InlineData(0.0, double.PositiveInfinity, "endMilliseconds")]
    [InlineData(0.0, double.NegativeInfinity, "endMilliseconds")]
    [InlineData(2.0, 1.0, "endMilliseconds")]
    public void QueueTimedHitSoundEventsInRange_WhenRangeIsInvalid_ThrowsArgumentOutOfRangeException(
        double startMilliseconds,
        double endMilliseconds,
        string expectedParameter)
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.QueueTimedHitSoundEventsInRange(
                    new Beatmap(),
                    startMilliseconds,
                    endMilliseconds,
                    "beatmap",
                    resolver,
                    engine));

        Assert.Equal(expectedParameter, exception.ParamName);
    }

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(1000.0, false)]
    public void QueueTimedHitSoundEventsInRange_WhenBoundsAreEqual_HandlesZeroTimeBoundary(
        double positionMilliseconds,
        bool expectsZeroTimeEvent)
    {
        var beatmap = new Beatmap();

        HitObject circle = new()
        {
            ObjectType = 1,
            Time = 0,
            HitSample = new HitSample
            {
                Filename = "missing.wav",
                Volume = 50
            }
        };

        beatmap.HitObjects.Add(circle);

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        var results = service.QueueTimedHitSoundEventsInRange(
            beatmap,
            positionMilliseconds,
            positionMilliseconds,
            "beatmap",
            resolver,
            engine);

        if (expectsZeroTimeEvent)
        {
            var queued = Assert.Single(results);

            Assert.Equal(0, queued.Event.Time);
            Assert.Same(circle, queued.Event.HitObject);

            SampleResolutionResult resolution =
                Assert.Single(queued.Results);

            Assert.Equal(
                SampleResolutionOutcome.CustomSampleMissing,
                resolution.Outcome);
        }
        else
        {
            Assert.Empty(results);
        }
    }

    [Fact]
    public void QueueTimedHitSoundEventsInRange_SchedulesOverlappingCirclesAtRelativeTimes()
    {
        string directory = CreateTempDirectory();

        try
        {
            string firstPath = Path.Combine(directory, "first.wav");
            string secondPath = Path.Combine(directory, "second.wav");

            WriteHalfAmplitudeWav(firstPath);
            WriteHalfAmplitudeWav(secondPath);

            var beatmap = new Beatmap();

            HitObject[] circles =
            {
            new()
            {
                ObjectType = 1,
                Time = 1000,
                HitSample = new HitSample
                {
                    Filename = "excluded-start.wav",
                    Volume = 50
                }
            },
            new()
            {
                ObjectType = 1,
                Time = 1001,
                HitSample = new HitSample
                {
                    Filename = "first.wav",
                    Volume = 50
                }
            },
            new()
            {
                ObjectType = 1,
                Time = 1002,
                HitSample = new HitSample
                {
                    Filename = "second.wav",
                    Volume = 50
                }
            },
            new()
            {
                ObjectType = 1,
                Time = 1003,
                HitSample = new HitSample
                {
                    Filename = "excluded-end.wav",
                    Volume = 50
                }
            }
        };

            beatmap.HitObjects.AddRange(circles);

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            mixer.ReadFully = true;

            var results = service.QueueTimedHitSoundEventsInRange(
                beatmap,
                startMilliseconds: 1000,
                endMilliseconds: 1002,
                beatmapDirectory: directory,
                resolver: resolver,
                engine: engine);

            Assert.Equal(2, results.Count);

            Assert.Equal(1001, results[0].Event.Time);
            Assert.Equal(1002, results[1].Event.Time);

            Assert.Same(circles[1], results[0].Event.HitObject);
            Assert.Same(circles[2], results[1].Event.HitObject);

            Assert.Equal(
                firstPath,
                Assert.Single(results[0].Results).ResolvedPath);

            Assert.Equal(
                secondPath,
                Assert.Single(results[1].Results).ResolvedPath);

            // Each file contains 100 frames at 44100 Hz.
            // First starts at frame 44.
            // Second starts at frame 88.
            // Both overlap between frames 88 and 143.
            float[] output = new float[190 * 2];

            Assert.Equal(
                output.Length,
                mixer.Read(output.AsSpan()));

            static void AssertFrame(
                float[] samples,
                int frame,
                float expectedAmplitude)
            {
                const float tolerance = 0.01f;

                Assert.InRange(
                    samples[frame * 2],
                    expectedAmplitude - tolerance,
                    expectedAmplitude + tolerance);

                Assert.InRange(
                    samples[frame * 2 + 1],
                    expectedAmplitude - tolerance,
                    expectedAmplitude + tolerance);
            }

            // Both sources are initially silent.
            AssertFrame(output, 0, 0f);
            AssertFrame(output, 43, 0f);

            // Only the first source is active.
            AssertFrame(output, 44, 0.25f);
            AssertFrame(output, 87, 0.25f);

            // Both sources overlap.
            AssertFrame(output, 88, 0.50f);
            AssertFrame(output, 143, 0.50f);

            // Only the second source remains.
            AssertFrame(output, 144, 0.25f);
            AssertFrame(output, 187, 0.25f);

            // Both sources have ended.
            AssertFrame(output, 188, 0f);
            AssertFrame(output, 189, 0f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void QueueTimedHitSoundEventsInRange_WithTimelineAudio_MixesAtCorrectFrame()
    {
        string directory = CreateTempDirectory();

        try
        {
            string musicPath = Path.Combine(directory, "music.wav");
            string hitPath = Path.Combine(directory, "hit.wav");

            // Music: 200 frames at half amplitude.
            var format = new WaveFormat(44100, 16, 1);

            using (var writer = new WaveFileWriter(musicPath, format))
            {
                byte[] audio = new byte[format.BlockAlign * 200];

                for (int i = 0; i < audio.Length; i += 2)
                {
                    audio[i] = 0;
                    audio[i + 1] = 64;
                }

                writer.Write(audio, 0, audio.Length);
            }

            // Hitsound: 100 frames at half amplitude.
            WriteHalfAmplitudeWav(hitPath);

            var beatmap = new Beatmap();

            beatmap.HitObjects.Add(new HitObject
            {
                ObjectType = 1,
                Time = 1,
                HitSample = new HitSample
                {
                    Filename = "hit.wav",
                    Volume = 50
                }
            });

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            mixer.ReadFully = true;

            engine.LoadTimelineAudio(musicPath);

            var results = service.QueueTimedHitSoundEventsInRange(
                beatmap,
                0,
                2,
                directory,
                resolver,
                engine);

            var queued = Assert.Single(results);

            Assert.Equal(1, queued.Event.Time);

            SampleResolutionResult result =
                Assert.Single(queued.Results);

            Assert.Equal(
                SampleResolutionOutcome.CustomSampleFound,
                result.Outcome);

            Assert.Equal(hitPath, result.ResolvedPath);

            float[] output = new float[150 * 2];

            Assert.Equal(
                output.Length,
                mixer.Read(output.AsSpan()));

            // Music plays alone before frame 44.
            Assert.InRange(output[0], 0.49f, 0.51f);
            Assert.InRange(output[43 * 2], 0.49f, 0.51f);

            // At frame 44, music + hitsound overlap:
            // Music 0.5 + hitsound (0.5 * 0.5) = 0.75.
            Assert.InRange(output[44 * 2], 0.74f, 0.76f);
            Assert.InRange(output[143 * 2], 0.74f, 0.76f);

            // The hitsound ends; music continues.
            Assert.InRange(output[144 * 2], 0.49f, 0.51f);
            Assert.InRange(output[149 * 2], 0.49f, 0.51f);

            // Stereo channels have equal values.
            Assert.Equal(output[44 * 2], output[44 * 2 + 1]);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(0.0, false, true)]
    [InlineData(0.0, true, true)]
    [InlineData(1000.0, false, false)]
    [InlineData(1000.0, true, true)]
    public void QueueTimedHitSoundEventsInRange_WhenBoundsAreEqual_RespectsIncludeStart(
        double positionMilliseconds,
        bool includeStart,
        bool expectsEvent)
    {
        var beatmap = new Beatmap();

        var circle = new HitObject
        {
            ObjectType = 1,
            Time = (int)positionMilliseconds,
            HitSample = new HitSample
            {
                Filename = "definitely-missing-include-start.wav",
                Volume = 50
            }
        };

        beatmap.HitObjects.Add(circle);

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        var results = service.QueueTimedHitSoundEventsInRange(
            beatmap,
            positionMilliseconds,
            positionMilliseconds,
            "beatmap",
            resolver,
            engine,
            includeStart);

        if (!expectsEvent)
        {
            Assert.Empty(results);
            return;
        }

        var queued = Assert.Single(results);

        Assert.Equal(positionMilliseconds, queued.Event.Time);
        Assert.Same(circle, queued.Event.HitObject);

        SampleResolutionResult resolution =
            Assert.Single(queued.Results);

        Assert.Equal(
            SampleResolutionOutcome.CustomSampleMissing,
            resolution.Outcome);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public void QueueTimedHitSoundEventsInRange_WhenStartEventExists_SchedulesAccordingToIncludeStart(
        bool includeStart,
        int expectedEventCount)
    {
        string directory = CreateTempDirectory();

        try
        {
            string boundaryPath = Path.Combine(
                directory, "boundary.wav");

            string laterPath = Path.Combine(
                directory, "later.wav");

            WriteHalfAmplitudeWav(boundaryPath);
            WriteHalfAmplitudeWav(laterPath);

            var beatmap = new Beatmap();

            beatmap.HitObjects.Add(new HitObject
            {
                ObjectType = 1,
                Time = 1000,
                HitSample = new HitSample
                {
                    Filename = "boundary.wav",
                    Volume = 50
                }
            });

            beatmap.HitObjects.Add(new HitObject
            {
                ObjectType = 1,
                Time = 1001,
                HitSample = new HitSample
                {
                    Filename = "later.wav",
                    Volume = 50
                }
            });

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            mixer.ReadFully = true;

            var results = service.QueueTimedHitSoundEventsInRange(
                beatmap,
                1000,
                1001,
                directory,
                resolver,
                engine,
                includeStart);

            Assert.Equal(expectedEventCount, results.Count);

            Assert.All(results, queued =>
                Assert.Equal(
                    SampleResolutionOutcome.CustomSampleFound,
                    Assert.Single(queued.Results).Outcome));

            Assert.Equal(1001, results[^1].Event.Time);
            Assert.Equal(
                laterPath,
                Assert.Single(results[^1].Results).ResolvedPath);

            if (includeStart)
            {
                Assert.Equal(1000, results[0].Event.Time);
                Assert.Equal(
                    boundaryPath,
                    Assert.Single(results[0].Results).ResolvedPath);
            }

            float[] output = new float[145 * 2];

            Assert.Equal(
                output.Length,
                mixer.Read(output.AsSpan()));

            static void AssertFrame(
                float[] samples,
                int frame,
                float expected)
            {
                Assert.InRange(
                    samples[frame * 2],
                    expected - 0.01f,
                    expected + 0.01f);

                Assert.InRange(
                    samples[frame * 2 + 1],
                    expected - 0.01f,
                    expected + 0.01f);
            }

            // The boundary event starts at frame 0 only
            // when includeStart is true.
            float initialAmplitude = includeStart ? 0.25f : 0f;

            AssertFrame(output, 0, initialAmplitude);
            AssertFrame(output, 43, initialAmplitude);

            // The event at 1001 ms starts at frame 44.
            // With includeStart, both samples overlap.
            float overlapAmplitude = includeStart ? 0.50f : 0.25f;

            AssertFrame(output, 44, overlapAmplitude);
            AssertFrame(output, 99, overlapAmplitude);

            // The boundary sample has ended.
            // Only the later sample remains.
            AssertFrame(output, 100, 0.25f);
            AssertFrame(output, 143, 0.25f);

            // Both samples have ended.
            AssertFrame(output, 144, 0f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
    [Fact]
    public void QueueTimedHitSoundEventsInRange_SliderHead_SchedulesBodyWithSameDelay()
    {
        string directory = CreateTempDirectory();

        try
        {
            string headPath = Path.Combine(directory, "normal-hitnormal.wav");
            string bodyPath = Path.Combine(directory, "normal-sliderslide.wav");

            WriteHalfAmplitudeWav(headPath);
            WriteHalfAmplitudeWav(bodyPath);

            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.SliderTickRate = 1;
            beatmap.HitObjects.Add(slider);

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            mixer.ReadFully = true;

            var queuedEvents = service.QueueTimedHitSoundEventsInRange(
                beatmap, 900, 1100, directory, resolver, engine);

            var queued = Assert.Single(queuedEvents);

            Assert.Equal(HitSoundEventType.SliderEdge, queued.Event.EventType);
            Assert.Equal(0, queued.Event.EdgeIndex);
            Assert.Equal(1000, queued.Event.Time);

            Assert.Equal(2, queued.Results.Count);
            Assert.Equal(headPath, queued.Results[0].ResolvedPath);
            Assert.Equal(bodyPath, queued.Results[1].ResolvedPath);

            Assert.All(queued.Results, result =>
                Assert.Equal(
                    SampleResolutionOutcome.BeatmapSampleFound,
                    result.Outcome));

            Assert.Equal(1000d, beatmap.GetSliderDuration(slider));

            const int delayFrames = 4410;
            const int headFrames = 100;
            const int bodyFrames = 44100;
            const int endFrame = delayFrames + bodyFrames;

            float[] output = new float[(endFrame + 1) * 2];
            Assert.Equal(output.Length, mixer.Read(output.AsSpan()));

            static void AssertFrame(float[] samples, int frame, float expected)
            {
                Assert.InRange(
                    samples[frame * 2],
                    expected - 0.01f,
                    expected + 0.01f);

                Assert.InRange(
                    samples[frame * 2 + 1],
                    expected - 0.01f,
                    expected + 0.01f);
            }

            AssertFrame(output, 0, 0f);
            AssertFrame(output, delayFrames - 1, 0f);

            // Head and body start together.
            AssertFrame(output, delayFrames, 0.50f);
            AssertFrame(output, delayFrames + headFrames - 1, 0.50f);

            // The head ends, but the body continues.
            AssertFrame(output, delayFrames + headFrames, 0.25f);
            AssertFrame(output, endFrame - 1, 0.25f);

            // The body finishes after the full slider duration.
            AssertFrame(output, endFrame, 0f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void QueueTimedHitSoundEventsInRange_SliderWithWhistle_QueuesBothBodyLayers()
    {
        string directory = CreateTempDirectory();

        try
        {
            string headPath = Path.Combine(directory, "normal-hitnormal.wav");
            string slidePath = Path.Combine(directory, "normal-sliderslide.wav");
            string whistlePath = Path.Combine(directory, "soft-sliderwhistle.wav");

            WriteHalfAmplitudeWav(headPath);
            WriteHalfAmplitudeWav(slidePath);
            WriteHalfAmplitudeWav(whistlePath);

            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.SliderTickRate = 1;
            slider.HitSound = 2;
            slider.HitSample.AdditionSet = 2;
            beatmap.HitObjects.Add(slider);

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            var queued = Assert.Single(
                service.QueueTimedHitSoundEventsInRange(
                    beatmap, 1000, 1000, directory, resolver, engine,
                    includeStart: true));

            Assert.Equal(3, queued.Results.Count);

            Assert.Equal(headPath, queued.Results[0].ResolvedPath);
            Assert.Equal(slidePath, queued.Results[1].ResolvedPath);
            Assert.Equal(whistlePath, queued.Results[2].ResolvedPath);

            Assert.All(queued.Results, result =>
                Assert.Equal(
                    SampleResolutionOutcome.BeatmapSampleFound,
                    result.Outcome));

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // Head + slide + whistle, each at 50% volume.
            Assert.InRange(output[0], 0.74f, 0.76f);
            Assert.InRange(output[1], 0.74f, 0.76f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void QueueTimedHitSoundEventsInRange_MissingBody_PreservesHeadAndFallback()
    {
        string directory = CreateTempDirectory();

        try
        {
            string headPath = Path.Combine(directory, "normal-hitnormal.wav");
            WriteHalfAmplitudeWav(headPath);

            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.SliderTickRate = 1;
            beatmap.HitObjects.Add(slider);

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            var queued = Assert.Single(
                service.QueueTimedHitSoundEventsInRange(
                    beatmap, 1000, 1000, directory,
                    resolver, engine, includeStart: true));

            Assert.Equal(2, queued.Results.Count);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                queued.Results[0].Outcome);
            Assert.Equal(headPath, queued.Results[0].ResolvedPath);

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                queued.Results[1].Outcome);
            Assert.Null(queued.Results[1].ResolvedPath);
            Assert.Equal(
                "normal-sliderslide.wav",
                queued.Results[1].FallbackFilename);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void QueueTimedHitSoundEventsInRange_RepeatAndTail_DoNotQueueSliderBody()
    {
        string directory = CreateTempDirectory();

        try
        {
            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.SliderTickRate = 1;
            beatmap.HitObjects.Add(slider);

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            // No mixer: missing samples must not attempt playback.
            using var engine = new AudioPlaybackEngine();

            var queuedEvents = service.QueueTimedHitSoundEventsInRange(
                beatmap, 1000, 2000, directory, resolver, engine);

            Assert.Equal(2, queuedEvents.Count);

            Assert.Equal(1, queuedEvents[0].Event.EdgeIndex);
            Assert.Equal(1500, queuedEvents[0].Event.Time);
            Assert.Equal(2, queuedEvents[0].Results.Count);

            Assert.Equal(2, queuedEvents[1].Event.EdgeIndex);
            Assert.Equal(2000, queuedEvents[1].Event.Time);
            Assert.Single(queuedEvents[1].Results);

            Assert.All(queuedEvents, queued =>
                Assert.All(queued.Results, result =>
                    Assert.Equal(
                        SampleResolutionOutcome.ExternalFallbackRequired,
                        result.Outcome)));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public void QueueTimedHitSoundEventsInRange_SliderAtStart_RespectsIncludeStart(
        bool includeStart,
        int expectedCount)
    {
        string directory = CreateTempDirectory();

        try
        {
            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.SliderTickRate = 1;
            beatmap.HitObjects.Add(slider);

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = new AudioPlaybackEngine();

            var queuedEvents = service.QueueTimedHitSoundEventsInRange(
                beatmap, 1000, 1000, directory,
                resolver, engine, includeStart);

            Assert.Equal(expectedCount, queuedEvents.Count);

            if (!includeStart)
            {
                Assert.Empty(queuedEvents);
                return;
            }

            var queued = Assert.Single(queuedEvents);

            Assert.Equal(0, queued.Event.EdgeIndex);
            Assert.Equal(1000, queued.Event.Time);
            Assert.Equal(2, queued.Results.Count);

            Assert.All(queued.Results, result =>
                Assert.Equal(
                    SampleResolutionOutcome.ExternalFallbackRequired,
                    result.Outcome));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
    [Fact]
    public void PlaySliderBody_WithoutWhistle_LoopsForTotalSliderDuration()
    {
        string directory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(directory, "normal-sliderslide.wav");
            WriteHalfAmplitudeWav(path);

            var (beatmap, slider) = CreateTwoSpanSlider();
            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            mixer.ReadFully = true;

            List<SampleResolutionResult> results = service.PlaySliderBody(
                beatmap, slider, directory, resolver, engine);

            SampleResolutionResult result = Assert.Single(results);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);
            Assert.Equal(path, result.ResolvedPath);
            Assert.Equal(1000d, beatmap.GetSliderDuration(slider));

            const int durationFrames = 44100;
            float[] output = new float[(durationFrames + 1) * 2];

            Assert.Equal(output.Length, mixer.Read(output.AsSpan()));

            foreach (int frame in new[] { 0, 100, durationFrames - 1 })
            {
                Assert.InRange(output[frame * 2], 0.24f, 0.26f);
                Assert.InRange(output[frame * 2 + 1], 0.24f, 0.26f);
            }

            Assert.Equal(0f, output[durationFrames * 2]);
            Assert.Equal(0f, output[durationFrames * 2 + 1]);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderBody_WithWhistle_MixesBothBodyLayers()
    {
        string directory = CreateTempDirectory();

        try
        {
            string slidePath = Path.Combine(
                directory, "normal-sliderslide.wav");
            string whistlePath = Path.Combine(
                directory, "soft-sliderwhistle.wav");

            WriteHalfAmplitudeWav(slidePath);
            WriteHalfAmplitudeWav(whistlePath);

            var (beatmap, slider) = CreateTwoSpanSlider();
            slider.HitSound = 2;
            slider.HitSample.AdditionSet = 2;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results = service.PlaySliderBody(
                beatmap, slider, directory, resolver, engine);

            Assert.Equal(2, results.Count);
            Assert.Equal(slidePath, results[0].ResolvedPath);
            Assert.Equal(whistlePath, results[1].ResolvedPath);

            Assert.All(results, result =>
                Assert.Equal(
                    SampleResolutionOutcome.BeatmapSampleFound,
                    result.Outcome));

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            // Two 0.5-amplitude samples, each at 50% volume.
            Assert.InRange(output[0], 0.49f, 0.51f);
            Assert.InRange(output[1], 0.49f, 0.51f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderBody_WhenWhistleMissing_PlaysSlideAndReturnsFallback()
    {
        string directory = CreateTempDirectory();

        try
        {
            string slidePath = Path.Combine(
                directory, "normal-sliderslide.wav");

            WriteHalfAmplitudeWav(slidePath);

            var (beatmap, slider) = CreateTwoSpanSlider();
            slider.HitSound = 2;
            slider.HitSample.AdditionSet = 2;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            List<SampleResolutionResult> results = service.PlaySliderBody(
                beatmap, slider, directory, resolver, engine);

            Assert.Equal(2, results.Count);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                results[0].Outcome);
            Assert.Equal(slidePath, results[0].ResolvedPath);

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                results[1].Outcome);
            Assert.Null(results[1].ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));

            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderBody_WithDelay_StartsAndEndsAtExpectedFrames()
    {
        string directory = CreateTempDirectory();

        try
        {
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "normal-sliderslide.wav"));

            var (beatmap, slider) = CreateTwoSpanSlider();
            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            mixer.ReadFully = true;

            SampleResolutionResult result = Assert.Single(
                service.PlaySliderBody(
                    beatmap, slider, directory, resolver, engine,
                    delayMilliseconds: 10));

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);

            const int delayFrames = 441;
            const int durationFrames = 44100;
            const int endFrame = delayFrames + durationFrames;

            float[] output = new float[(endFrame + 1) * 2];
            Assert.Equal(output.Length, mixer.Read(output.AsSpan()));

            Assert.Equal(0f, output[0]);
            Assert.Equal(0f, output[(delayFrames - 1) * 2]);

            Assert.InRange(output[delayFrames * 2], 0.24f, 0.26f);
            Assert.InRange(output[(endFrame - 1) * 2], 0.24f, 0.26f);

            Assert.Equal(0f, output[endFrame * 2]);
            Assert.Equal(0f, output[endFrame * 2 + 1]);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderBody_WhenSampleIndexIsZero_DoesNotUseBeatmapFile()
    {
        string directory = CreateTempDirectory();

        try
        {
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "normal-sliderslide.wav"));

            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.TimingPoints[0].SampleIndex = 0;

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();
            using var engine = new AudioPlaybackEngine();

            SampleResolutionResult result = Assert.Single(
                service.PlaySliderBody(
                    beatmap, slider, directory, resolver, engine));

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                result.Outcome);
            Assert.Null(result.ResolvedPath);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderBody_WhenBankedSampleMissing_UsesUniversalSample()
    {
        string directory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(directory, "sliderslide.wav");
            WriteHalfAmplitudeWav(path);

            var (beatmap, slider) = CreateTwoSpanSlider();
            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            SampleResolutionResult result = Assert.Single(
                service.PlaySliderBody(
                    beatmap, slider, directory, resolver, engine));

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);
            Assert.Equal(path, result.ResolvedPath);

            float[] output = new float[2];
            Assert.Equal(2, mixer.Read(output.AsSpan()));
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PlaySliderBody_WhenDelayInvalid_ThrowsArgumentOutOfRangeException(
        double delayMilliseconds)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlaySliderBody(
                    beatmap, slider, Path.GetTempPath(),
                    resolver, engine, delayMilliseconds));

        Assert.Equal("delayMilliseconds", exception.ParamName);
        Assert.Equal(delayMilliseconds, (double)exception.ActualValue!);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void PlaySliderBody_WhenDurationInvalid_ThrowsInvalidOperationException(
        double sliderLength)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        slider.Length = sliderLength;

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                service.PlaySliderBody(
                    beatmap, slider, Path.GetTempPath(),
                    resolver, engine));

        Assert.Equal(
            "The slider body duration must be finite and greater than zero.",
            exception.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void PlaySliderBody_WhenObjectIsNotSlider_ThrowsArgumentException(
        int objectType)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        slider.ObjectType = objectType;

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.PlaySliderBody(
                    beatmap, slider, Path.GetTempPath(),
                    resolver, engine));

        Assert.Equal("slider", exception.ParamName);
    }

    [Fact]
    public void PlaySliderBody_WhenSampleExistsButOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        string directory = CreateTempDirectory();

        try
        {
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "normal-sliderslide.wav"));

            var (beatmap, slider) = CreateTwoSpanSlider();

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();
            using var engine = new AudioPlaybackEngine();

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() =>
                    service.PlaySliderBody(
                        beatmap, slider, directory, resolver, engine));

            Assert.Equal(
                "The audio output has not been initialized.",
                exception.Message);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaySliderBody_WhenElapsedTimeProvided_PreservesPhaseAndRemainingDuration()
    {
        string directory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(directory, "normal-sliderslide.wav");
            var format = new WaveFormat(44100, 16, 1);
            byte[] audio = new byte[100 * format.BlockAlign];

            for (int frame = 0; frame < 100; frame++)
            {
                audio[frame * 2] = 0;
                audio[frame * 2 + 1] = frame < 50
                    ? (byte)64
                    : (byte)192;
            }

            using (var writer = new WaveFileWriter(path, format))
            {
                writer.Write(audio, 0, audio.Length);
            }

            var (beatmap, slider) = CreateTwoSpanSlider();
            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            mixer.ReadFully = true;

            SampleResolutionResult result = Assert.Single(
                service.PlaySliderBody(
                    beatmap, slider, directory, resolver, engine,
                    delayMilliseconds: 10,
                    elapsedMilliseconds: 1));

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                result.Outcome);
            Assert.Equal(path, result.ResolvedPath);

            const int delayFrames = 441;
            const int remainingFrames = 44055;
            const int endFrame = delayFrames + remainingFrames;

            float[] output = new float[(endFrame + 1) * 2];
            Assert.Equal(output.Length, mixer.Read(output.AsSpan()));

            static void AssertFrame(float[] samples, int frame, float expected)
            {
                Assert.InRange(
                    samples[frame * 2], expected - 0.01f, expected + 0.01f);
                Assert.InRange(
                    samples[frame * 2 + 1], expected - 0.01f, expected + 0.01f);
            }

            AssertFrame(output, 0, 0f);
            AssertFrame(output, delayFrames - 1, 0f);

            // Offset: 1 ms = 44 complete source frames.
            // Source frames 44-49 contain +0.5, at 50% volume.
            AssertFrame(output, delayFrames, 0.25f);
            AssertFrame(output, delayFrames + 5, 0.25f);

            // Source frames 50-99 contain -0.5.
            AssertFrame(output, delayFrames + 6, -0.25f);
            AssertFrame(output, delayFrames + 55, -0.25f);

            // The loop wraps to source frame zero.
            AssertFrame(output, delayFrames + 56, 0.25f);

            // Playback stops after the remaining 999 ms.
            AssertFrame(output, endFrame - 1, -0.25f);
            AssertFrame(output, endFrame, 0f);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PlaySliderBody_WhenElapsedTimeIsInvalid_ThrowsArgumentOutOfRangeException(
        double elapsedMilliseconds)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlaySliderBody(
                    beatmap, slider, Path.GetTempPath(),
                    resolver, engine,
                    elapsedMilliseconds: elapsedMilliseconds));

        Assert.Equal("elapsedMilliseconds", exception.ParamName);
        Assert.Contains(
            "The elapsed slider time must be finite and non-negative.",
            exception.Message);
    }

    [Theory]
    [InlineData(1000d)]
    [InlineData(1001d)]
    public void PlaySliderBody_WhenElapsedTimeReachesOrExceedsDuration_ThrowsArgumentOutOfRangeException(
        double elapsedMilliseconds)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();
        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.PlaySliderBody(
                    beatmap, slider, Path.GetTempPath(),
                    resolver, engine,
                    elapsedMilliseconds: elapsedMilliseconds));

        Assert.Equal("elapsedMilliseconds", exception.ParamName);
        Assert.Contains(
            "The elapsed slider time must be less than the slider duration.",
            exception.Message);
    }

    [Fact]
    public void QueueActiveSliderBodiesAt_WhenSliderIsActive_PlaysOnlyRemainingBody()
    {
        string directory = CreateTempDirectory();

        try
        {
            string bodyPath = Path.Combine(directory, "normal-sliderslide.wav");
            string headPath = Path.Combine(directory, "normal-hitnormal.wav");

            WriteHalfAmplitudeWav(bodyPath);
            WriteHalfAmplitudeWav(headPath);

            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.HitObjects.Add(slider);

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            mixer.ReadFully = true;

            var queued = Assert.Single(service.QueueActiveSliderBodiesAt(
                beatmap, 1500, directory, resolver, engine));

            Assert.Same(slider, queued.Slider);

            SampleResolutionResult resolution = Assert.Single(queued.Results);

            Assert.Equal(
                SampleResolutionOutcome.BeatmapSampleFound,
                resolution.Outcome);
            Assert.Equal(bodyPath, resolution.ResolvedPath);

            const int remainingFrames = 22050;
            float[] output = new float[(remainingFrames + 1) * 2];

            Assert.Equal(output.Length, mixer.Read(output.AsSpan()));

            // Only the continuous body is played, not the slider head.
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[1], 0.24f, 0.26f);

            Assert.InRange(
                output[(remainingFrames - 1) * 2],
                0.24f, 0.26f);

            Assert.Equal(0f, output[remainingFrames * 2]);
            Assert.Equal(0f, output[remainingFrames * 2 + 1]);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void QueueActiveSliderBodiesAt_WithOverlappingSliders_PlaysBothRemainingBodies()
    {
        string directory = CreateTempDirectory();

        try
        {
            string bodyPath = Path.Combine(directory, "normal-sliderslide.wav");
            WriteHalfAmplitudeWav(bodyPath);

            var (beatmap, firstSlider) = CreateTwoSpanSlider();

            var secondSlider = new HitObject
            {
                ObjectType = 2,
                Time = 1200,
                Length = 140,
                Slides = 2
            };

            beatmap.HitObjects.Add(firstSlider);
            beatmap.HitObjects.Add(secondSlider);

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            mixer.ReadFully = true;

            var results = service.QueueActiveSliderBodiesAt(
                beatmap, 1500, directory, resolver, engine);

            Assert.Equal(2, results.Count);
            Assert.Same(firstSlider, results[0].Slider);
            Assert.Same(secondSlider, results[1].Slider);

            Assert.All(results, item =>
            {
                SampleResolutionResult resolution = Assert.Single(item.Results);

                Assert.Equal(
                    SampleResolutionOutcome.BeatmapSampleFound,
                    resolution.Outcome);
                Assert.Equal(bodyPath, resolution.ResolvedPath);
            });

            const int firstEndFrame = 22050;
            const int secondEndFrame = 30870;

            float[] output = new float[(secondEndFrame + 1) * 2];

            Assert.Equal(output.Length, mixer.Read(output.AsSpan()));

            // Both sliders are active at the interval start.
            Assert.InRange(output[0], 0.49f, 0.51f);

            // First slider ends after 500 ms; second continues.
            Assert.InRange(
                output[firstEndFrame * 2],
                0.24f, 0.26f);

            // Second slider ends after 700 ms.
            Assert.Equal(0f, output[secondEndFrame * 2]);
            Assert.Equal(0f, output[secondEndFrame * 2 + 1]);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(999d)]
    [InlineData(1000d)]
    [InlineData(2000d)]
    [InlineData(2001d)]
    public void QueueActiveSliderBodiesAt_WhenSliderIsNotStrictlyActive_ReturnsEmpty(
        double startMilliseconds)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        beatmap.HitObjects.Add(slider);

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        var results = service.QueueActiveSliderBodiesAt(
            beatmap, startMilliseconds, "beatmap", resolver, engine);

        Assert.Empty(results);
    }

    [Fact]
    public void QueueActiveSliderBodiesAt_WhenBodySampleMissing_ReturnsFallback()
    {
        string directory = CreateTempDirectory();

        try
        {
            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.HitObjects.Add(slider);

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = new AudioPlaybackEngine();

            var queued = Assert.Single(service.QueueActiveSliderBodiesAt(
                beatmap, 1500, directory, resolver, engine));

            Assert.Same(slider, queued.Slider);

            SampleResolutionResult resolution = Assert.Single(queued.Results);

            Assert.Equal(
                SampleResolutionOutcome.ExternalFallbackRequired,
                resolution.Outcome);
            Assert.Null(resolution.ResolvedPath);
            Assert.Equal(
                "normal-sliderslide.wav",
                resolution.FallbackFilename);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void QueueActiveSliderBodiesAt_WhenStartIsInvalid_ThrowsArgumentOutOfRangeException(
        double startMilliseconds)
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.QueueActiveSliderBodiesAt(
                    new Beatmap(), startMilliseconds,
                    "beatmap", resolver, engine));

        Assert.Equal("timeMilliseconds", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void QueueActiveSliderBodiesAt_WhenDirectoryIsInvalid_ThrowsArgumentException(
        string? directory)
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.QueueActiveSliderBodiesAt(
                    new Beatmap(), 1500, directory!,
                    resolver, engine));

        Assert.Equal("beatmapDirectory", exception.ParamName);
    }

    [Fact]
    public void QueueActiveSliderBodiesAt_WhenBeatmapIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.QueueActiveSliderBodiesAt(
                    null!, 1500, "beatmap", resolver, engine));

        Assert.Equal("beatmap", exception.ParamName);
    }

    [Fact]
    public void QueueActiveSliderBodiesAt_WhenResolverIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();

        using var engine = new AudioPlaybackEngine();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.QueueActiveSliderBodiesAt(
                    new Beatmap(), 1500, "beatmap", null!, engine));

        Assert.Equal("resolver", exception.ParamName);
    }

    [Fact]
    public void QueueActiveSliderBodiesAt_WhenEngineIsNull_ThrowsArgumentNullException()
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.QueueActiveSliderBodiesAt(
                    new Beatmap(), 1500, "beatmap", resolver, null!));

        Assert.Equal("engine", exception.ParamName);
    }

    [Fact]
    public void QueuePlaybackInterval_InsideSlider_PlaysRemainingBodyWithoutRestartingAtRepeatOrTail()
    {
        string directory = CreateTempDirectory();

        try
        {
            string bodyPath = Path.Combine(directory, "normal-sliderslide.wav");
            WriteHalfAmplitudeWav(bodyPath);

            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.SliderTickRate = 1;
            beatmap.HitObjects.Add(slider);

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            mixer.ReadFully = true;

            var result = service.QueuePlaybackInterval(
                beatmap, 1250, 2000, directory, resolver, engine);

            var active = Assert.Single(result.ActiveSliderBodies);
            Assert.Same(slider, active.Slider);
            Assert.Equal(bodyPath, Assert.Single(active.Results).ResolvedPath);

            Assert.Equal(2, result.DiscreteEvents.Count);
            Assert.Equal(1, result.DiscreteEvents[0].Event.EdgeIndex);
            Assert.Equal(2, result.DiscreteEvents[1].Event.EdgeIndex);

            const int remainingFrames = 33075;
            const int repeatFrame = 11025;

            float[] output = new float[(remainingFrames + 1) * 2];
            Assert.Equal(output.Length, mixer.Read(output.AsSpan()));

            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[repeatFrame * 2], 0.24f, 0.26f);
            Assert.InRange(
                output[(remainingFrames - 1) * 2],
                0.24f, 0.26f);

            Assert.Equal(0f, output[remainingFrames * 2]);
            Assert.Equal(0f, output[remainingFrames * 2 + 1]);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void QueuePlaybackInterval_AtSliderHead_RespectsIncludeStartWithoutDuplicatingBody(
        bool includeStart)
    {
        string directory = CreateTempDirectory();

        try
        {
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "normal-hitnormal.wav"));
            WriteHalfAmplitudeWav(
                Path.Combine(directory, "normal-sliderslide.wav"));

            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.SliderTickRate = 1;
            beatmap.HitObjects.Add(slider);

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            mixer.ReadFully = true;

            var result = service.QueuePlaybackInterval(
                beatmap, 1000, 1000, directory, resolver, engine,
                includeStart);

            Assert.Empty(result.ActiveSliderBodies);

            if (!includeStart)
            {
                Assert.Empty(result.DiscreteEvents);

                float[] silentFrame = new float[2];
                Assert.Equal(2, mixer.Read(silentFrame.AsSpan()));
                Assert.Equal(0f, silentFrame[0]);
                Assert.Equal(0f, silentFrame[1]);
                return;
            }

            var head = Assert.Single(result.DiscreteEvents);

            Assert.Equal(HitSoundEventType.SliderEdge, head.Event.EventType);
            Assert.Equal(0, head.Event.EdgeIndex);
            Assert.Equal(2, head.Results.Count);

            const int durationFrames = 44100;
            float[] output = new float[(durationFrames + 1) * 2];

            Assert.Equal(output.Length, mixer.Read(output.AsSpan()));

            // Head and one continuous body start together.
            Assert.InRange(output[0], 0.49f, 0.51f);

            // The head sample ends, leaving only the body.
            Assert.InRange(output[100 * 2], 0.24f, 0.26f);

            // One body ends after the original slider duration.
            Assert.Equal(0f, output[durationFrames * 2]);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void QueuePlaybackInterval_WhenNewSliderStarts_KeepsRecoveredBodySeparate()
    {
        string directory = CreateTempDirectory();

        try
        {
            string bodyPath = Path.Combine(directory, "normal-sliderslide.wav");
            WriteHalfAmplitudeWav(bodyPath);

            var (beatmap, firstSlider) = CreateTwoSpanSlider();
            var (_, secondSlider) = CreateTwoSpanSlider();

            secondSlider.Time = 1800;

            beatmap.SliderTickRate = 1;
            beatmap.HitObjects.Add(firstSlider);
            beatmap.HitObjects.Add(secondSlider);

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            mixer.ReadFully = true;

            var result = service.QueuePlaybackInterval(
                beatmap, 1500, 1800, directory, resolver, engine);

            var active = Assert.Single(result.ActiveSliderBodies);
            Assert.Same(firstSlider, active.Slider);
            Assert.Equal(bodyPath, Assert.Single(active.Results).ResolvedPath);

            var discrete = Assert.Single(result.DiscreteEvents);
            Assert.Same(secondSlider, discrete.Event.HitObject);
            Assert.Equal(0, discrete.Event.EdgeIndex);

            Assert.All(discrete.Results, resolution =>
                Assert.Equal(
                    SampleResolutionOutcome.ExternalFallbackRequired,
                    resolution.Outcome));

            const int remainingFrames = 22050;
            const int newHeadFrame = 13230;

            float[] output = new float[(remainingFrames + 1) * 2];
            Assert.Equal(output.Length, mixer.Read(output.AsSpan()));

            // Only the first slider's available body contributes audio.
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[newHeadFrame * 2], 0.24f, 0.26f);
            Assert.Equal(0f, output[remainingFrames * 2]);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void QueuePlaybackInterval_InsideSlider_PreservesOriginalSamplePhase()
    {
        string directory = CreateTempDirectory();

        try
        {
            string bodyPath = Path.Combine(directory, "normal-sliderslide.wav");
            var format = new WaveFormat(44100, 16, 1);
            byte[] audio = new byte[100 * format.BlockAlign];

            for (int frame = 0; frame < 100; frame++)
            {
                audio[frame * 2] = 0;
                audio[frame * 2 + 1] =
                    frame < 50 ? (byte)64 : (byte)192;
            }

            using (var writer = new WaveFileWriter(bodyPath, format))
            {
                writer.Write(audio, 0, audio.Length);
            }

            var (beatmap, slider) = CreateTwoSpanSlider();
            beatmap.SliderTickRate = 1;
            beatmap.HitObjects.Add(slider);

            var service = new HitSoundPlaybackService();
            var resolver = new SampleResolver();

            using var engine = CreateEngineWithMixer(
                out MixingSampleProvider mixer);

            mixer.ReadFully = true;

            var result = service.QueuePlaybackInterval(
                beatmap, 1001, 1001, directory, resolver, engine);

            Assert.Empty(result.DiscreteEvents);

            var active = Assert.Single(result.ActiveSliderBodies);
            Assert.Same(slider, active.Slider);
            Assert.Equal(bodyPath, Assert.Single(active.Results).ResolvedPath);

            const int remainingFrames = 44055;
            float[] output = new float[(remainingFrames + 1) * 2];

            Assert.Equal(output.Length, mixer.Read(output.AsSpan()));

            // The one-millisecond offset starts at source frame 44.
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[5 * 2], 0.24f, 0.26f);

            // Source frame 50 changes polarity.
            Assert.InRange(output[6 * 2], -0.26f, -0.24f);

            // The sample wraps to its original first frame.
            Assert.InRange(output[56 * 2], 0.24f, 0.26f);

            Assert.Equal(0f, output[remainingFrames * 2]);
            Assert.Equal(0f, output[remainingFrames * 2 + 1]);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public void QueuePlaybackInterval_ZeroLengthIntervalInsideSlider_SeparatesBodyAndEvents(
        bool includeStart,
        int expectedEventCount)
    {
        var (beatmap, slider) = CreateTwoSpanSlider();
        beatmap.SliderTickRate = 1;
        beatmap.HitObjects.Add(slider);

        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        var result = service.QueuePlaybackInterval(
            beatmap, 1500, 1500, "beatmap",
            resolver, engine, includeStart);

        var active = Assert.Single(result.ActiveSliderBodies);
        Assert.Same(slider, active.Slider);

        Assert.Equal(
            SampleResolutionOutcome.ExternalFallbackRequired,
            Assert.Single(active.Results).Outcome);

        Assert.Equal(expectedEventCount, result.DiscreteEvents.Count);

        if (includeStart)
        {
            var repeat = Assert.Single(result.DiscreteEvents);
            Assert.Equal(1, repeat.Event.EdgeIndex);
        }
    }

    [Fact]
    public void QueuePlaybackInterval_WhenBeatmapIsEmpty_ReturnsBothCollectionsEmpty()
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        var result = service.QueuePlaybackInterval(
            new Beatmap(), 0, 2000, "beatmap", resolver, engine);

        Assert.Empty(result.DiscreteEvents);
        Assert.Empty(result.ActiveSliderBodies);
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void QueuePlaybackInterval_WhenStartIsInvalid_ThrowsBeforeQueueing(
        double startMilliseconds)
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.QueuePlaybackInterval(
                    new Beatmap(), startMilliseconds, 2000,
                    "beatmap", resolver, engine));

        Assert.Equal("startMilliseconds", exception.ParamName);
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(1000d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void QueuePlaybackInterval_WhenEndIsInvalid_ThrowsBeforeQueueing(
        double endMilliseconds)
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.QueuePlaybackInterval(
                    new Beatmap(), 1500, endMilliseconds,
                    "beatmap", resolver, engine));

        Assert.Equal("endMilliseconds", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void QueuePlaybackInterval_WhenDirectoryIsInvalid_ThrowsArgumentException(
        string? directory)
    {
        var service = new HitSoundPlaybackService();
        var resolver = new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                service.QueuePlaybackInterval(
                    new Beatmap(), 0, 1000, directory!,
                    resolver, engine));

        Assert.Equal("beatmapDirectory", exception.ParamName);
    }

    [Theory]
    [InlineData("beatmap")]
    [InlineData("resolver")]
    [InlineData("engine")]
    public void QueuePlaybackInterval_WhenDependencyIsNull_ThrowsArgumentNullException(
        string argumentName)
    {
        var service = new HitSoundPlaybackService();

        Beatmap? beatmap =
            argumentName == "beatmap" ? null : new Beatmap();

        SampleResolver? resolver =
            argumentName == "resolver" ? null : new SampleResolver();

        using var engine = new AudioPlaybackEngine();

        AudioPlaybackEngine? selectedEngine =
            argumentName == "engine" ? null : engine;

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                service.QueuePlaybackInterval(
                    beatmap!, 0, 1000, "beatmap",
                    resolver!, selectedEngine!));

        Assert.Equal(argumentName, exception.ParamName);
    }
}
