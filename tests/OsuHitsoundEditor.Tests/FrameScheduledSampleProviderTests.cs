using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OsuHitsoundEditor;

namespace OsuHitsoundEditor.Tests;

public class FrameScheduledSampleProviderTests
{
    [Fact]
    public void Constructor_WhenMixerIsNull_ThrowsArgumentNullException()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => new FrameScheduledSampleProvider(null!));

        Assert.Equal("mixer", exception.ParamName);
    }

    [Fact]
    public void WaveFormat_WhenMixerIsProvided_ReturnsMixerWaveFormat()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new MixingSampleProvider(format);

        FrameScheduledSampleProvider provider =
            new FrameScheduledSampleProvider(mixer);

        Assert.Same(mixer.WaveFormat, provider.WaveFormat);
    }

    [Fact]
    public void Read_WhenBufferIsEmpty_ReturnsZero()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format);
        FrameScheduledSampleProvider provider = new(mixer);

        float[] buffer = Array.Empty<float>();

        int samplesRead = provider.Read(buffer);

        Assert.Equal(0, samplesRead);
    }

    [Fact]
    public void Read_WhenBufferIsNotFrameAligned_ThrowsArgumentException()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format);
        FrameScheduledSampleProvider provider = new(mixer);

        float[] buffer = new float[3];

        ArgumentException exception = Assert.Throws<ArgumentException>(() => provider.Read(buffer));

        Assert.Equal("buffer", exception.ParamName);
        Assert.Contains(
            "The buffer length must contain complete audio frames.",
            exception.Message);
    }

    [Fact]
    public void Read_WhenMixerHasNoInputsAndReadFullyIsTrue_ReturnsSilence()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format)
        {
            ReadFully = true
        };
        FrameScheduledSampleProvider provider = new(mixer);

        float[] buffer = { 1f, 1f, 1f, 1f };

        int samplesRead = provider.Read(buffer);

        Assert.Equal(4, samplesRead);
        Assert.All(buffer, sample => Assert.Equal(0f, sample));
    }

    [Fact]
    public void Read_WhenMixerHasSignal_ForwardsAudioSamples()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format);

        SignalGenerator signal = new(48000, 2)
        {
            Frequency = 440,
            Gain = 0.5f
        };

        mixer.AddMixerInput(signal);

        FrameScheduledSampleProvider provider = new(mixer);
        float[] buffer = new float[20];

        int samplesRead = provider.Read(buffer);

        Assert.Equal(buffer.Length, samplesRead);
        Assert.Contains(buffer, sample => sample != 0f);
    }

    [Fact]
    public void FramesRead_WhenCreated_ReturnsZero()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format);

        FrameScheduledSampleProvider provider = new(mixer);

        Assert.Equal(0L, provider.FramesRead);
    }

    [Fact]
    public void FramesRead_AfterMultipleReads_AccumulatesCompleteFrames()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format)
        {
            ReadFully = true
        };

        FrameScheduledSampleProvider provider = new(mixer);

        float[] firstBuffer = new float[8];
        float[] secondBuffer = new float[6];

        Assert.Equal(8, provider.Read(firstBuffer));
        Assert.Equal(4L, provider.FramesRead);

        Assert.Equal(6, provider.Read(secondBuffer));
        Assert.Equal(7L, provider.FramesRead);
    }

    [Fact]
    public void FramesRead_WhenBufferIsEmpty_DoesNotAdvance()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format)
        {
            ReadFully = true
        };

        FrameScheduledSampleProvider provider = new(mixer);

        provider.Read(new float[4]);
        Assert.Equal(2L, provider.FramesRead);

        int samplesRead = provider.Read(Array.Empty<float>());

        Assert.Equal(0, samplesRead);
        Assert.Equal(2L, provider.FramesRead);
    }

    [Fact]
    public void FramesRead_WhenMixerReturnsZero_DoesNotAdvance()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format)
        {
            ReadFully = false
        };

        FrameScheduledSampleProvider provider = new(mixer);

        int samplesRead = provider.Read(new float[4]);

        Assert.Equal(0, samplesRead);
        Assert.Equal(0L, provider.FramesRead);
    }

    [Fact]
    public void FramesRead_WhenBufferIsNotFrameAligned_DoesNotAdvance()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format)
        {
            ReadFully = true
        };

        FrameScheduledSampleProvider provider = new(mixer);

        provider.Read(new float[4]);

        Assert.Throws<ArgumentException>(() => provider.Read(new float[3]));

        Assert.Equal(2L, provider.FramesRead);
    }

    [Fact]
    public void Schedule_WhenSourceIsNull_ThrowsArgumentNullException()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format);
        FrameScheduledSampleProvider provider = new(mixer);

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => provider.Schedule(null!, 0));

        Assert.Equal("source", exception.ParamName);
    }

    [Fact]
    public void Schedule_WhenFrameIsNegative_ThrowsArgumentOutOfRangeException()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format);
        FrameScheduledSampleProvider provider = new(mixer);
        SignalGenerator source = new(48000, 2);

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.Schedule(source, -1));

        Assert.Equal("framePosition", exception.ParamName);
    }

    [Theory]
    [InlineData(44100, 2)]
    [InlineData(48000, 1)]
    public void Schedule_WhenSourceFormatDiffers_ThrowsArgumentException(
        int sampleRate,
        int channels)
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format);
        FrameScheduledSampleProvider provider = new(mixer);
        SignalGenerator source = new(sampleRate, channels);

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() => provider.Schedule(source, 100));

        Assert.Equal("source", exception.ParamName);
    }

    [Fact]
    public void Schedule_WhenFormatMatches_AcceptsEvent()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format);
        FrameScheduledSampleProvider provider = new(mixer);
        SignalGenerator source = new(48000, 2);

        Exception? exception = Record.Exception(() => provider.Schedule(source, 100));

        Assert.Null(exception);
        Assert.Equal(0L, provider.FramesRead);
    }

    [Fact]
    public void Schedule_WhenFrameWasConsumed_ThrowsArgumentOutOfRangeException()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format)
        {
            ReadFully = true
        };
        FrameScheduledSampleProvider provider = new(mixer);
        SignalGenerator source = new(48000, 2);

        provider.Read(new float[8]);

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.Schedule(source, 3));

        Assert.Equal("framePosition", exception.ParamName);
        Assert.Equal(4L, provider.FramesRead);
    }

    [Fact]
    public void Schedule_WhenFrameEqualsCurrentPosition_AcceptsEvent()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format)
        {
            ReadFully = true
        };
        FrameScheduledSampleProvider provider = new(mixer);
        SignalGenerator source = new(48000, 2);

        provider.Read(new float[8]);

        Exception? exception = Record.Exception(() => provider.Schedule(source, 4));

        Assert.Null(exception);
        Assert.Equal(4L, provider.FramesRead);
    }

    [Fact]
    public void Read_WhenEventIsInsideBuffer_StartsAtScheduledFrame()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format) { ReadFully = true };
        FrameScheduledSampleProvider provider = new(mixer);

        SignalGenerator source = new(48000, 2)
        {
            Type = SignalGeneratorType.Square,
            Gain = 0.25
        };

        provider.Schedule(source, 3);

        float[] buffer = new float[12];
        int samplesRead = provider.Read(buffer);

        Assert.Equal(12, samplesRead);
        Assert.Equal(6L, provider.FramesRead);
        Assert.Equal(
            new float[]
            {
                0, 0, 0, 0, 0, 0,
                -0.25f, -0.25f, -0.25f,
                -0.25f, -0.25f, -0.25f
            },
            buffer);
    }

    [Fact]
    public void Read_WhenMixerDoesNotReadFully_FillsSilenceUntilFutureEvent()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format) { ReadFully = false };
        FrameScheduledSampleProvider provider = new(mixer);

        SignalGenerator source = new(48000, 2)
        {
            Type = SignalGeneratorType.Square,
            Gain = 0.25
        };

        provider.Schedule(source, 2);

        float[] buffer = new float[10];
        int samplesRead = provider.Read(buffer);

        Assert.Equal(10, samplesRead);
        Assert.Equal(5L, provider.FramesRead);
        Assert.Equal(
            new float[]
            {
                0, 0, 0, 0,
                -0.25f, -0.25f, -0.25f,
                -0.25f, -0.25f, -0.25f
            },
            buffer);
    }

    [Fact]
    public void Read_WhenEventsShareFrame_MixesBothSources()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format) { ReadFully = true };
        FrameScheduledSampleProvider provider = new(mixer);

        SignalGenerator first = new(48000, 2)
        {
            Type = SignalGeneratorType.Square,
            Gain = 0.25
        };

        SignalGenerator second = new(48000, 2)
        {
            Type = SignalGeneratorType.Square,
            Gain = 0.25
        };

        provider.Schedule(first, 2);
        provider.Schedule(second, 2);

        float[] buffer = new float[8];
        int samplesRead = provider.Read(buffer);

        Assert.Equal(8, samplesRead);
        Assert.Equal(4L, provider.FramesRead);
        Assert.Equal(
            new float[]
            {
                0, 0, 0, 0,
                -0.5f, -0.5f, -0.5f, -0.5f
            },
            buffer);
    }

    [Fact]
    public void Read_WhenEventIsAtBufferEnd_WaitsForNextRead()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format) { ReadFully = true };
        FrameScheduledSampleProvider provider = new(mixer);

        SignalGenerator source = new(48000, 2)
        {
            Type = SignalGeneratorType.Square,
            Gain = 0.25
        };

        provider.Schedule(source, 3);

        float[] firstBuffer = new float[6];
        Assert.Equal(6, provider.Read(firstBuffer));
        Assert.Equal(new float[6], firstBuffer);
        Assert.Equal(3L, provider.FramesRead);

        float[] secondBuffer = new float[4];
        Assert.Equal(4, provider.Read(secondBuffer));
        Assert.Equal(
            new float[] { -0.25f, -0.25f, -0.25f, -0.25f },
            secondBuffer);
        Assert.Equal(5L, provider.FramesRead);
    }

    [Fact]
    public void Read_WhenTwoEventsFallInsideBuffer_StartsEachAtItsFrame()
    {
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        MixingSampleProvider mixer = new(format) { ReadFully = true };
        FrameScheduledSampleProvider provider = new(mixer);

        SignalGenerator first = new(48000, 2)
        {
            Type = SignalGeneratorType.Square,
            Gain = 0.25
        };

        SignalGenerator second = new(48000, 2)
        {
            Type = SignalGeneratorType.Square,
            Gain = 0.25
        };

        provider.Schedule(first, 2);
        provider.Schedule(second, 4);

        float[] buffer = new float[12];
        int samplesRead = provider.Read(buffer);

        Assert.Equal(12, samplesRead);
        Assert.Equal(6L, provider.FramesRead);
        Assert.Equal(
            new float[]
            {
                0, 0, 0, 0,
                -0.25f, -0.25f, -0.25f, -0.25f,
                -0.5f, -0.5f, -0.5f, -0.5f
            },
            buffer);
    }
}