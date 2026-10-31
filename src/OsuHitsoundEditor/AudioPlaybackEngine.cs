using NAudio.CoreAudioApi;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OsuHitsoundEditor;

public class AudioPlaybackEngine : IDisposable
{
    private WasapiPlayer? outputDevice;
    private MixingSampleProvider? mixer;
    private FrameScheduledSampleProvider? frameScheduler;
    private readonly Dictionary<ISampleProvider, WaveStream> activeReaders = new();
    private WaveStream? timelineReader;
    private ISampleProvider? timelineProvider;
    private readonly object activeReadersLock = new();
    private double timelineBaseMilliseconds;
    private readonly object playbackStateLock = new();
    private bool isDisposed = false;

    public void InitializeOutput()
    {
        lock (playbackStateLock)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(
                    nameof(AudioPlaybackEngine),
                    "The audio playback engine has been disposed.");
            }

            if (outputDevice != null)
            {
                throw new InvalidOperationException(
                    "The audio output has already been initialized.");
            }

            WasapiPlayerBuilder builder = new WasapiPlayerBuilder()
                .WithLowLatency();
            WasapiPlayer device = builder.Build();
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
                FrameScheduledSampleProvider currentScheduler = new FrameScheduledSampleProvider(currentMixer);
                device.Init(currentScheduler);
                outputDevice = device;
                mixer = currentMixer;
                frameScheduler = currentScheduler;
            }
            catch
            {
                if (currentMixer != null)
                {
                    currentMixer.MixerInputEnded -= OnMixerInputEnded;
                }

                device.Dispose();
                throw;
            }
        }
    }

    private WasapiPlayer GetInitializedOutputDevice()
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(
                nameof(AudioPlaybackEngine),
                "The audio playback engine has been disposed.");
        }

        if (outputDevice == null)
        {
            throw new InvalidOperationException(
                "The audio output has not been initialized.");
        }

        return outputDevice;
    }

    public void Play()
    {
        lock (playbackStateLock)
        {
            WasapiPlayer device = GetInitializedOutputDevice();
            FrameScheduledSampleProvider currentScheduler = frameScheduler
                                                            ?? throw new InvalidOperationException(
                                                                "The frame scheduler has not been initialized.");

            if (currentScheduler.IsFaulted)
            {
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");
            }

            device.Play();
        }
    }

    public void Pause()
    {
        lock (playbackStateLock)
        {
            WasapiPlayer device = GetInitializedOutputDevice();
            device.Pause();
        }
    }

    public void Stop()
    {
        lock (playbackStateLock)
        {
            WasapiPlayer device = GetInitializedOutputDevice();
            device.Stop();
            FrameScheduledSampleProvider currentScheduler = frameScheduler
                                                            ?? throw new InvalidOperationException(
                                                                "The frame scheduler has not been initialized.");
            if (timelineReader != null && !currentScheduler.IsFaulted)
            {
                Seek(0);
                return;
            }

            MixingSampleProvider currentMixer = mixer
                                                ?? throw new InvalidOperationException(
                                                    "The audio output has not been initialized.");
            currentScheduler.ResetScheduling();
            currentMixer.RemoveAllMixerInputs();
            timelineProvider = null;
            List<WaveStream> readersToDispose = new List<WaveStream>();
            lock (activeReadersLock)
            {
                readersToDispose.AddRange(activeReaders.Values);
                activeReaders.Clear();
            }

            foreach (var reader in readersToDispose)
            {
                reader.Dispose();
            }

            timelineBaseMilliseconds = 0;
        }
    }

    public void Seek(double timeMilliseconds)
    {
        lock (playbackStateLock)
        {
            WasapiPlayer device = GetInitializedOutputDevice();
            if (mixer == null)
            {
                throw new InvalidOperationException(
                    "The audio output has not been initialized.");
            }

            if (timelineReader == null)
            {
                throw new InvalidOperationException(
                    "No timeline audio source has been loaded.");
            }

            if (!double.IsFinite(timeMilliseconds) || timeMilliseconds < 0 ||
                timeMilliseconds > timelineReader.TotalTime.TotalMilliseconds)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeMilliseconds),
                    timeMilliseconds,
                    $"The seek position must be finite and between 0 and {timelineReader.TotalTime.TotalMilliseconds} milliseconds.");
            }

            FrameScheduledSampleProvider currentScheduler = frameScheduler
                                                            ?? throw new InvalidOperationException(
                                                                "The frame scheduler has not been initialized.");
            if (currentScheduler.IsFaulted)
            {
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");
            }

            bool wasPlaying = device.PlaybackState == PlaybackState.Playing;
            device.Stop();
            if (currentScheduler.IsFaulted)
            {
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");
            }

            currentScheduler.ResetScheduling();
            mixer.RemoveAllMixerInputs();
            timelineProvider = null;
            List<WaveStream> readersToDispose = new List<WaveStream>();
            lock (activeReadersLock)
            {
                readersToDispose.AddRange(activeReaders.Values);
                activeReaders.Clear();
            }

            foreach (var reader in readersToDispose)
            {
                reader.Dispose();
            }

            timelineReader.CurrentTime = TimeSpan.FromMilliseconds(timeMilliseconds);
            ISampleProvider provider = NormalizeForMixer(timelineReader, mixer.WaveFormat.SampleRate);
            mixer.AddMixerInput(provider);
            timelineProvider = provider;
            timelineBaseMilliseconds = timeMilliseconds;
            if (wasPlaying)
            {
                device.Play();
            }
        }
    }

    public double GetTimelinePositionMilliseconds()
    {
        lock (playbackStateLock)
        {
            WasapiPlayer device = GetInitializedOutputDevice();
            if (timelineReader == null)
            {
                throw new InvalidOperationException(
                    "No timeline audio source has been loaded.");
            }

            FrameScheduledSampleProvider currentScheduler = frameScheduler
                                                            ?? throw new InvalidOperationException(
                                                                "The frame scheduler has not been initialized.");

            if (currentScheduler.IsFaulted)
            {
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");
            }

            long renderedBytes = device.GetPosition();
            double renderedMilliseconds = renderedBytes * 1000.0 / device.OutputWaveFormat.AverageBytesPerSecond;
            return timelineBaseMilliseconds + renderedMilliseconds;
        }
    }

    public void LoadTimelineAudio(string filePath)
    {
        lock (playbackStateLock)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(
                    nameof(AudioPlaybackEngine),
                    "The audio playback engine has been disposed.");
            }

            if (mixer == null)
            {
                throw new InvalidOperationException(
                    "The audio output has not been initialized.");
            }

            if (timelineReader != null)
            {
                throw new InvalidOperationException(
                    "A timeline audio source has already been loaded.");
            }

            if (frameScheduler?.IsFaulted == true)
            {
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");
            }

            MixingSampleProvider currentMixer = mixer;
            WaveStream reader = OpenAudioFile(filePath);
            ISampleProvider provider;
            try
            {
                provider = NormalizeForMixer(reader, currentMixer.WaveFormat.SampleRate);
            }
            catch
            {
                reader.Dispose();
                throw;
            }

            timelineReader = reader;
            timelineProvider = provider;
            try
            {
                currentMixer.AddMixerInput(provider);
            }
            catch
            {
                currentMixer.RemoveMixerInput(provider);
                timelineProvider = null;
                timelineReader = null;
                reader.Dispose();
                throw;
            }
        }
    }

    public void AddAudioSource(string filePath, int volume = 100, double delayMilliseconds = 0)
    {
        lock (playbackStateLock)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(
                    nameof(AudioPlaybackEngine),
                    "The audio playback engine has been disposed.");
            }

            if (volume < 0 || volume > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(volume),
                    volume,
                    "The volume must be between 0 and 100.");
            }

            if (!double.IsFinite(delayMilliseconds) || delayMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(delayMilliseconds),
                    delayMilliseconds,
                    "The audio delay must be finite and non-negative.");
            }

            if (mixer == null)
            {
                throw new InvalidOperationException(
                    "The audio output has not been initialized.");
            }

            MixingSampleProvider currentMixer = mixer;
            double delayFrames = delayMilliseconds * currentMixer.WaveFormat.SampleRate / 1000;
            if (delayFrames > int.MaxValue / currentMixer.WaveFormat.Channels)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(delayMilliseconds),
                    delayMilliseconds,
                    "The audio delay exceeds the supported sample range.");
            }

            if (frameScheduler?.IsFaulted == true)
            {
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");
            }

            int delaySamples = (int)delayFrames * currentMixer.WaveFormat.Channels;
            WaveStream reader = OpenAudioFile(filePath);
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
                    OffsetSampleProvider delayProvider = new OffsetSampleProvider(volumeProvider);
                    delayProvider.DelayBySamples = delaySamples;
                    mixerInput = delayProvider;
                }
            }
            catch
            {
                reader.Dispose();
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
            catch
            {
                currentMixer.RemoveMixerInput(mixerInput);
                WaveStream? readerToDispose = null;
                lock (activeReadersLock)
                {
                    activeReaders.Remove(mixerInput, out readerToDispose);
                }

                if (readerToDispose != null)
                {
                    readerToDispose.Dispose();
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
            {
                throw new ObjectDisposedException(
                    nameof(AudioPlaybackEngine),
                    "The audio playback engine has been disposed.");
            }

            MixingSampleProvider currentMixer = mixer
                                                ?? throw new InvalidOperationException(
                                                    "The audio output has not been initialized.");

            FrameScheduledSampleProvider currentScheduler = frameScheduler
                                                            ?? throw new InvalidOperationException(
                                                                "The frame scheduler has not been initialized.");

            if (outputDevice == null)
            {
                throw new InvalidOperationException(
                    "The audio output has not been initialized.");
            }

            if (timelineReader == null)
            {
                throw new InvalidOperationException(
                    "No timeline audio source has been loaded.");
            }

            if (volume < 0 || volume > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(volume),
                    volume,
                    "The volume must be between 0 and 100.");
            }

            if (!double.IsFinite(eventTimeMilliseconds) ||
                eventTimeMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(eventTimeMilliseconds),
                    eventTimeMilliseconds,
                    "The event time must be finite and non-negative.");
            }

            if (eventTimeMilliseconds < timelineBaseMilliseconds)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(eventTimeMilliseconds),
                    eventTimeMilliseconds,
                    "The event time cannot be earlier than the current timeline origin.");
            }

            double relativeMilliseconds = eventTimeMilliseconds - timelineBaseMilliseconds;
            double framePosition = relativeMilliseconds * currentMixer.WaveFormat.SampleRate / 1000.0;
            if (!double.IsFinite(framePosition) ||
                framePosition < 0 ||
                framePosition >= long.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(eventTimeMilliseconds),
                    eventTimeMilliseconds,
                    "The event time exceeds the supported frame range.");
            }

            long scheduledFrame = (long)framePosition;
            if (currentScheduler.IsFaulted)
            {
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");
            }

            WaveStream reader = OpenAudioFile(filePath);
            VolumeSampleProvider volumeProvider;
            try
            {
                ISampleProvider normalizedProvider = NormalizeForMixer(reader, currentMixer.WaveFormat.SampleRate);
                volumeProvider = new VolumeSampleProvider(normalizedProvider);
                volumeProvider.Volume = volume / 100f;
            }
            catch
            {
                reader.Dispose();
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
            catch
            {
                lock (activeReadersLock)
                {
                    activeReaders.Remove(volumeProvider);
                }

                reader.Dispose();
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
            {
                throw new ObjectDisposedException(
                    nameof(AudioPlaybackEngine),
                    "The audio playback engine has been disposed.");
            }

            if (volume < 0 || volume > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(volume),
                    volume,
                    "The volume must be between 0 and 100.");
            }

            if (!double.IsFinite(durationMilliseconds) || durationMilliseconds <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationMilliseconds),
                    durationMilliseconds,
                    "The loop duration must be finite and positive.");
            }

            if (!double.IsFinite(delayMilliseconds) || delayMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(delayMilliseconds),
                    delayMilliseconds,
                    "The audio delay must be finite and non-negative.");
            }

            if (!double.IsFinite(sourceOffsetMilliseconds) || sourceOffsetMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourceOffsetMilliseconds),
                    sourceOffsetMilliseconds,
                    "The loop source offset must be finite and non-negative.");
            }

            if (mixer == null)
            {
                throw new InvalidOperationException(
                    "The audio output has not been initialized.");
            }

            MixingSampleProvider currentMixer = mixer;
            double durationFrames = durationMilliseconds * currentMixer.WaveFormat.SampleRate / 1000;
            if (durationFrames < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationMilliseconds),
                    durationMilliseconds,
                    "The loop duration must produce at least one audio frame.");
            }

            if (durationFrames > int.MaxValue / currentMixer.WaveFormat.Channels)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationMilliseconds),
                    durationMilliseconds,
                    "The loop duration exceeds the supported sample range.");
            }

            int takeSamples = (int)durationFrames * currentMixer.WaveFormat.Channels;
            double delayFrames = delayMilliseconds * currentMixer.WaveFormat.SampleRate / 1000;
            if (delayFrames > int.MaxValue / currentMixer.WaveFormat.Channels)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(delayMilliseconds),
                    delayMilliseconds,
                    "The audio delay exceeds the supported sample range.");
            }

            if (frameScheduler?.IsFaulted == true)
            {
                throw new InvalidOperationException(
                    "The frame scheduler is faulted and cannot be used.");
            }

            int delaySamples = (int)delayFrames * currentMixer.WaveFormat.Channels;
            WaveStream reader = OpenAudioFile(filePath);
            OffsetSampleProvider limitedProvider;
            try
            {
                LoopingWaveStream loopReader = new LoopingWaveStream(reader);
                long sourceFrameCount = loopReader.Length / loopReader.WaveFormat.BlockAlign;
                double sourceOffsetFrames = sourceOffsetMilliseconds * loopReader.WaveFormat.SampleRate / 1000.0;
                if (!double.IsFinite(sourceOffsetFrames) ||
                    sourceOffsetFrames >= 9007199254740992d)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(sourceOffsetMilliseconds),
                        sourceOffsetMilliseconds,
                        "The loop source offset exceeds the supported frame range.");
                }

                long phaseFrame = (long)(sourceOffsetFrames % sourceFrameCount);
                loopReader.Position = phaseFrame * loopReader.WaveFormat.BlockAlign;
                ISampleProvider normalizedProvider = NormalizeForMixer(loopReader, currentMixer.WaveFormat.SampleRate);
                VolumeSampleProvider volumeProvider = new VolumeSampleProvider(normalizedProvider);
                volumeProvider.Volume = volume / 100f;
                limitedProvider = new OffsetSampleProvider(volumeProvider);
                limitedProvider.TakeSamples = takeSamples;
                limitedProvider.DelayBySamples = delaySamples;
            }
            catch
            {
                reader.Dispose();
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
            catch
            {
                currentMixer.RemoveMixerInput(limitedProvider);
                WaveStream? readerToDispose = null;
                lock (activeReadersLock)
                {
                    activeReaders.Remove(limitedProvider, out readerToDispose);
                }

                if (readerToDispose != null)
                {
                    readerToDispose.Dispose();
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
            activeReaders.Remove(e.SampleProvider, out reader);
        }

        if (reader != null)
        {
            reader.Dispose();
        }
    }

    public WaveStream OpenAudioFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "The audio file does not exist.",
                filePath);
        }

        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension == ".wav" || extension == ".mp3")
        {
            AudioFileReader reader = new AudioFileReader(filePath);
            return reader;
        }
        else if (extension == ".ogg")
        {
            VorbisWaveReader reader = new VorbisWaveReader(filePath);
            return reader;
        }
        else
        {
            throw new NotSupportedException(
                $"The audio format '{extension}' is not supported.");
        }
    }

    public ISampleProvider NormalizeForMixer(WaveStream reader, int targetSampleRate)
    {
        ISampleProvider provider = reader.ToSampleProvider();

        if (provider.WaveFormat.Channels == 1)
        {
            provider = new MonoToStereoSampleProvider(provider);
        }

        if (provider.WaveFormat.Channels > 2)
        {
            MultiplexingSampleProvider stereo = new MultiplexingSampleProvider(new[] { provider }, 2);
            stereo.ConnectInputToOutput(0, 0);
            stereo.ConnectInputToOutput(1, 1);
            provider = stereo;
        }

        if (provider.WaveFormat.SampleRate != targetSampleRate)
        {
            WdlResamplingSampleProvider resampler = new WdlResamplingSampleProvider(provider, targetSampleRate);
            provider = resampler;
        }

        return provider;
    }

    public void Dispose()
    {
        lock (playbackStateLock)
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            List<Exception> disposalErrors = new();
            MixingSampleProvider? currentMixer = mixer;
            if (outputDevice != null)
            {
                outputDevice.Dispose();
                outputDevice = null;
            }

            if (timelineReader != null)
            {
                WaveStream reader = timelineReader;
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
                currentMixer.RemoveAllMixerInputs();
            }

            List<WaveStream> readersToDispose = new List<WaveStream>();

            lock (activeReadersLock)
            {
                readersToDispose.AddRange(activeReaders.Values);
                activeReaders.Clear();
            }

            timelineProvider = null;

            foreach (WaveStream reader in readersToDispose)
            {
                try
                {
                    reader.Dispose();
                }
                catch (Exception exception)
                {
                    disposalErrors.Add(exception);
                }
            }

            mixer = null;
            if (disposalErrors.Count > 0)
            {
                throw new AggregateException(
                    "One or more audio resources could not be disposed.",
                    disposalErrors);
            }
        }
    }
}