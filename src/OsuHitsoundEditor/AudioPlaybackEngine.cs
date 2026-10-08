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
    public void InitializeOutput()
    {
        if (outputDevice != null)
        {
            throw new InvalidOperationException(
                "The audio output has already been initialized.");
        }
        WasapiPlayerBuilder builder = new WasapiPlayerBuilder()
            .WithLowLatency();
        outputDevice = builder.Build();
        int targetSampleRate = outputDevice.DeviceMixFormat.SampleRate;

        WaveFormat mixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(targetSampleRate, 2);
        mixer = new MixingSampleProvider(mixerFormat);
        mixer.MixerInputEnded += OnMixerInputEnded;
        mixer.ReadFully = true;


        outputDevice.Init(mixer);
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
    public void AddAudioSource(string filePath)
    {

        if (mixer == null)
        {
            throw new InvalidOperationException(
                "The audio output has not been initialized.");
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
        lock (activeReadersLock)
        {
            activeReaders[provider] = reader;
        }
        try
        {
            currentMixer.AddMixerInput(provider);
        }
        catch
        {
            currentMixer.RemoveMixerInput(provider);
            WaveStream? readerToDispose = null;
            lock (activeReadersLock)
            {
                activeReaders.Remove(provider, out readerToDispose);
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
