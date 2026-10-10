using NAudio.Wave;
using System.Reflection;
using NAudio.Wave.SampleProviders;
using OsuHitsoundEditor;

namespace OsuHitsoundEditor.Tests;

public class AudioPlaybackEngineTests
{
    [Fact]
    public void OpenAudioFile_WhenFileDoesNotExist_ThrowsFileNotFoundException()
    {
        var engine = new AudioPlaybackEngine();

        string path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

        FileNotFoundException exception =
            Assert.Throws<FileNotFoundException>(
                () => engine.OpenAudioFile(path));

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

        string path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.txt");

        File.WriteAllText(path, "test");

        try
        {
            NotSupportedException exception =
                Assert.Throws<NotSupportedException>(
                    () => engine.OpenAudioFile(path));

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

        string path = Path.Combine(
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
            byte[] silence =
                new byte[format.AverageBytesPerSecond / 10];

            writer.Write(
                silence,
                0,
                silence.Length);
        }

        try
        {
            using WaveStream reader =
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

        string path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

        var format = new WaveFormat(
            44100,
            16,
            2);

        using (var writer = new WaveFileWriter(path, format))
        {
            byte[] audio = new byte[format.BlockAlign * 10];
            writer.Write(audio, 0, audio.Length);
        }

        try
        {
            using var reader = new WaveFileReader(path);

            ISampleProvider provider =
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

        string path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

        var format = new WaveFormat(
            44100,
            16,
            1);

        using (var writer = new WaveFileWriter(path, format))
        {
            byte[] audio = new byte[format.BlockAlign * 10];
            writer.Write(audio, 0, audio.Length);
        }

        try
        {
            using var reader = new WaveFileReader(path);

            ISampleProvider provider =
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

        string path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

        var format = new WaveFormat(
            44100,
            16,
            4);

        using (var writer = new WaveFileWriter(path, format))
        {
            byte[] audio = new byte[format.BlockAlign * 10];
            writer.Write(audio, 0, audio.Length);
        }

        try
        {
            using var reader = new WaveFileReader(path);

            ISampleProvider provider =
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

        string path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

        var format = new WaveFormat(
            22050,
            16,
            2);

        using (var writer = new WaveFileWriter(path, format))
        {
            byte[] audio = new byte[format.BlockAlign * 10];
            writer.Write(audio, 0, audio.Length);
        }

        try
        {
            using var reader = new WaveFileReader(path);

            ISampleProvider provider =
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

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => engine.Play());

        Assert.Equal(
            "The audio output has not been initialized.",
            exception.Message);
    }
    [Fact]
    public void Pause_WhenOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        var engine = new AudioPlaybackEngine();

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => engine.Pause());

        Assert.Equal(
            "The audio output has not been initialized.",
            exception.Message);
    }
    [Fact]
    public void Stop_WhenOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        var engine = new AudioPlaybackEngine();

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => engine.Stop());

        Assert.Equal(
            "The audio output has not been initialized.",
            exception.Message);
    }
    [Fact]
    public void AddAudioSource_WhenOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        var engine = new AudioPlaybackEngine();

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => engine.AddAudioSource("audio.wav"));

        Assert.Equal(
            "The audio output has not been initialized.",
            exception.Message);
    }
    [Fact]
    public void LoadTimelineAudio_WhenOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        var engine = new AudioPlaybackEngine();

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => engine.LoadTimelineAudio("audio.wav"));

        Assert.Equal(
            "The audio output has not been initialized.",
            exception.Message);
    }
    [Fact]
    public void Seek_WhenOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        using var engine = new AudioPlaybackEngine();

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => engine.Seek(1000));

        Assert.Equal(
            "The audio output has not been initialized.",
            exception.Message);
    }
    [Fact]
    public void GetTimelinePositionMilliseconds_WhenOutputIsNotInitialized_ThrowsInvalidOperationException()
    {
        using var engine = new AudioPlaybackEngine();

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => engine.GetTimelinePositionMilliseconds());

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

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => engine.AddAudioSource("audio.wav", volume));

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
        string path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.wav");

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

        try
        {
            using var engine = new AudioPlaybackEngine();

            var mixer = new MixingSampleProvider(
                WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));

            FieldInfo? mixerField =
                typeof(AudioPlaybackEngine).GetField(
                    "mixer",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(mixerField);

            mixerField.SetValue(engine, mixer);

            engine.AddAudioSource(path, volume);

            float[] output = new float[2];

            int samplesRead =
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

            foreach (string path in paths)
            {
                using var writer = new WaveFileWriter(path, format);

                byte[] audio = new byte[format.BlockAlign * 100];

                for (int i = 0; i < audio.Length; i += 2)
                {
                    audio[i] = 0;
                    audio[i + 1] = 64;
                }

                writer.Write(audio, 0, audio.Length);
            }

            var mixer = new MixingSampleProvider(
                WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));

            FieldInfo? mixerField =
                typeof(AudioPlaybackEngine).GetField(
                    "mixer",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(mixerField);
            mixerField.SetValue(engine, mixer);

            engine.AddAudioSource(paths[0], 50);
            engine.AddAudioSource(paths[1], 50);

            float[] output = new float[2];

            int samplesRead = mixer.Read(output.AsSpan());

            Assert.Equal(2, samplesRead);

            Assert.InRange(output[0], 0.49f, 0.51f);
            Assert.InRange(output[1], 0.49f, 0.51f);
        }
        finally
        {
            foreach (string path in paths)
            {
                File.Delete(path);
            }
        }
    }
}
