using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using OsuHitsoundEditor;

namespace OsuHitsoundEditor.Tests;

public class LoopingWaveStreamTests
{
    private static readonly WaveFormat Mono16Bit = new(44100, 16, 1);

    [Fact]
    public void Constructor_WhenSourceIsNull_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new LoopingWaveStream(null!));

        Assert.Equal("sourceReader", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenSourceCannotSeek_ThrowsArgumentException()
    {
        using TestWaveStream source = new([1, 2], Mono16Bit, canSeek: false);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new LoopingWaveStream(source));

        Assert.Equal("sourceReader", exception.ParamName);
        Assert.StartsWith(
            "The audio source must support seeking for looping.",
            exception.Message);
    }

    [Fact]
    public void Constructor_WhenSourceIsEmpty_ThrowsArgumentException()
    {
        using TestWaveStream source = CreateSource([]);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new LoopingWaveStream(source));

        Assert.Equal("sourceReader", exception.ParamName);
        Assert.StartsWith(
            "The audio source must contain at least one complete audio frame.",
            exception.Message);
    }

    [Fact]
    public void Constructor_WhenSourceIsShorterThanOneFrame_ThrowsArgumentException()
    {
        using TestWaveStream source = CreateSource([1]);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new LoopingWaveStream(source));

        Assert.Equal("sourceReader", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenSourceContainsExactlyOneFrame_AllowsReading()
    {
        using TestWaveStream source = CreateSource([10, 20]);
        using LoopingWaveStream loop = new(source);

        byte[] output = new byte[6];

        int bytesRead = loop.Read(output.AsSpan());

        Assert.Equal(6, bytesRead);
        Assert.Equal(
            new byte[] { 10, 20, 10, 20, 10, 20 },
            output);
    }

    [Fact]
    public void Constructor_WhenSourceIsPartlyConsumed_StartsAtBeginning()
    {
        using TestWaveStream source = CreateSource([1, 2, 3, 4]);
        source.Position = 2;

        using LoopingWaveStream loop = new(source);

        byte[] output = new byte[2];

        Assert.Equal(0L, source.Position);
        Assert.Equal(2, loop.Read(output.AsSpan()));
        Assert.Equal(new byte[] { 1, 2 }, output);
    }

    [Fact]
    public void Properties_DelegateFormatLengthAndPositionToSource()
    {
        using TestWaveStream source = CreateSource([1, 2, 3, 4]);
        using LoopingWaveStream loop = new(source);

        Assert.Same(source.WaveFormat, loop.WaveFormat);
        Assert.Equal(4L, loop.Length);
        Assert.Equal(0L, loop.Position);

        loop.Position = 2;

        Assert.Equal(2L, source.Position);
        Assert.Equal(2L, loop.Position);
    }

    [Fact]
    public void ReadSpan_WhenBufferFitsInSource_ReadsWithoutRepeating()
    {
        using TestWaveStream source = CreateSource([1, 2, 3, 4, 5, 6]);
        using LoopingWaveStream loop = new(source);

        byte[] output = new byte[4];

        int bytesRead = loop.Read(output.AsSpan());

        Assert.Equal(4, bytesRead);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, output);
        Assert.Equal(4L, source.Position);
    }

    [Fact]
    public void ReadSpan_WhenBufferExceedsSourceLength_WrapsAtEnd()
    {
        using TestWaveStream source = CreateSource([1, 2, 3, 4, 5, 6]);
        using LoopingWaveStream loop = new(source);

        byte[] output = new byte[10];

        int bytesRead = loop.Read(output.AsSpan());

        Assert.Equal(10, bytesRead);
        Assert.Equal(
            new byte[] { 1, 2, 3, 4, 5, 6, 1, 2, 3, 4 },
            output);
    }

    [Fact]
    public void ReadSpan_WhenBufferNeedsSeveralLoops_RepeatsUntilFull()
    {
        using TestWaveStream source = CreateSource([11, 12, 13, 14]);
        using LoopingWaveStream loop = new(source);

        byte[] output = new byte[14];

        int bytesRead = loop.Read(output.AsSpan());

        Assert.Equal(14, bytesRead);
        Assert.Equal(
            new byte[]
            {
                11, 12, 13, 14,
                11, 12, 13, 14,
                11, 12, 13, 14,
                11, 12
            },
            output);
    }

    [Fact]
    public void ReadSpan_WhenPreviousReadEndsExactlyAtEnd_NextReadRestarts()
    {
        using TestWaveStream source = CreateSource([1, 2, 3, 4]);
        using LoopingWaveStream loop = new(source);

        byte[] first = new byte[4];
        byte[] second = new byte[2];

        int firstRead = loop.Read(first.AsSpan());
        int secondRead = loop.Read(second.AsSpan());

        Assert.Equal(4, firstRead);
        Assert.Equal(2, secondRead);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, first);
        Assert.Equal(new byte[] { 1, 2 }, second);
    }

    [Fact]
    public void ReadSpan_WhenBufferIsEmpty_ReturnsZeroWithoutMovingSource()
    {
        using TestWaveStream source = CreateSource([1, 2, 3, 4]);
        using LoopingWaveStream loop = new(source);

        loop.Position = 2;

        int bytesRead = loop.Read(Span<byte>.Empty);

        Assert.Equal(0, bytesRead);
        Assert.Equal(2L, source.Position);
    }

    [Fact]
    public void ReadSpan_WhenBufferLengthIsNotFrameAligned_ThrowsArgumentException()
    {
        using TestWaveStream source = CreateSource([1, 2, 3, 4]);
        using LoopingWaveStream loop = new(source);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => loop.Read(new byte[3].AsSpan()));

        Assert.Equal("buffer", exception.ParamName);
        Assert.StartsWith(
            "The buffer length must be aligned to complete audio frames.",
            exception.Message);

        Assert.Equal(0L, source.Position);
    }

    [Fact]
    public void ReadSpan_WhenSourceReturnsIncompleteFrame_ThrowsInvalidOperationException()
    {
        using TestWaveStream source = CreateSource([1, 2, 3]);
        using LoopingWaveStream loop = new(source);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => loop.Read(new byte[4].AsSpan()));

        Assert.Equal(
            "The audio source returned an incomplete audio frame.",
            exception.Message);
    }

    [Fact]
    public void ReadSpan_WhenSourceStillReturnsZeroAfterRewind_ThrowsInvalidOperationException()
    {
        using TestWaveStream source = new(
            [1, 2],
            Mono16Bit,
            alwaysEmpty: true);

        using LoopingWaveStream loop = new(source);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => loop.Read(new byte[2].AsSpan()));

        Assert.Equal(
            "The audio source returned no data after rewinding.",
            exception.Message);
    }

    [Fact]
    public void ReadArray_WhenOffsetIsNonzero_OnlyWritesWithinRequestedRegion()
    {
        using TestWaveStream source = CreateSource([1, 2, 3, 4]);
        using LoopingWaveStream loop = new(source);

        byte[] output = Enumerable.Repeat((byte)255, 10).ToArray();

        int bytesRead = loop.Read(output, 2, 6);

        Assert.Equal(6, bytesRead);
        Assert.Equal(
            new byte[]
            {
                255, 255,
                1, 2, 3, 4, 1, 2,
                255, 255
            },
            output);
    }

    [Fact]
    public void ReadArray_WhenCountIsZero_ReturnsZeroWithoutMovingSource()
    {
        using TestWaveStream source = CreateSource([1, 2, 3, 4]);
        using LoopingWaveStream loop = new(source);

        loop.Position = 2;

        int bytesRead = loop.Read(new byte[4], 2, 0);

        Assert.Equal(0, bytesRead);
        Assert.Equal(2L, source.Position);
    }

    [Theory]
    [InlineData(-1, 2)]
    [InlineData(0, -1)]
    [InlineData(3, 2)]
    public void ReadArray_WhenRegionIsInvalid_ThrowsArgumentException(
        int offset,
        int count)
    {
        using TestWaveStream source = CreateSource([1, 2, 3, 4]);
        using LoopingWaveStream loop = new(source);

        Assert.ThrowsAny<ArgumentException>(
            () => loop.Read(new byte[4], offset, count));
    }

    [Fact]
    public void Dispose_WhenLoopIsDisposed_DoesNotDisposeOriginalReader()
    {
        using TestWaveStream source = CreateSource([1, 2, 3, 4]);

        LoopingWaveStream loop = new(source);
        loop.Dispose();

        Assert.False(source.WasDisposed);
        Assert.Equal(2, source.Read(new byte[2].AsSpan()));
    }


    [Fact]
    public void OffsetSampleProvider_WhenSourceLoops_StopsAtRequestedSampleCount()
    {
        using TestWaveStream source = CreateSource([0, 64, 0, 32]);
        using LoopingWaveStream loop = new(source);

        ISampleProvider sampleProvider = loop.ToSampleProvider();

        OffsetSampleProvider limitedProvider = new(sampleProvider)
        {
            TakeSamples = 6
        };

        float[] output = Enumerable.Repeat(-1f, 10).ToArray();

        int samplesRead = limitedProvider.Read(output.AsSpan());
        int samplesAfterEnd = limitedProvider.Read(new float[2].AsSpan());

        Assert.Equal(6, samplesRead);
        Assert.Equal(0, samplesAfterEnd);

        Assert.Equal(
            new float[]
            {
            0.5f, 0.25f,
            0.5f, 0.25f,
            0.5f, 0.25f,
            -1f, -1f, -1f, -1f
            },
            output);
    }

    [Fact]
    public void OffsetSampleProvider_WhenNormalizedToStereo_UsesInterleavedSampleCount()
    {
        using TestWaveStream source = CreateSource([0, 64, 0, 32]);
        using LoopingWaveStream loop = new(source);

        ISampleProvider monoProvider = loop.ToSampleProvider();
        ISampleProvider stereoProvider =
            new MonoToStereoSampleProvider(monoProvider);

        OffsetSampleProvider limitedProvider = new(stereoProvider)
        {
            TakeSamples = 12
        };

        float[] output = new float[20];

        int samplesRead = limitedProvider.Read(output.AsSpan());
        int samplesAfterEnd = limitedProvider.Read(new float[4].AsSpan());

        Assert.Equal(2, limitedProvider.WaveFormat.Channels);
        Assert.Equal(12, samplesRead);
        Assert.Equal(0, samplesAfterEnd);

        Assert.Equal(
            new float[]
            {
            0.5f, 0.5f, 0.25f, 0.25f,
            0.5f, 0.5f, 0.25f, 0.25f,
            0.5f, 0.5f, 0.25f, 0.25f
            },
            output.Take(12).ToArray());
    }


    private static TestWaveStream CreateSource(byte[] audio)
    {
        return new TestWaveStream(audio, Mono16Bit);
    }

    private sealed class TestWaveStream : WaveStream
    {
        private readonly MemoryStream memoryStream;
        private readonly WaveFormat format;
        private readonly bool canSeek;
        private readonly bool alwaysEmpty;

        public bool WasDisposed { get; private set; }

        public TestWaveStream(
            byte[] audio,
            WaveFormat format,
            bool canSeek = true,
            bool alwaysEmpty = false)
        {
            memoryStream = new MemoryStream(audio);
            this.format = format;
            this.canSeek = canSeek;
            this.alwaysEmpty = alwaysEmpty;
        }

        public override WaveFormat WaveFormat
        {
            get
            {
                return format;
            }
        }

        public override long Length
        {
            get
            {
                return memoryStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return memoryStream.Position;
            }
            set
            {
                memoryStream.Position = value;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return canSeek;
            }
        }

        public override int Read(Span<byte> buffer)
        {
            if (alwaysEmpty)
            {
                return 0;
            }

            return memoryStream.Read(buffer);
        }

        public override int Read(
            byte[] buffer,
            int offset,
            int count)
        {
            if (alwaysEmpty)
            {
                return 0;
            }

            return memoryStream.Read(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                WasDisposed = true;
                memoryStream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
