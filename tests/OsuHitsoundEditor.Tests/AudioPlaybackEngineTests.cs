using NAudio.Wave;
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
}
