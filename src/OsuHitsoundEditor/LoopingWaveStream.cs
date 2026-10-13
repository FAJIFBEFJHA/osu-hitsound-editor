using NAudio.Wave;

namespace OsuHitsoundEditor;

public class LoopingWaveStream : WaveStream
{
    private readonly WaveStream sourceReader;
    public LoopingWaveStream(WaveStream sourceReader)
    {
        ArgumentNullException.ThrowIfNull(sourceReader);
        if (!sourceReader.CanSeek)
        {
            throw new ArgumentException(
                "The audio source must support seeking for looping.",
                nameof(sourceReader));
        }
        if (sourceReader.Length < sourceReader.WaveFormat.BlockAlign)
        {
            throw new ArgumentException(
                "The audio source must contain at least one complete audio frame.",
                nameof(sourceReader));
        }
        this.sourceReader = sourceReader;
        sourceReader.Position = 0;
    }
    public override WaveFormat WaveFormat
    {
        get
        {
            return sourceReader.WaveFormat;
        }
    }
    public override long Length
    {
        get
        {
            return sourceReader.Length;
        }
    }
    public override long Position
    {
        get
        {
            return sourceReader.Position;
        }
        set
        {
            sourceReader.Position = value;
        }
    }
    public override int Read(Span<byte> buffer)
    {
        if (buffer.Length == 0)
        {
            return 0;
        }
        if (buffer.Length % WaveFormat.BlockAlign != 0)
        {
            throw new ArgumentException(
                "The buffer length must be aligned to complete audio frames.",
                nameof(buffer));
        }
        int totalBytesRead = 0;
        while (totalBytesRead < buffer.Length)
        {
            Span<byte> remainingBuffer = buffer.Slice(totalBytesRead);
            int bytesRead = sourceReader.Read(remainingBuffer);
            if (bytesRead == 0)
            {
                sourceReader.Position = 0;
                bytesRead = sourceReader.Read(remainingBuffer);
                if (bytesRead == 0)
                {
                    throw new InvalidOperationException(
                        "The audio source returned no data after rewinding.");
                }
            }
            if (bytesRead % WaveFormat.BlockAlign != 0)
            {
                throw new InvalidOperationException(
                    "The audio source returned an incomplete audio frame.");
            }
            totalBytesRead += bytesRead;
        }
        return totalBytesRead;
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        Span<byte> region =  buffer.AsSpan(offset, count);
        int bytesRead =  Read(region);
        return bytesRead;
    }
}