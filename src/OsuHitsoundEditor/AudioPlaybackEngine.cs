using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OsuHitsoundEditor;

public class AudioPlaybackEngine : IDisposable
{
    private readonly Dictionary<ISampleProvider, WaveStream> activeReaders = new();
    private readonly object activeReadersLock = new();
    private readonly object playbackStateLock = new();
    private FrameScheduledSampleProvider? frameScheduler;
    private bool isDisposed;
    private MixingSampleProvider? mixer;
    private WasapiPlayer? outputDevice;
    private double timelineBaseMilliseconds;
    private ISampleProvider? timelineProvider;
    private WaveStream? timelineReader;

    public void Dispose()
    {
        lock (playbackStateLock)
        {
            if (isDisposed) return;

            isDisposed = true;
            List<Exception> disposalErrors = new();
            var currentMixer = mixer;
            if (outputDevice != null)
            {
                var device = outputDevice;
                outputDevice = null;
                try
                {
                    device.Dispose();
                }
                catch (Exception exception)
                {
                    disposalErrors.Add(exception);
                }
            }

            if (timelineReader != null)
            {
                var reader = timelineReader;
                timelineReader = null;

                try
                {
                    reader.Dispose();
                }
                catch (Exception exception)
                {
                    disposalErrors.Add(exception);
                }
            }

            frameScheduler = null;
            if (currentMixer != null)
            {
                currentMixer.MixerInputEnded -= OnMixerInputEnded;
                try
                {
                    currentMixer.RemoveAllMixerInputs();
                }
                catch (Exception exception)
                {
                    disposalErrors.Add(exception);
                }
            }

            var readersToDispose = new List<WaveStream>();

            lock (activeReadersLock)
            {
                readersToDispose.AddRange(activeReaders.Values);
                activeReaders.Clear();
            }

            timelineProvider = null;

            foreach (var reader in readersToDispose)
                try
                {
                    reader.Dispose();
                }
                catch (Exception exception)
                {
                    disposalErrors.Add(exception);
                }

            mixer = null;
            if (disposalErrors.Count > 0)
                throw new AggregateException(
                    "One or more audio resources could not be disposed.",
                    disposalErrors);
        }
    }

    public void InitializeOutput()
    {
        lock (playbackStateLock)
        {
            if (isDisposed)
                throw new ObjectDisposedException(
                    nameof(AudioPlaybackEngine),
                    "The audio playback engine has been disposed.");

            if (outputDevice != null)
                throw new InvalidOperationException(
                    "The audio output has already been initialized.");

            var builder = new WasapiPlayerBuilder()
                .WithLowLatency();
            var device = builder.Build();
            int targetSampleRate;
            WaveFormat mixerFormat;
            MixingSampleProvider? currentMixer = null;
            try
            {
                targetSampleRate = device.DeviceMixFormat.SampleRate;
                mixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(targetSampleRate, 2);
                currentMixer = new MixingSampleProvider(mixerFormat);
                currentMixer.MixerInputEnded += OnMixerInputEnded;
                currentMixer.ReadFully = true;
                var currentScheduler = new FrameScheduledSampleProvider(currentMixer);
                device.Init(currentScheduler);
                outputDevice = device;
                mixer = currentMixer;
                frameScheduler = currentScheduler;
            }
            catch (Exception initializationError)
            {
                List<Exception> cleanupErrors = new();
                if (currentMixer != null)
                    try
                    {
                        currentMixer.MixerInputEnded -= OnMixerInputEnded;
                    }
                    catch (Exception exception)
                    {
                        cleanupErrors.Add(exception);
                    }

                try
                {
                    device.Dispose();
                }
                catch (Exception exception)
                {
                    cleanupErrors.Add(exception);
                }

                if (cleanupErrors.Count > 0)
                {
                    List<Exception> allErrors = new() { initializationError };
                    allErrors.AddRange(cleanupErrors);
                    throw new AggregateException(
                        "Audio output initialization failed, and cleanup also failed.",
                        allErrors);
                }

                throw;
            }
        }
    }

    private WasapiPlayer GetInitializedOutputDevice()
    {
        if (isDisposed)
            throw new ObjectDisposedException(
                nameof(AudioPlaybackEngine),
                "The audio playback engine has been disposed.");

        if (outputDevice == null)
            throw new InvalidOperationException(
                "The audio output has not been initialized.");

        return outputDevice;
    }

    public void Play()
    {
        lock (playbackStateLock)
        {
            var device = GetInitializedOutputDevice();
            var currentScheduler = frameScheduler
                                   ?? throw new InvalidOperationException(
                                       "The frame scheduler has not been initialized.");

            if (currentScheduler.IsFaulted)
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");

            device.Play();
        }
    }

    public void Pause()
    {
        lock (playbackStateLock)
        {
            var device = GetInitializedOutputDevice();
            device.Pause();
        }
    }

    public void Stop()
    {
        lock (playbackStateLock)
        {
            var device = GetInitializedOutputDevice();
            device.Stop();
            var currentScheduler = frameScheduler
                                   ?? throw new InvalidOperationException(
                                       "The frame scheduler has not been initialized.");
            if (timelineReader != null && !currentScheduler.IsFaulted)
            {
                Seek(0);
                return;
            }

            var currentMixer = mixer
                               ?? throw new InvalidOperationException(
                                   "The audio output has not been initialized.");
            try
            {
                currentScheduler.ResetScheduling();
                currentMixer.RemoveAllMixerInputs();
                timelineProvider = null;
                var readersToDispose = new List<WaveStream>();
                lock (activeReadersLock)
                {
                    readersToDispose.AddRange(activeReaders.Values);
                    activeReaders.Clear();
                }

                List<Exception> cleanupErrors = new();

                foreach (var reader in readersToDispose)
                    try
                    {
                        reader.Dispose();
                    }
                    catch (Exception exception)
                    {
                        cleanupErrors.Add(exception);
                    }

                if (cleanupErrors.Count > 0)
                    throw new AggregateException(
                        "One or more audio resources could not be released during stopping.",
                        cleanupErrors);

                timelineBaseMilliseconds = 0;
            }
            catch (Exception operationError)
            {
                try
                {
                    Dispose();
                }
                catch (Exception disposalError)
                {
                    throw new AggregateException(
                        "The audio stop failed, and engine cleanup also failed.",
                        operationError,
                        disposalError);
                }

                throw;
            }
        }
    }

    public void Seek(double timeMilliseconds)
    {
        lock (playbackStateLock)
        {
            var device = GetInitializedOutputDevice();
            if (mixer == null)
                throw new InvalidOperationException(
                    "The audio output has not been initialized.");

            if (timelineReader == null)
                throw new InvalidOperationException(
                    "No timeline audio source has been loaded.");

            if (!double.IsFinite(timeMilliseconds) || timeMilliseconds < 0 ||
                timeMilliseconds > timelineReader.TotalTime.TotalMilliseconds)
                throw new ArgumentOutOfRangeException(
                    nameof(timeMilliseconds),
                    timeMilliseconds,
                    $"The seek position must be finite and between 0 and {timelineReader.TotalTime.TotalMilliseconds} milliseconds.");

            var currentScheduler = frameScheduler
                                   ?? throw new InvalidOperationException(
                                       "The frame scheduler has not been initialized.");
            if (currentScheduler.IsFaulted)
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");

            var wasPlaying = device.PlaybackState == PlaybackState.Playing;
            device.Stop();
            try
            {
                if (currentScheduler.IsFaulted)
                    throw new InvalidOperationException(
                        "The frame scheduler is faulted and cannot be used.");

                currentScheduler.ResetScheduling();
                mixer.RemoveAllMixerInputs();
                timelineProvider = null;
                var readersToDispose = new List<WaveStream>();
                lock (activeReadersLock)
                {
                    readersToDispose.AddRange(activeReaders.Values);
                    activeReaders.Clear();
                }

                List<Exception> cleanupErrors = new();
                foreach (var reader in readersToDispose)
                    try
                    {
                        reader.Dispose();
                    }
                    catch (Exception exception)
                    {
                        cleanupErrors.Add(exception);
                    }

                if (cleanupErrors.Count > 0)
                    throw new AggregateException(
                        "One or more audio resources could not be released during seeking.",
                        cleanupErrors);

                timelineReader.CurrentTime = TimeSpan.FromMilliseconds(timeMilliseconds);
                var provider = NormalizeForMixer(timelineReader, mixer.WaveFormat.SampleRate);
                mixer.AddMixerInput(provider);
                timelineProvider = provider;
                timelineBaseMilliseconds = timeMilliseconds;
                if (wasPlaying) device.Play();
            }
            catch (Exception operationError)
            {
                try
                {
                    Dispose();
                }
                catch (Exception disposalError)
                {
                    throw new AggregateException(
                        "The audio seek failed, and engine cleanup also failed.",
                        operationError,
                        disposalError);
                }

                throw;
            }
        }
    }

    public double GetTimelinePositionMilliseconds()
    {
        lock (playbackStateLock)
        {
            var device = GetInitializedOutputDevice();
            if (timelineReader == null)
                throw new InvalidOperationException(
                    "No timeline audio source has been loaded.");

            var currentScheduler = frameScheduler
                                   ?? throw new InvalidOperationException(
                                       "The frame scheduler has not been initialized.");

            if (currentScheduler.IsFaulted)
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");

            var renderedBytes = device.GetPosition();
            var renderedMilliseconds = renderedBytes * 1000.0 / device.OutputWaveFormat.AverageBytesPerSecond;
            return timelineBaseMilliseconds + renderedMilliseconds;
        }
    }

    public void LoadTimelineAudio(string filePath)
    {
        lock (playbackStateLock)
        {
            if (isDisposed)
                throw new ObjectDisposedException(
                    nameof(AudioPlaybackEngine),
                    "The audio playback engine has been disposed.");

            if (mixer == null)
                throw new InvalidOperationException(
                    "The audio output has not been initialized.");

            if (timelineReader != null)
                throw new InvalidOperationException(
                    "A timeline audio source has already been loaded.");

            if (frameScheduler?.IsFaulted == true)
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");

            var currentMixer = mixer;
            var reader = OpenAudioFile(filePath);
            ISampleProvider provider;
            try
            {
                provider = NormalizeForMixer(reader, currentMixer.WaveFormat.SampleRate);
            }
            catch (Exception normalizationError)
            {
                try
                {
                    reader.Dispose();
                }
                catch (Exception cleanupError)
                {
                    throw new AggregateException(
                        "Audio normalization failed, and reader cleanup also failed.",
                        normalizationError,
                        cleanupError);
                }

                throw;
            }

            timelineReader = reader;
            timelineProvider = provider;
            try
            {
                currentMixer.AddMixerInput(provider);
            }
            catch (Exception registrationError)
            {
                List<Exception> errors = new() { registrationError };
                var mixerRollbackCompleted = false;
                try
                {
                    currentMixer.RemoveMixerInput(provider);
                    mixerRollbackCompleted = true;
                }
                catch (Exception removalError)
                {
                    errors.Add(removalError);
                }

                if (!mixerRollbackCompleted)
                {
                    try
                    {
                        Dispose();
                    }
                    catch (Exception disposalError)
                    {
                        errors.Add(disposalError);
                    }

                    throw new AggregateException(
                        "Timeline audio registration failed, and mixer rollback failed.",
                        errors);
                }

                timelineProvider = null;
                timelineReader = null;
                try
                {
                    reader.Dispose();
                }
                catch (Exception cleanupError)
                {
                    errors.Add(cleanupError);
                }

                if (errors.Count > 1)
                    throw new AggregateException(
                        "Timeline audio registration failed, and reader cleanup also failed.",
                        errors);

                throw;
            }
        }
    }

    public void AddAudioSource(string filePath, int volume = 100, double delayMilliseconds = 0)
    {
        lock (playbackStateLock)
        {
            if (isDisposed)
                throw new ObjectDisposedException(
                    nameof(AudioPlaybackEngine),
                    "The audio playback engine has been disposed.");

            if (volume < 0 || volume > 100)
                throw new ArgumentOutOfRangeException(
                    nameof(volume),
                    volume,
                    "The volume must be between 0 and 100.");

            if (!double.IsFinite(delayMilliseconds) || delayMilliseconds < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(delayMilliseconds),
                    delayMilliseconds,
                    "The audio delay must be finite and non-negative.");

            if (mixer == null)
                throw new InvalidOperationException(
                    "The audio output has not been initialized.");

            var currentMixer = mixer;
            var delayFrames = delayMilliseconds * currentMixer.WaveFormat.SampleRate / 1000;
            if (delayFrames > int.MaxValue / currentMixer.WaveFormat.Channels)
                throw new ArgumentOutOfRangeException(
                    nameof(delayMilliseconds),
                    delayMilliseconds,
                    "The audio delay exceeds the supported sample range.");

            if (frameScheduler?.IsFaulted == true)
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");

            var delaySamples = (int)delayFrames * currentMixer.WaveFormat.Channels;
            var reader = OpenAudioFile(filePath);
            ISampleProvider normalizedProvider;
            VolumeSampleProvider volumeProvider;
            ISampleProvider mixerInput;
            try
            {
                normalizedProvider = NormalizeForMixer(reader, currentMixer.WaveFormat.SampleRate);
                volumeProvider = new VolumeSampleProvider(normalizedProvider);
                volumeProvider.Volume = volume / 100f;
                mixerInput = volumeProvider;
                if (delaySamples > 0)
                {
                    var delayProvider = new OffsetSampleProvider(volumeProvider);
                    delayProvider.DelayBySamples = delaySamples;
                    mixerInput = delayProvider;
                }
            }
            catch (Exception preparationError)
            {
                try
                {
                    reader.Dispose();
                }
                catch (Exception cleanupError)
                {
                    throw new AggregateException(
                        "Audio source preparation failed, and reader cleanup also failed.",
                        preparationError,
                        cleanupError);
                }

                throw;
            }

            lock (activeReadersLock)
            {
                activeReaders[mixerInput] = reader;
            }

            try
            {
                currentMixer.AddMixerInput(mixerInput);
            }
            catch (Exception registrationError)
            {
                try
                {
                    currentMixer.RemoveMixerInput(mixerInput);
                }
                catch (Exception removalError)
                {
                    try
                    {
                        Dispose();
                    }
                    catch (Exception disposalError)
                    {
                        throw new AggregateException(
                            "Audio source registration, mixer rollback, and engine cleanup failed.",
                            registrationError,
                            removalError,
                            disposalError);
                    }

                    throw new AggregateException(
                        "Audio source registration failed, and mixer rollback also failed.",
                        registrationError,
                        removalError);
                }

                WaveStream? readerToDispose = null;
                lock (activeReadersLock)
                {
                    activeReaders.Remove(mixerInput, out readerToDispose);
                }

                if (readerToDispose != null)
                    try
                    {
                        readerToDispose.Dispose();
                    }
                    catch (Exception cleanupError)
                    {
                        throw new AggregateException(
                            "Audio source registration failed, and reader cleanup also failed.",
                            registrationError,
                            cleanupError);
                    }

                throw;
            }
        }
    }

    public void ScheduleAudioSource(string filePath, double eventTimeMilliseconds, int volume = 100)
    {
        lock (playbackStateLock)
        {
            if (isDisposed)
                throw new ObjectDisposedException(
                    nameof(AudioPlaybackEngine),
                    "The audio playback engine has been disposed.");

            var currentMixer = mixer
                               ?? throw new InvalidOperationException(
                                   "The audio output has not been initialized.");

            var currentScheduler = frameScheduler
                                   ?? throw new InvalidOperationException(
                                       "The frame scheduler has not been initialized.");

            if (outputDevice == null)
                throw new InvalidOperationException(
                    "The audio output has not been initialized.");

            if (timelineReader == null)
                throw new InvalidOperationException(
                    "No timeline audio source has been loaded.");

            if (volume < 0 || volume > 100)
                throw new ArgumentOutOfRangeException(
                    nameof(volume),
                    volume,
                    "The volume must be between 0 and 100.");

            if (!double.IsFinite(eventTimeMilliseconds) ||
                eventTimeMilliseconds < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(eventTimeMilliseconds),
                    eventTimeMilliseconds,
                    "The event time must be finite and non-negative.");

            if (eventTimeMilliseconds < timelineBaseMilliseconds)
                throw new ArgumentOutOfRangeException(
                    nameof(eventTimeMilliseconds),
                    eventTimeMilliseconds,
                    "The event time cannot be earlier than the current timeline origin.");

            var relativeMilliseconds = eventTimeMilliseconds - timelineBaseMilliseconds;
            var framePosition = relativeMilliseconds * currentMixer.WaveFormat.SampleRate / 1000.0;
            if (!double.IsFinite(framePosition) ||
                framePosition < 0 ||
                framePosition >= long.MaxValue)
                throw new ArgumentOutOfRangeException(
                    nameof(eventTimeMilliseconds),
                    eventTimeMilliseconds,
                    "The event time exceeds the supported frame range.");

            var scheduledFrame = (long)framePosition;
            if (currentScheduler.IsFaulted)
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");

            var reader = OpenAudioFile(filePath);
            VolumeSampleProvider volumeProvider;
            try
            {
                var normalizedProvider = NormalizeForMixer(reader, currentMixer.WaveFormat.SampleRate);
                volumeProvider = new VolumeSampleProvider(normalizedProvider);
                volumeProvider.Volume = volume / 100f;
            }
            catch (Exception preparationError)
            {
                try
                {
                    reader.Dispose();
                }
                catch (Exception cleanupError)
                {
                    throw new AggregateException(
                        "Scheduled audio source preparation failed, and reader cleanup also failed.",
                        preparationError,
                        cleanupError);
                }

                throw;
            }

            try
            {
                lock (activeReadersLock)
                {
                    activeReaders[volumeProvider] = reader;
                }

                currentScheduler.Schedule(volumeProvider, scheduledFrame);
            }
            catch (Exception schedulingError)
            {
                try
                {
                    lock (activeReadersLock)
                    {
                        reader.Dispose();
                        activeReaders.Remove(volumeProvider);
                    }
                }
                catch (Exception cleanupError)
                {
                    throw new AggregateException(
                        "Audio source scheduling failed, and reader cleanup also failed.",
                        schedulingError,
                        cleanupError);
                }

                throw;
            }
        }
    }

    public void AddLoopingAudioSource(string filePath, double durationMilliseconds, int volume = 100,
        double delayMilliseconds = 0, double sourceOffsetMilliseconds = 0)
    {
        lock (playbackStateLock)
        {
            if (isDisposed)
                throw new ObjectDisposedException(
                    nameof(AudioPlaybackEngine),
                    "The audio playback engine has been disposed.");

            if (volume < 0 || volume > 100)
                throw new ArgumentOutOfRangeException(
                    nameof(volume),
                    volume,
                    "The volume must be between 0 and 100.");

            if (!double.IsFinite(durationMilliseconds) || durationMilliseconds <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(durationMilliseconds),
                    durationMilliseconds,
                    "The loop duration must be finite and positive.");

            if (!double.IsFinite(delayMilliseconds) || delayMilliseconds < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(delayMilliseconds),
                    delayMilliseconds,
                    "The audio delay must be finite and non-negative.");

            if (!double.IsFinite(sourceOffsetMilliseconds) || sourceOffsetMilliseconds < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(sourceOffsetMilliseconds),
                    sourceOffsetMilliseconds,
                    "The loop source offset must be finite and non-negative.");

            if (mixer == null)
                throw new InvalidOperationException(
                    "The audio output has not been initialized.");

            var currentMixer = mixer;
            var durationFrames = durationMilliseconds * currentMixer.WaveFormat.SampleRate / 1000;
            if (durationFrames < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(durationMilliseconds),
                    durationMilliseconds,
                    "The loop duration must produce at least one audio frame.");

            if (durationFrames > int.MaxValue / currentMixer.WaveFormat.Channels)
                throw new ArgumentOutOfRangeException(
                    nameof(durationMilliseconds),
                    durationMilliseconds,
                    "The loop duration exceeds the supported sample range.");

            var takeSamples = (int)durationFrames * currentMixer.WaveFormat.Channels;
            var delayFrames = delayMilliseconds * currentMixer.WaveFormat.SampleRate / 1000;
            if (delayFrames > int.MaxValue / currentMixer.WaveFormat.Channels)
                throw new ArgumentOutOfRangeException(
                    nameof(delayMilliseconds),
                    delayMilliseconds,
                    "The audio delay exceeds the supported sample range.");

            if (frameScheduler?.IsFaulted == true)
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");

            var delaySamples = (int)delayFrames * currentMixer.WaveFormat.Channels;
            var reader = OpenAudioFile(filePath);
            OffsetSampleProvider limitedProvider;
            try
            {
                var loopReader = new LoopingWaveStream(reader);
                var sourceFrameCount = loopReader.Length / loopReader.WaveFormat.BlockAlign;
                var sourceOffsetFrames = sourceOffsetMilliseconds * loopReader.WaveFormat.SampleRate / 1000.0;
                if (!double.IsFinite(sourceOffsetFrames) ||
                    sourceOffsetFrames >= 9007199254740992d)
                    throw new ArgumentOutOfRangeException(
                        nameof(sourceOffsetMilliseconds),
                        sourceOffsetMilliseconds,
                        "The loop source offset exceeds the supported frame range.");

                var phaseFrame = (long)(sourceOffsetFrames % sourceFrameCount);
                loopReader.Position = phaseFrame * loopReader.WaveFormat.BlockAlign;
                var normalizedProvider = NormalizeForMixer(loopReader, currentMixer.WaveFormat.SampleRate);
                var volumeProvider = new VolumeSampleProvider(normalizedProvider);
                volumeProvider.Volume = volume / 100f;
                limitedProvider = new OffsetSampleProvider(volumeProvider);
                limitedProvider.TakeSamples = takeSamples;
                limitedProvider.DelayBySamples = delaySamples;
            }
            catch (Exception preparationError)
            {
                try
                {
                    reader.Dispose();
                }
                catch (Exception cleanupError)
                {
                    throw new AggregateException(
                        "Looping audio source preparation failed, and reader cleanup also failed.",
                        preparationError,
                        cleanupError);
                }

                throw;
            }

            lock (activeReadersLock)
            {
                activeReaders[limitedProvider] = reader;
            }

            try
            {
                currentMixer.AddMixerInput(limitedProvider);
            }
            catch (Exception registrationError)
            {
                try
                {
                    currentMixer.RemoveMixerInput(limitedProvider);
                }
                catch (Exception rollbackError)
                {
                    try
                    {
                        Dispose();
                    }
                    catch (Exception disposalError)
                    {
                        throw new AggregateException(
                            "Looping audio source registration, mixer rollback, and engine cleanup failed.",
                            registrationError,
                            rollbackError,
                            disposalError);
                    }

                    throw new AggregateException(
                        "Looping audio source registration failed, and mixer rollback also failed.",
                        registrationError,
                        rollbackError);
                }

                try
                {
                    WaveStream? readerToDispose = null;
                    lock (activeReadersLock)
                    {
                        activeReaders.TryGetValue(limitedProvider, out readerToDispose);

                        if (readerToDispose != null)
                        {
                            readerToDispose.Dispose();
                            activeReaders.Remove(limitedProvider);
                        }
                    }
                }
                catch (Exception cleanupError)
                {
                    throw new AggregateException(
                        "Looping audio source registration failed, and reader cleanup also failed.",
                        registrationError,
                        cleanupError);
                }

                throw;
            }
        }
    }

    private void OnMixerInputEnded(object? sender, SampleProviderEventArgs e)
    {
        if (e.SampleProvider == timelineProvider)
        {
            timelineProvider = null;
            return;
        }

        WaveStream? reader = null;
        lock (activeReadersLock)
        {
            activeReaders.TryGetValue(e.SampleProvider, out reader);
            if (reader == null) return;
            reader.Dispose();
            activeReaders.Remove(e.SampleProvider);
        }
    }

    public WaveStream OpenAudioFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException(
                "The audio file does not exist.",
                filePath);

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension == ".wav" || extension == ".mp3")
        {
            var reader = new AudioFileReader(filePath);
            return reader;
        }

        if (extension == ".ogg")
        {
            var reader = new VorbisWaveReader(filePath);
            return reader;
        }

        throw new NotSupportedException(
            $"The audio format '{extension}' is not supported.");
    }

    public ISampleProvider NormalizeForMixer(WaveStream reader, int targetSampleRate)
    {
        var provider = reader.ToSampleProvider();

        if (provider.WaveFormat.Channels == 1) provider = new MonoToStereoSampleProvider(provider);

        if (provider.WaveFormat.Channels > 2)
        {
            var stereo = new MultiplexingSampleProvider(new[] { provider }, 2);
            stereo.ConnectInputToOutput(0, 0);
            stereo.ConnectInputToOutput(1, 1);
            provider = stereo;
        }

        if (provider.WaveFormat.SampleRate != targetSampleRate)
        {
            var resampler = new WdlResamplingSampleProvider(provider, targetSampleRate);
            provider = resampler;
        }

        return provider;
    }
}