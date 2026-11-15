using System.Reflection;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OsuHitsoundEditor.Tests;

public class AudioPlaybackEngineTests
{
    [Fact]
    public void OpenAudioFile_WhenFileDoesNotExist_ThrowsFileNotFoundException()
    {
        var engine = new AudioPlaybackEngine();

        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

        var exception =
            Assert.Throws<FileNotFoundException>(() => engine.OpenAudioFile(path));

        Assert.Equal(
            "The audio file does not exist.",
            exception.Message);

        Assert.Equal(
            path,
            exception.FileName);
    }

    [Fact]
    public void OpenAudioFile_WhenExtensionIsUnsupported_ThrowsNotSupportedException()
    {
        var engine = new AudioPlaybackEngine();

        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.txt");

        File.WriteAllText(path, "test");

        try
        {
            var exception =
                Assert.Throws<NotSupportedException>(() => engine.OpenAudioFile(path));

            Assert.Equal(
                "The audio format '.txt' is not supported.",
                exception.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void OpenAudioFile_WhenFileIsWav_ReturnsAudioFileReader()
    {
        var engine = new AudioPlaybackEngine();

        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

        var format =
            new WaveFormat(
                44100,
                16,
                1);

        using (var writer =
               new WaveFileWriter(path, format))
        {
            var silence =
                new byte[format.AverageBytesPerSecond / 10];

            writer.Write(
                silence,
                0,
                silence.Length);
        }

        try
        {
            using var reader =
                engine.OpenAudioFile(path);

            Assert.IsType<AudioFileReader>(reader);
            Assert.Equal(
                44100,
                reader.WaveFormat.SampleRate);
            Assert.Equal(
                1,
                reader.WaveFormat.Channels);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void NormalizeForMixer_WhenInputIsStereoAtTargetRate_KeepsFormat()
    {
        var engine = new AudioPlaybackEngine();

        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

        var format = new WaveFormat(
            44100,
            16,
            2);

        using (var writer = new WaveFileWriter(path, format))
        {
            var audio = new byte[format.BlockAlign * 10];
            writer.Write(audio, 0, audio.Length);
        }

        try
        {
            using var reader = new WaveFileReader(path);

            var provider =
                engine.NormalizeForMixer(reader, 44100);

            Assert.Equal(2, provider.WaveFormat.Channels);
            Assert.Equal(44100, provider.WaveFormat.SampleRate);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void NormalizeForMixer_WhenInputIsMono_ConvertsToStereo()
    {
        var engine = new AudioPlaybackEngine();

        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

        var format = new WaveFormat(
            44100,
            16,
            1);

        using (var writer = new WaveFileWriter(path, format))
        {
            var audio = new byte[format.BlockAlign * 10];
            writer.Write(audio, 0, audio.Length);
        }

        try
        {
            using var reader = new WaveFileReader(path);

            var provider =
                engine.NormalizeForMixer(reader, 44100);

            Assert.Equal(2, provider.WaveFormat.Channels);
            Assert.Equal(44100, provider.WaveFormat.SampleRate);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void NormalizeForMixer_WhenInputHasMoreThanTwoChannels_ConvertsToStereo()
    {
        var engine = new AudioPlaybackEngine();

        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

        var format = new WaveFormat(
            44100,
            16,
            4);

        using (var writer = new WaveFileWriter(path, format))
        {
            var audio = new byte[format.BlockAlign * 10];
            writer.Write(audio, 0, audio.Length);
        }

        try
        {
            using var reader = new WaveFileReader(path);

            var provider =
                engine.NormalizeForMixer(reader, 44100);

            Assert.Equal(2, provider.WaveFormat.Channels);
            Assert.Equal(44100, provider.WaveFormat.SampleRate);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void NormalizeForMixer_WhenSampleRateDiffers_ResamplesToTargetRate()
    {
        var engine = new AudioPlaybackEngine();

        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

        var format = new WaveFormat(
            22050,
            16,
            2);

        using (var writer = new WaveFileWriter(path, format))
        {
            var audio = new byte[format.BlockAlign * 10];
            writer.Write(audio, 0, audio.Length);
        }

        try
        {
            using var reader = new WaveFileReader(path);

            var provider =
                engine.NormalizeForMixer(reader, 48000);

            Assert.Equal(2, provider.WaveFormat.Channels);
            Assert.Equal(48000, provider.WaveFormat.SampleRate);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Play_WhenOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        var engine = new AudioPlaybackEngine();

        var exception =
            Assert.Throws<InvalidOperationException>(() => engine.Play());

        Assert.Equal(
            "The audio output has not been initialized.",
            exception.Message);
    }

    [Fact]
    public void Pause_WhenOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        var engine = new AudioPlaybackEngine();

        var exception =
            Assert.Throws<InvalidOperationException>(() => engine.Pause());

        Assert.Equal(
            "The audio output has not been initialized.",
            exception.Message);
    }

    [Fact]
    public void Stop_WhenOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        var engine = new AudioPlaybackEngine();

        var exception =
            Assert.Throws<InvalidOperationException>(() => engine.Stop());

        Assert.Equal(
            "The audio output has not been initialized.",
            exception.Message);
    }

    [Fact]
    public void AddAudioSource_WhenOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        var engine = new AudioPlaybackEngine();

        var exception =
            Assert.Throws<InvalidOperationException>(() => engine.AddAudioSource("audio.wav"));

        Assert.Equal(
            "The audio output has not been initialized.",
            exception.Message);
    }

    [Fact]
    public void LoadTimelineAudio_WhenOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        var engine = new AudioPlaybackEngine();

        var exception =
            Assert.Throws<InvalidOperationException>(() => engine.LoadTimelineAudio("audio.wav"));

        Assert.Equal(
            "The audio output has not been initialized.",
            exception.Message);
    }

    [Fact]
    public void Seek_WhenOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        using var engine = new AudioPlaybackEngine();

        var exception =
            Assert.Throws<InvalidOperationException>(() => engine.Seek(1000));

        Assert.Equal(
            "The audio output has not been initialized.",
            exception.Message);
    }

    [Fact]
    public void GetTimelinePositionMilliseconds_WhenOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        using var engine = new AudioPlaybackEngine();

        var exception =
            Assert.Throws<InvalidOperationException>(() => engine.GetTimelinePositionMilliseconds());

        Assert.Equal(
            "The audio output has not been initialized.",
            exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void AddAudioSource_WhenVolumeIsInvalid_ThrowsArgumentOutOfRangeException(
        int volume)
    {
        using var engine = new AudioPlaybackEngine();

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.AddAudioSource("audio.wav", volume));

        Assert.Equal("volume", exception.ParamName);
        Assert.Equal(volume, exception.ActualValue);

        Assert.Contains(
            "The volume must be between 0 and 100.",
            exception.Message);
    }

    [Theory]
    [InlineData(0, 0.0f)]
    [InlineData(50, 0.25f)]
    [InlineData(100, 0.5f)]
    public void AddAudioSource_WhenVolumeIsValid_ScalesAudioSamples(
        int volume,
        float expectedSample)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

        var format = new WaveFormat(44100, 16, 1);

        using (var writer = new WaveFileWriter(path, format))
        {
            var audio = new byte[format.BlockAlign * 100];

            for (var i = 0; i < audio.Length; i += 2)
            {
                audio[i] = 0;
                audio[i + 1] = 64;
            }

            writer.Write(audio, 0, audio.Length);
        }

        try
        {
            using var engine = new AudioPlaybackEngine();

            var mixer = new MixingSampleProvider(
                WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));

            var mixerField =
                typeof(AudioPlaybackEngine).GetField(
                    "mixer",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(mixerField);

            mixerField.SetValue(engine, mixer);

            engine.AddAudioSource(path, volume);

            var output = new float[2];

            var samplesRead =
                mixer.Read(output.AsSpan());

            Assert.Equal(2, samplesRead);

            Assert.InRange(
                output[0],
                expectedSample - 0.01f,
                expectedSample + 0.01f);

            Assert.InRange(
                output[1],
                expectedSample - 0.01f,
                expectedSample + 0.01f);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AddAudioSource_WhenTwoSamplesOverlap_MixesTheirVolumes()
    {
        string[] paths =
        {
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav"),
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav")
        };

        try
        {
            using var engine = new AudioPlaybackEngine();

            var format = new WaveFormat(44100, 16, 1);

            foreach (var path in paths)
            {
                using var writer = new WaveFileWriter(path, format);

                var audio = new byte[format.BlockAlign * 100];

                for (var i = 0; i < audio.Length; i += 2)
                {
                    audio[i] = 0;
                    audio[i + 1] = 64;
                }

                writer.Write(audio, 0, audio.Length);
            }

            var mixer = new MixingSampleProvider(
                WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));

            var mixerField =
                typeof(AudioPlaybackEngine).GetField(
                    "mixer",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(mixerField);
            mixerField.SetValue(engine, mixer);

            engine.AddAudioSource(paths[0], 50);
            engine.AddAudioSource(paths[1], 50);

            var output = new float[2];

            var samplesRead = mixer.Read(output.AsSpan());

            Assert.Equal(2, samplesRead);

            Assert.InRange(output[0], 0.49f, 0.51f);
            Assert.InRange(output[1], 0.49f, 0.51f);
        }
        finally
        {
            foreach (var path in paths) File.Delete(path);
        }
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AddAudioSource_WhenDelayIsInvalid_ThrowsArgumentOutOfRangeException(
        double invalidDelay)
    {
        using var engine = new AudioPlaybackEngine();

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                engine.AddAudioSource(
                    "audio.wav", 100, invalidDelay));

        Assert.Equal("delayMilliseconds", exception.ParamName);
        Assert.Contains(
            "The audio delay must be finite and non-negative.",
            exception.Message);
    }

    [Theory]
    [InlineData(30_000_000.0)]
    [InlineData(double.MaxValue)]
    public void AddAudioSource_WhenDelayExceedsSupportedRange_ThrowsBeforeOpeningFile(
        double invalidDelay)
    {
        using var engine = new AudioPlaybackEngine();

        var mixer = new MixingSampleProvider(
            WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));

        var mixerField =
            typeof(AudioPlaybackEngine).GetField(
                "mixer",
                BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(mixerField);
        mixerField.SetValue(engine, mixer);

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                engine.AddAudioSource(
                    "nonexistent-audio.wav", 100, invalidDelay));

        Assert.Equal("delayMilliseconds", exception.ParamName);
        Assert.Contains(
            "The audio delay exceeds the supported sample range.",
            exception.Message);
    }

    [Fact]
    public void AddAudioSource_WhenDelayIsApplied_MixesAndReleasesDelayedSource()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

        try
        {
            var format = new WaveFormat(44100, 16, 1);

            using (var writer = new WaveFileWriter(path, format))
            {
                var audio = new byte[format.BlockAlign * 100];

                for (var i = 0; i < audio.Length; i += 2)
                {
                    audio[i] = 0;
                    audio[i + 1] = 64;
                }

                writer.Write(audio, 0, audio.Length);
            }

            using var engine = new AudioPlaybackEngine();

            var mixer = new MixingSampleProvider(
                WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
            {
                ReadFully = true
            };

            var mixerField =
                typeof(AudioPlaybackEngine).GetField(
                    "mixer",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(mixerField);
            mixerField.SetValue(engine, mixer);
            var handlerMethod =
                typeof(AudioPlaybackEngine).GetMethod(
                    "OnMixerInputEnded",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(handlerMethod);

            var handler =
                (EventHandler<SampleProviderEventArgs>)Delegate.CreateDelegate(
                    typeof(EventHandler<SampleProviderEventArgs>),
                    engine,
                    handlerMethod);

            mixer.MixerInputEnded += handler;

            var readersField =
                typeof(AudioPlaybackEngine).GetField(
                    "activeReaders",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(readersField);

            var activeReaders =
                Assert.IsType<Dictionary<ISampleProvider, WaveStream>>(
                    readersField.GetValue(engine));

            // Two independent readers for the same physical file.
            engine.AddAudioSource(path, 50);

            engine.AddAudioSource(
                path,
                50,
                1);

            Assert.Equal(2, activeReaders.Count);

            // 1 ms at 44100 Hz is truncated to 44 frames.
            // Each audio source contains 100 frames.
            var output = new float[144 * 2];

            var samplesRead = mixer.Read(output.AsSpan());

            Assert.Equal(output.Length, samplesRead);

            // Before the delayed source begins.
            Assert.InRange(output[0], 0.24f, 0.26f);
            Assert.InRange(output[43 * 2], 0.24f, 0.26f);

            // Both sources overlap.
            Assert.InRange(output[44 * 2], 0.49f, 0.51f);
            Assert.InRange(output[99 * 2], 0.49f, 0.51f);

            // Only the delayed source remains.
            Assert.InRange(output[100 * 2], 0.24f, 0.26f);
            Assert.InRange(output[143 * 2], 0.24f, 0.26f);

            // Mono input was normalized to stereo.
            Assert.Equal(output[44 * 2], output[44 * 2 + 1]);

            // Trigger end-of-input cleanup for the delayed provider.
            var remaining = new float[2];
            mixer.Read(remaining.AsSpan());

            Assert.Empty(activeReaders);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void AddLoopingAudioSource_WhenVolumeIsInvalid_ThrowsArgumentOutOfRangeException(
        int volume)
    {
        using AudioPlaybackEngine engine = new();

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                engine.AddLoopingAudioSource("audio.wav", 100, volume));

        Assert.Equal("volume", exception.ParamName);
        Assert.Equal(volume, exception.ActualValue);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AddLoopingAudioSource_WhenDurationIsInvalid_ThrowsArgumentOutOfRangeException(
        double durationMilliseconds)
    {
        using AudioPlaybackEngine engine = new();

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                engine.AddLoopingAudioSource("audio.wav", durationMilliseconds));

        Assert.Equal("durationMilliseconds", exception.ParamName);
        Assert.Contains(
            "The loop duration must be finite and positive.",
            exception.Message);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AddLoopingAudioSource_WhenDelayIsInvalid_ThrowsArgumentOutOfRangeException(
        double delayMilliseconds)
    {
        using AudioPlaybackEngine engine = new();

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                engine.AddLoopingAudioSource(
                    "audio.wav", 100, 100, delayMilliseconds));

        Assert.Equal("delayMilliseconds", exception.ParamName);
        Assert.Contains(
            "The audio delay must be finite and non-negative.",
            exception.Message);
    }

    [Fact]
    public void AddLoopingAudioSource_WhenOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        using AudioPlaybackEngine engine = new();

        var exception =
            Assert.Throws<InvalidOperationException>(() =>
                engine.AddLoopingAudioSource("audio.wav", 100));

        Assert.Equal(
            "The audio output has not been initialized.",
            exception.Message);
    }

    [Theory]
    [InlineData(0.001, "The loop duration must produce at least one audio frame.")]
    [InlineData(double.MaxValue, "The loop duration exceeds the supported sample range.")]
    public void AddLoopingAudioSource_WhenDurationCannotBeRepresented_ThrowsBeforeOpeningFile(
        double durationMilliseconds,
        string expectedMessage)
    {
        using AudioPlaybackEngine engine = new();

        MixingSampleProvider mixer = new(
            WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));

        var mixerField = typeof(AudioPlaybackEngine).GetField(
            "mixer",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(mixerField);
        mixerField.SetValue(engine, mixer);

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                engine.AddLoopingAudioSource(
                    "nonexistent-audio.wav",
                    durationMilliseconds));

        Assert.Equal("durationMilliseconds", exception.ParamName);
        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public void AddLoopingAudioSource_WhenDelayExceedsSupportedRange_ThrowsBeforeOpeningFile()
    {
        using AudioPlaybackEngine engine = new();

        MixingSampleProvider mixer = new(
            WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));

        var mixerField = typeof(AudioPlaybackEngine).GetField(
            "mixer",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(mixerField);
        mixerField.SetValue(engine, mixer);

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                engine.AddLoopingAudioSource(
                    "nonexistent-audio.wav",
                    100,
                    delayMilliseconds: double.MaxValue));

        Assert.Equal("delayMilliseconds", exception.ParamName);
        Assert.Contains(
            "The audio delay exceeds the supported sample range.",
            exception.Message);
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(1.0, 44)]
    public void AddLoopingAudioSource_WhenDurationEnds_MixesAndReleasesReader(
        double delayMilliseconds,
        int expectedDelayFrames)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

        WaveFormat format = new(44100, 16, 1);

        using (WaveFileWriter writer = new(path, format))
        {
            byte[] audio = { 0, 64, 0, 32 };
            writer.Write(audio, 0, audio.Length);
        }

        try
        {
            using AudioPlaybackEngine engine = new();

            MixingSampleProvider mixer = new(
                WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
            {
                ReadFully = true
            };

            var mixerField = typeof(AudioPlaybackEngine).GetField(
                "mixer",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(mixerField);
            mixerField.SetValue(engine, mixer);

            var handlerMethod = typeof(AudioPlaybackEngine).GetMethod(
                "OnMixerInputEnded",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(handlerMethod);

            var handler =
                (EventHandler<SampleProviderEventArgs>)Delegate.CreateDelegate(
                    typeof(EventHandler<SampleProviderEventArgs>),
                    engine,
                    handlerMethod);

            mixer.MixerInputEnded += handler;

            var readersField = typeof(AudioPlaybackEngine).GetField(
                "activeReaders",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(readersField);

            var activeReaders =
                Assert.IsType<Dictionary<ISampleProvider, WaveStream>>(
                    readersField.GetValue(engine));

            engine.AddLoopingAudioSource(
                path,
                2,
                50,
                delayMilliseconds);

            Assert.Single(activeReaders);

            // 2 ms at 44100 Hz becomes 88 complete frames.
            var durationFrames = 88;
            var totalFrames = expectedDelayFrames + durationFrames + 2;

            var output = new float[totalFrames * 2];
            var samplesRead = mixer.Read(output.AsSpan());

            Assert.Equal(output.Length, samplesRead);

            if (expectedDelayFrames > 0)
            {
                Assert.Equal(0f, output[0]);
                Assert.Equal(
                    0f,
                    output[(expectedDelayFrames - 1) * 2]);
            }

            // Two-frame source: 0.5, 0.25.
            // At 50% volume: 0.25, 0.125.
            var firstFrame = expectedDelayFrames;

            Assert.InRange(
                output[firstFrame * 2],
                0.24f,
                0.26f);

            Assert.InRange(
                output[(firstFrame + 1) * 2],
                0.12f,
                0.13f);

            // Verify repetition and stereo normalization.
            Assert.InRange(
                output[(firstFrame + 2) * 2],
                0.24f,
                0.26f);

            Assert.Equal(
                output[firstFrame * 2],
                output[firstFrame * 2 + 1]);

            // The last permitted frame still contains audio.
            Assert.InRange(
                output[(firstFrame + 87) * 2],
                0.12f,
                0.13f);

            // Audio ends at the requested frame boundary.
            Assert.Equal(
                0f,
                output[(firstFrame + 88) * 2]);

            // Ensure the mixer observes end-of-input.
            mixer.Read(new float[2].AsSpan());

            Assert.Empty(activeReaders);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AddLoopingAudioSource_WhenEngineIsDisposed_ReleasesActiveReader()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

        WaveFormat format = new(44100, 16, 1);

        using (WaveFileWriter writer = new(path, format))
        {
            byte[] audio = { 0, 64, 0, 32 };
            writer.Write(audio, 0, audio.Length);
        }

        try
        {
            using AudioPlaybackEngine engine = new();

            MixingSampleProvider mixer = new(
                WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));

            var mixerField = typeof(AudioPlaybackEngine).GetField(
                "mixer",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(mixerField);
            mixerField.SetValue(engine, mixer);

            var readersField = typeof(AudioPlaybackEngine).GetField(
                "activeReaders",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(readersField);

            var activeReaders =
                Assert.IsType<Dictionary<ISampleProvider, WaveStream>>(
                    readersField.GetValue(engine));

            engine.AddLoopingAudioSource(
                path,
                1000);

            Assert.Single(activeReaders);

            engine.Dispose();

            Assert.Empty(activeReaders);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AddLoopingAudioSource_WhenSourceOffsetIsInvalid_ThrowsArgumentOutOfRangeException(
        double sourceOffsetMilliseconds)
    {
        using AudioPlaybackEngine engine = new();

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                engine.AddLoopingAudioSource(
                    "audio.wav",
                    100,
                    sourceOffsetMilliseconds: sourceOffsetMilliseconds));

        Assert.Equal("sourceOffsetMilliseconds", exception.ParamName);
        Assert.Contains(
            "The loop source offset must be finite and non-negative.",
            exception.Message);
    }

    [Fact]
    public void AddLoopingAudioSource_WhenOffsetIsProvided_PreservesPhaseAndDuration()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

        WaveFormat format = new(44100, 16, 1);

        var audio = new byte[100 * format.BlockAlign];

        for (var frame = 0; frame < 100; frame++)
        {
            audio[frame * 2] = 0;
            audio[frame * 2 + 1] = frame < 50
                ? (byte)64
                : (byte)192;
        }

        using (WaveFileWriter writer = new(path, format))
        {
            writer.Write(audio, 0, audio.Length);
        }

        try
        {
            using AudioPlaybackEngine engine = new();

            MixingSampleProvider mixer = new(
                WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
            {
                ReadFully = true
            };

            var mixerField =
                typeof(AudioPlaybackEngine).GetField(
                    "mixer",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(mixerField);
            mixerField.SetValue(engine, mixer);

            engine.AddLoopingAudioSource(
                path,
                2,
                sourceOffsetMilliseconds: 1);

            var output = new float[90 * 2];

            Assert.Equal(
                output.Length,
                mixer.Read(output.AsSpan()));

            void AssertStereoFrame(int frame, float expected)
            {
                Assert.InRange(
                    output[frame * 2],
                    expected - 0.01f,
                    expected + 0.01f);

                Assert.InRange(
                    output[frame * 2 + 1],
                    expected - 0.01f,
                    expected + 0.01f);
            }

            // 1 ms = 44 complete source frames.
            // Source frames 44-49 contain +0.5.
            AssertStereoFrame(0, 0.5f);
            AssertStereoFrame(5, 0.5f);

            // Source frames 50-99 contain -0.5.
            AssertStereoFrame(6, -0.5f);
            AssertStereoFrame(55, -0.5f);

            // The loop returns to source frame zero.
            AssertStereoFrame(56, 0.5f);
            AssertStereoFrame(87, 0.5f);

            // 2 ms = 88 output frames.
            AssertStereoFrame(88, 0f);
            AssertStereoFrame(89, 0f);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
    {
        using AudioPlaybackEngine engine = new();

        engine.Dispose();

        var exception = Record.Exception(() => engine.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void PlaybackOperations_AfterDispose_ThrowObjectDisposedException()
    {
        using AudioPlaybackEngine engine = new();

        engine.Dispose();

        var exception =
            Assert.Throws<ObjectDisposedException>(() => engine.InitializeOutput());

        Assert.Equal(
            nameof(AudioPlaybackEngine),
            exception.ObjectName);

        Assert.Throws<ObjectDisposedException>(() => engine.Play());

        Assert.Throws<ObjectDisposedException>(() => engine.Pause());

        Assert.Throws<ObjectDisposedException>(() => engine.Stop());

        Assert.Throws<ObjectDisposedException>(() => engine.Seek(0));

        Assert.Throws<ObjectDisposedException>(() => engine.GetTimelinePositionMilliseconds());

        Assert.Throws<ObjectDisposedException>(() => engine.LoadTimelineAudio("unused.wav"));

        Assert.Throws<ObjectDisposedException>(() => engine.AddAudioSource("unused.wav"));

        Assert.Throws<ObjectDisposedException>(() => engine.AddLoopingAudioSource("unused.wav", 100));
    }

    [Fact]
    public void Dispose_WhenReaderDisposalFails_StillReleasesRemainingReaders()
    {
        using AudioPlaybackEngine engine = new();

        var readersField =
            typeof(AudioPlaybackEngine).GetField(
                "activeReaders",
                BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(readersField);

        var readers =
            Assert.IsType<Dictionary<ISampleProvider, WaveStream>>(
                readersField.GetValue(engine));

        TrackingWaveStream failingReader = new(true);
        TrackingWaveStream workingReader = new();

        readers.Add(new SignalGenerator(44100, 2), failingReader);
        readers.Add(new SignalGenerator(44100, 2), workingReader);

        var exception =
            Assert.Throws<AggregateException>(() => engine.Dispose());

        Assert.Single(exception.InnerExceptions);
        Assert.IsType<IOException>(exception.InnerExceptions[0]);

        Assert.True(failingReader.WasDisposed);
        Assert.True(workingReader.WasDisposed);
        Assert.Empty(readers);

        engine.Dispose();
    }

    [Fact]
    public void ScheduleAudioSource_WhenOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        using AudioPlaybackEngine engine = new();

        var exception =
            Assert.Throws<InvalidOperationException>(() => engine.ScheduleAudioSource("unused.wav", 100));

        Assert.Equal(
            "The audio output has not been initialized.",
            exception.Message);
    }

    [Fact]
    public void ScheduleAudioSource_WhenEngineIsDisposed_ThrowsObjectDisposedException()
    {
        using AudioPlaybackEngine engine = new();

        engine.Dispose();

        var exception =
            Assert.Throws<ObjectDisposedException>(() => engine.ScheduleAudioSource("unused.wav", 100));

        Assert.Equal(
            nameof(AudioPlaybackEngine),
            exception.ObjectName);
    }

    [Theory]
    [InlineData(nameof(AudioPlaybackEngine.LoadTimelineAudio))]
    [InlineData(nameof(AudioPlaybackEngine.AddAudioSource))]
    [InlineData(nameof(AudioPlaybackEngine.AddLoopingAudioSource))]
    public void AudioInputOperations_WhenSchedulerIsFaulted_RejectBeforeOpeningFile(
        string operation)
    {
        using AudioPlaybackEngine engine = new();

        MixingSampleProvider mixer = new(
            WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));

        FrameScheduledSampleProvider scheduler = new(mixer);

        var mixerField = typeof(AudioPlaybackEngine).GetField(
            "mixer", BindingFlags.Instance | BindingFlags.NonPublic);

        var schedulerField = typeof(AudioPlaybackEngine).GetField(
            "frameScheduler", BindingFlags.Instance | BindingFlags.NonPublic);

        var faultField = typeof(FrameScheduledSampleProvider).GetField(
            "_isFaulted", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(mixerField);
        Assert.NotNull(schedulerField);
        Assert.NotNull(faultField);

        mixerField.SetValue(engine, mixer);
        schedulerField.SetValue(engine, scheduler);
        faultField.SetValue(scheduler, true);

        Assert.True(scheduler.IsFaulted);

        var missingPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.wav");

        Action action = operation switch
        {
            nameof(AudioPlaybackEngine.LoadTimelineAudio) =>
                () => engine.LoadTimelineAudio(missingPath),

            nameof(AudioPlaybackEngine.AddAudioSource) =>
                () => engine.AddAudioSource(missingPath),

            nameof(AudioPlaybackEngine.AddLoopingAudioSource) =>
                () => engine.AddLoopingAudioSource(missingPath, 100),

            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        var exception =
            Assert.Throws<InvalidOperationException>(action);

        Assert.Equal(
            "The frame scheduler is faulted and cannot be used.",
            exception.Message);
    }

    [Fact]
    public void Dispose_WhenMultipleReaderDisposalsFail_ContinuesCleanupAndAggregatesErrors()
    {
        AudioPlaybackEngine engine = new();

        var timelineField = typeof(AudioPlaybackEngine).GetField(
            "timelineReader",
            BindingFlags.Instance | BindingFlags.NonPublic);

        var readersField = typeof(AudioPlaybackEngine).GetField(
            "activeReaders",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(timelineField);
        Assert.NotNull(readersField);

        TrackingWaveStream failingTimelineReader = new(true);
        TrackingWaveStream failingActiveReader = new(true);
        TrackingWaveStream workingActiveReader = new();

        timelineField.SetValue(engine, failingTimelineReader);

        var readers =
            Assert.IsType<Dictionary<ISampleProvider, WaveStream>>(
                readersField.GetValue(engine));

        readers.Add(new SignalGenerator(44100, 2), failingActiveReader);
        readers.Add(new SignalGenerator(44100, 2), workingActiveReader);

        var exception =
            Assert.Throws<AggregateException>(() => engine.Dispose());

        Assert.Equal(2, exception.InnerExceptions.Count);

        Assert.All(
            exception.InnerExceptions,
            error => Assert.IsType<IOException>(error));

        Assert.True(failingTimelineReader.WasDisposed);
        Assert.True(failingActiveReader.WasDisposed);
        Assert.True(workingActiveReader.WasDisposed);

        Assert.Null(timelineField.GetValue(engine));
        Assert.Empty(readers);

        Assert.Null(Record.Exception(() => engine.Dispose()));
    }

    [Fact]
    public void OnMixerInputEnded_WhenReaderDisposalFails_RetainsReaderForCleanup()
    {
        AudioPlaybackEngine engine = new();

        MixingSampleProvider mixer = new(
            WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
        {
            ReadFully = true
        };

        var mixerField = typeof(AudioPlaybackEngine).GetField(
            "mixer",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(mixerField);
        mixerField.SetValue(engine, mixer);

        var handlerMethod = typeof(AudioPlaybackEngine).GetMethod(
            "OnMixerInputEnded",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(handlerMethod);

        var handler =
            (EventHandler<SampleProviderEventArgs>)Delegate.CreateDelegate(
                typeof(EventHandler<SampleProviderEventArgs>),
                engine,
                handlerMethod);

        mixer.MixerInputEnded += handler;

        var readersField = typeof(AudioPlaybackEngine).GetField(
            "activeReaders",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(readersField);

        var readers =
            Assert.IsType<Dictionary<ISampleProvider, WaveStream>>(
                readersField.GetValue(engine));

        TrackingWaveStream failingReader = new(true);

        ISampleProvider mixerInput = new OffsetSampleProvider(
            new SignalGenerator(44100, 2))
        {
            TakeSamples = 2
        };

        readers.Add(mixerInput, failingReader);
        mixer.AddMixerInput(mixerInput);

        Assert.Throws<IOException>(() => mixer.Read(new float[4].AsSpan()));

        Assert.True(failingReader.WasDisposed);
        Assert.Same(failingReader, readers[mixerInput]);

        var cleanupError =
            Assert.Throws<AggregateException>(() => engine.Dispose());

        Assert.Contains(
            cleanupError.InnerExceptions,
            exception => exception is IOException);

        Assert.Empty(readers);
        Assert.Null(Record.Exception(() => engine.Dispose()));
    }

    [Fact]
    public void AddLoopingAudioSource_WhenMixerRegistrationFails_RollsBackAndReleasesReader()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.wav");

        WaveFormat format = new(44100, 16, 1);

        using (WaveFileWriter writer = new(path, format))
        {
            byte[] audio = { 0, 64, 0, 32 };
            writer.Write(audio, 0, audio.Length);
        }

        try
        {
            using AudioPlaybackEngine engine = new();

            // The engine normalizes audio to stereo, but this mixer
            // deliberately accepts mono. Registration must fail.
            MixingSampleProvider mixer = new(
                WaveFormat.CreateIeeeFloatWaveFormat(44100, 1));

            var mixerField = typeof(AudioPlaybackEngine).GetField(
                "mixer",
                BindingFlags.Instance | BindingFlags.NonPublic);

            var readersField = typeof(AudioPlaybackEngine).GetField(
                "activeReaders",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(mixerField);
            Assert.NotNull(readersField);

            mixerField.SetValue(engine, mixer);

            var activeReaders =
                Assert.IsType<Dictionary<ISampleProvider, WaveStream>>(
                    readersField.GetValue(engine));

            var exception = Assert.Throws<ArgumentException>(() => engine.AddLoopingAudioSource(
                path,
                2));

            Assert.Contains(
                "All mixer inputs must have the same WaveFormat",
                exception.Message);

            // A failed registration must not retain the mixer input.
            Assert.Empty(mixer.MixerInputs);

            // The engine must not retain ownership of a released reader.
            Assert.Empty(activeReaders);

            // Verify that the reader no longer holds the audio file open.
            using FileStream exclusiveAccess = new(
                path,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None);

            Assert.True(exclusiveAccess.CanRead);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void OnMixerInputEnded_WhenReaderDisposalFails_FaultsScheduler()
    {
        AudioPlaybackEngine engine = new();

        MixingSampleProvider mixer = new(
            WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
        {
            ReadFully = true
        };

        FrameScheduledSampleProvider scheduler = new(mixer);

        var mixerField = typeof(AudioPlaybackEngine).GetField(
            "mixer",
            BindingFlags.Instance | BindingFlags.NonPublic);

        var readersField = typeof(AudioPlaybackEngine).GetField(
            "activeReaders",
            BindingFlags.Instance | BindingFlags.NonPublic);

        var callbackMethod = typeof(AudioPlaybackEngine).GetMethod(
            "OnMixerInputEnded",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(mixerField);
        Assert.NotNull(readersField);
        Assert.NotNull(callbackMethod);

        mixerField.SetValue(engine, mixer);

        var callback =
            (EventHandler<SampleProviderEventArgs>)Delegate.CreateDelegate(
                typeof(EventHandler<SampleProviderEventArgs>),
                engine,
                callbackMethod);

        mixer.MixerInputEnded += callback;

        var activeReaders =
            Assert.IsType<Dictionary<ISampleProvider, WaveStream>>(
                readersField.GetValue(engine));

        TrackingWaveStream failingReader = new(true);

        // One stereo frame. Reading two frames reaches the end
        // and causes MixingSampleProvider to raise MixerInputEnded.
        OffsetSampleProvider finiteSource = new(
            new SignalGenerator(44100, 2))
        {
            TakeSamples = 2
        };

        activeReaders.Add(finiteSource, failingReader);
        mixer.AddMixerInput(finiteSource);

        var originalException = Assert.Throws<IOException>(() => scheduler.Read(new float[4]));

        Assert.Equal(
            "Simulated reader disposal failure.",
            originalException.Message);

        Assert.True(failingReader.WasDisposed);
        Assert.True(scheduler.IsFaulted);
        Assert.Equal(0L, scheduler.FramesRead);

        // Ownership remains registered because cleanup failed.
        Assert.Single(activeReaders);
        Assert.Same(failingReader, activeReaders[finiteSource]);

        // The mixer callback threw before NAudio could remove the input.
        Assert.Contains(finiteSource, mixer.MixerInputs);

        var readException =
            Assert.Throws<InvalidOperationException>(() => scheduler.Read(new float[4]));

        Assert.Equal(
            "The frame scheduler is faulted and cannot be used.",
            readException.Message);

        scheduler.ResetScheduling();

        Assert.True(scheduler.IsFaulted);

        var scheduleException =
            Assert.Throws<InvalidOperationException>(() => scheduler.Schedule(
                new SignalGenerator(44100, 2),
                0));

        Assert.Equal(
            "The frame scheduler is faulted and cannot be used.",
            scheduleException.Message);

        var cleanupException =
            Assert.Throws<AggregateException>(() => engine.Dispose());

        Assert.Single(cleanupException.InnerExceptions);
        Assert.IsType<IOException>(
            cleanupException.InnerExceptions[0]);

        Assert.Empty(activeReaders);
        Assert.Empty(mixer.MixerInputs);

        Assert.Null(Record.Exception(() => engine.Dispose()));
    }

    private sealed class TrackingWaveStream : WaveStream
    {
        private readonly bool throwOnDispose;

        public TrackingWaveStream(bool throwOnDispose = false)
        {
            this.throwOnDispose = throwOnDispose;
        }

        public bool WasDisposed { get; private set; }

        public override WaveFormat WaveFormat { get; } =
            WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

        public override long Length => 0;

        public override long Position { get; set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                WasDisposed = true;

                if (throwOnDispose)
                    throw new IOException(
                        "Simulated reader disposal failure.");
            }

            base.Dispose(disposing);
        }
    }
}