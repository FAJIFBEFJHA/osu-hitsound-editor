using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OsuHitsoundEditor;

public class FrameScheduledSampleProvider : ISampleProvider
{
    private readonly PriorityQueue<ISampleProvider, (long Frame, long Order)> _scheduledEvents = new();
    private readonly object _scheduleLock = new();
    private long _nextOrder;
    private readonly MixingSampleProvider _mixer;
    private long _framesRead;
    private bool _isFaulted;

    public bool IsFaulted
    {
        get
        {
            lock (_scheduleLock)
            {
                return _isFaulted;
            }
        }
    }

    public FrameScheduledSampleProvider(MixingSampleProvider mixer)
    {
        if (mixer == null)
        {
            throw new ArgumentNullException(
                nameof(mixer),
                "The mixer cannot be null.");
        }

        this._mixer = mixer;
    }

    public WaveFormat WaveFormat
    {
        get { return _mixer.WaveFormat; }
    }

    public long FramesRead
    {
        get { return Interlocked.Read(ref _framesRead); }
    }

    public int Read(Span<float> buffer)
    {
        if (buffer.IsEmpty)
        {
            return 0;
        }

        if (buffer.Length % WaveFormat.Channels != 0)
        {
            throw new ArgumentException(
                "The buffer length must contain complete audio frames.",
                nameof(buffer));
        }

        int channels = WaveFormat.Channels;
        int totalSamplesRead = 0;
        lock (_scheduleLock)
        {
            if (_isFaulted)
            {
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");
            }

            try
            {
                while (totalSamplesRead < buffer.Length)
                {
                    long currentFrame = FramesRead;
                    long? nextEventFrame = null;
                    while (true)
                    {
                        bool hasEvent = _scheduledEvents.TryPeek(out ISampleProvider? eventSource,
                            out (long Frame, long Order) priority);
                        if (hasEvent == false)
                        {
                            break;
                        }

                        if (priority.Frame < currentFrame)
                        {
                            throw new InvalidOperationException(
                                "A scheduled event is earlier than the current read position.");
                        }

                        if (priority.Frame > currentFrame)
                        {
                            nextEventFrame = priority.Frame;
                            break;
                        }

                        if (priority.Frame == currentFrame)
                        {
                            _mixer.AddMixerInput(eventSource);
                            _scheduledEvents.Dequeue();
                        }
                    }

                    int remainingFrames = (buffer.Length - totalSamplesRead) / channels;
                    int segmentFrames = remainingFrames;
                    if (nextEventFrame.HasValue)
                    {
                        long framesUntilEvent = nextEventFrame.Value - currentFrame;
                        if (framesUntilEvent < segmentFrames)
                        {
                            segmentFrames = (int)framesUntilEvent;
                        }
                    }

                    if (currentFrame > long.MaxValue - segmentFrames)
                    {
                        throw new InvalidOperationException(
                            "The frame counter would exceed the supported range.");
                    }

                    Span<float> segment = buffer.Slice(totalSamplesRead, segmentFrames * channels);
                    int samplesRead = _mixer.Read(segment);
                    if (samplesRead < 0 || samplesRead > segment.Length)
                    {
                        throw new InvalidOperationException(
                            "The mixer returned an invalid sample count.");
                    }

                    if (samplesRead % channels != 0)
                    {
                        throw new InvalidOperationException(
                            "The mixer returned an incomplete audio frame.");
                    }

                    if (samplesRead < segment.Length && nextEventFrame.HasValue)
                    {
                        segment.Slice(samplesRead).Clear();
                        samplesRead = segment.Length;
                    }

                    totalSamplesRead += samplesRead;
                    int completedFrames = samplesRead / channels;
                    Interlocked.Add(ref _framesRead, completedFrames);
                    if (samplesRead < segment.Length)
                    {
                        break;
                    }
                }
            }
            catch
            {
                _isFaulted = true;
                throw;
            }
        }
        return totalSamplesRead;
    }

    public void Schedule(ISampleProvider source, long framePosition)
    {
        if (source == null)
        {
            throw new ArgumentNullException(
                nameof(source),
                "The scheduled source cannot be null.");
        }

        if (framePosition < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(framePosition),
                framePosition,
                "The scheduled frame must be non-negative.");
        }

        if (!source.WaveFormat.Equals(WaveFormat))
        {
            throw new ArgumentException(
                "The scheduled source must match the mixer format.",
                nameof(source));
        }

        if (Monitor.IsEntered(_scheduleLock))
        {
            throw new InvalidOperationException(
                "Scheduling from within an active audio read is not supported.");
        }

        lock (_scheduleLock)
        {
            if (_isFaulted)
            {
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");
            }
            if (framePosition < FramesRead)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(framePosition),
                    framePosition,
                    "The scheduled frame has already been consumed.");
            }

            if (_nextOrder == long.MaxValue)
            {
                throw new InvalidOperationException(
                    "The scheduling order has reached its maximum value.");
            }

            _scheduledEvents.Enqueue(source, (framePosition, _nextOrder));
            _nextOrder++;
        }
    }

    public void ResetScheduling()
    {
        if (Monitor.IsEntered(_scheduleLock))
        {
            throw new InvalidOperationException(
                "Resetting scheduling from within an active audio read is not supported.");
        }

        lock (_scheduleLock)
        {
            _scheduledEvents.Clear();
            Interlocked.Exchange(ref _framesRead, 0);
            _nextOrder = 0;
        }
    }
}