using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OsuHitsoundEditor;

public class AudioPlaybackEngine : IDisposable
{
    private WasapiPlayer? outputDevice;
    private MixingSampleProvider? mixer;
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
        if (outputDevice != null)
        {
            outputDevice.Dispose();
            outputDevice = null;
            mixer = null;
        }
    }
}
