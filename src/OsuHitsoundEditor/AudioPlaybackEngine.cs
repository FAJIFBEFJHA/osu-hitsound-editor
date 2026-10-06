using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OsuHitsoundEditor;

public class AudioPlaybackEngine
{
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
            stereo.ConnectInputToOutput(0,0);
            stereo.ConnectInputToOutput(1,1);
            provider = stereo;
        }
        if (provider.WaveFormat.SampleRate != targetSampleRate)
        {
            WdlResamplingSampleProvider resampler =new WdlResamplingSampleProvider(provider, targetSampleRate);
            provider = resampler;
        }
        return provider;
    }
}
