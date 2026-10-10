using NAudio.CoreAudioApi;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OsuHitsoundEditor;

public class AudioPlaybackEngine : IDisposable
{
    private WasapiPlayer? outputDevice;
    private MixingSampleProvider? mixer;
    private readonly Dictionary<ISampleProvider, WaveStream> activeReaders = new();
    private WaveStream? timelineReader;
    private ISampleProvider? timelineProvider;
    private readonly object activeReadersLock = new();
    private double timelineBaseMilliseconds;
    public void InitializeOutput()
    {
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
            device.Init(currentMixer);
            outputDevice = device;
            mixer =  currentMixer;
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
    private WasapiPlayer GetInitializedOutputDevice()
    {
        if (outputDevice == null)
        {
            throw new InvalidOperationException(
                "The audio output has not been initialized.");
        }
        return outputDevice;
    }
    public void Play()
    {
        WasapiPlayer device = GetInitializedOutputDevice();
        device.Play();
    }
    public void Pause()
    {
        WasapiPlayer device = GetInitializedOutputDevice();
        device.Pause();
    }
    public void Stop()
    {
        WasapiPlayer device = GetInitializedOutputDevice();
        device.Stop();
        if (timelineReader != null)
        {
            Seek(0);
        }
    }
    public void Seek(double timeMilliseconds)
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
        if (!double.IsFinite(timeMilliseconds) || timeMilliseconds < 0 || timeMilliseconds > timelineReader.TotalTime.TotalMilliseconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeMilliseconds),
                timeMilliseconds,
                $"The seek position must be finite and between 0 and {timelineReader.TotalTime.TotalMilliseconds} milliseconds.");
        }
        bool wasPlaying = device.PlaybackState == PlaybackState.Playing;
        device.Stop();
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
    public double GetTimelinePositionMilliseconds()
    {
        WasapiPlayer device = GetInitializedOutputDevice();
        if (timelineReader == null)
        {
            throw new InvalidOperationException(
                "No timeline audio source has been loaded.");
        }

        long renderedBytes = device.GetPosition();
        double renderedMilliseconds = renderedBytes * 1000.0 / device.OutputWaveFormat.AverageBytesPerSecond;
        return timelineBaseMilliseconds + renderedMilliseconds;
    }
    public void LoadTimelineAudio(string filePath)
    {
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
    public void AddAudioSource(string filePath, int volume = 100)
    {
        if (volume < 0 || volume > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(volume),
                volume,
                "The volume must be between 0 and 100.");
        }
        if (mixer == null)
        {
            throw new InvalidOperationException(
                "The audio output has not been initialized.");
        }
        MixingSampleProvider currentMixer = mixer;
        WaveStream reader = OpenAudioFile(filePath);
        ISampleProvider normalizedProvider;
        VolumeSampleProvider volumeProvider;
        try
        {
            normalizedProvider = NormalizeForMixer(reader, currentMixer.WaveFormat.SampleRate);
            volumeProvider = new VolumeSampleProvider(normalizedProvider);
            volumeProvider.Volume = volume / 100f;
        }
        catch
        {
            reader.Dispose();
            throw;
        }
        lock (activeReadersLock)
        {
            activeReaders[volumeProvider] = reader;
        }
        try
        {
            currentMixer.AddMixerInput(volumeProvider);
        }
        catch
        {
            currentMixer.RemoveMixerInput(volumeProvider);
            WaveStream? readerToDispose = null;
            lock (activeReadersLock)
            {
                activeReaders.Remove(volumeProvider, out readerToDispose);
            }
            if (readerToDispose != null)
            {
                readerToDispose.Dispose();
            }
            throw;
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
        MixingSampleProvider? currentMixer = mixer;

        if (outputDevice != null)
        {
            outputDevice.Dispose();
            outputDevice = null;
        }
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

        if (timelineReader != null)
        {
            timelineReader.Dispose();
            timelineReader = null;
        }

        timelineProvider = null;

        foreach (var reader in readersToDispose)
        {
            reader.Dispose();
        }
        mixer = null;
    }
}
