namespace OsuHitsoundEditor;

public class SampleResolver
{
    // STANDARD SAMPLE LOOKUP
    public (string? BeatmapFilename, string FallbackFilename) GetStandardSampleLookup(HitSoundLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        if (layer.SampleIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(layer.SampleIndex),
                layer.SampleIndex,
                "The sample index must be greater than or equal to zero.");
        }
        if (layer.SampleSet == SampleSetType.Default)
        {
            throw new InvalidOperationException(
                "A standard sample lookup requires a resolved sample set.");
        }
        string sampleSetPrefix;
        string soundName;
        switch (layer.SampleSet)
        {
            case SampleSetType.Normal:
                sampleSetPrefix = "normal";
                break;
            case SampleSetType.Soft:
                sampleSetPrefix = "soft";
                break;
            case SampleSetType.Drum:
                sampleSetPrefix = "drum";
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(layer.SampleSet),
                    layer.SampleSet,
                    "The sample set is not supported for standard sample lookup.");
        }
        switch (layer.Type)
        {
            case HitSoundType.Normal:
                soundName = "hitnormal";
                break;
            case HitSoundType.Whistle:
                soundName = "hitwhistle";
                break;
            case HitSoundType.Finish:
                soundName = "hitfinish";
                break;
            case HitSoundType.Clap:
                soundName = "hitclap";
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(layer.Type),
                    layer.Type,
                    "Standard sample lookup supports only Normal, Whistle, Finish, and Clap hitsounds.");
        }
        string fallbackFilename = sampleSetPrefix + "-" + soundName + ".wav";
        string? beatmapFilename = null;
        if (layer.SampleIndex == 0)
        {
            return (null, fallbackFilename);
        }
        if (layer.SampleIndex == 1)
        {
            beatmapFilename = fallbackFilename;
        }
        if (layer.SampleIndex > 1)
        {
            beatmapFilename = sampleSetPrefix + "-" + soundName + layer.SampleIndex + ".wav";
        }
        return (beatmapFilename, fallbackFilename);
    }
    //SLIDER SAMPLE LOOKUP
    public (string? BeatmapFilename, string FallbackFilename) GetSliderBodySampleLookup(HitSoundLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        if (layer.SampleIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(layer.SampleIndex),
                layer.SampleIndex,
                "The sample index must be greater than or equal to zero.");
        }
        if (layer.SampleSet == SampleSetType.Default)
        {
            throw new InvalidOperationException(
                "A slider body sample lookup requires a resolved sample set.");
        }
        string sampleSetPrefix;
        string soundName;
        switch (layer.SampleSet)
        {
            case SampleSetType.Normal:
                sampleSetPrefix = "normal";
                break;
            case SampleSetType.Soft:
                sampleSetPrefix = "soft";
                break;
            case SampleSetType.Drum:
                sampleSetPrefix = "drum";
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(layer.SampleSet),
                    layer.SampleSet,
                    "The sample set is not supported for slider body sample lookup.");
        }
        switch (layer.Type)
        {
            case HitSoundType.SliderSlide:
                soundName = "sliderslide";
                break;
            case HitSoundType.SliderWhistle:
                soundName = "sliderwhistle";
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(layer.Type),
                    layer.Type,
                    "Slider body sample lookup supports only SliderSlide and SliderWhistle hitsounds.");
        }
        string fallbackFilename = sampleSetPrefix + "-" + soundName + ".wav";
        string? beatmapFilename = null;
        if (layer.SampleIndex == 0)
        {
            return (null, fallbackFilename);
        }
        if (layer.SampleIndex == 1)
        {
            beatmapFilename = fallbackFilename;
        }
        if (layer.SampleIndex > 1)
        {
            beatmapFilename = sampleSetPrefix + "-" + soundName + layer.SampleIndex + ".wav";
        }
        return (beatmapFilename, fallbackFilename);
    }
    public (string? BeatmapFilename, string FallbackFilename) GetSliderTickSampleLookup(HitSoundLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        if (layer.SampleIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(layer.SampleIndex),
                layer.SampleIndex,
                "The sample index must be greater than or equal to zero.");
        }
        if (layer.SampleSet == SampleSetType.Default)
        {
            throw new InvalidOperationException(
                "A slider tick sample lookup requires a resolved sample set.");
        }
        if (layer.Type != HitSoundType.SliderTick)
        {
            throw new ArgumentOutOfRangeException(
                    nameof(layer.Type),
                    layer.Type,
                    "Slider tick sample lookup supports only SliderTick hitsounds.");
        }
        string sampleSetPrefix;
        switch (layer.SampleSet)
        {
            case SampleSetType.Normal:
                sampleSetPrefix = "normal";
                break;
            case SampleSetType.Soft:
                sampleSetPrefix = "soft";
                break;
            case SampleSetType.Drum:
                sampleSetPrefix = "drum";
                break;
            default:
                throw new ArgumentOutOfRangeException(
                        nameof(layer.SampleSet),
                        layer.SampleSet,
                        "The sample set is not supported for slider tick sample lookup.");
        }
        string fallbackFilename = sampleSetPrefix + "-slidertick.wav";
        string? beatmapFilename = null;
        if (layer.SampleIndex == 0)
        {
            return (null, fallbackFilename);
        }
        if (layer.SampleIndex == 1)
        {
            beatmapFilename = fallbackFilename;
        }
        if (layer.SampleIndex > 1)
        {
            beatmapFilename = sampleSetPrefix + "-slidertick" + layer.SampleIndex + ".wav";
        }
        return (beatmapFilename, fallbackFilename);
    }
    //CUSTOM SAMPLE LOOKUP
    public string? ResolveCustomSamplePath(HitSoundLayer layer, string beatmapDirectory)
    {
        ArgumentNullException.ThrowIfNull(layer);
        if (string.IsNullOrWhiteSpace(beatmapDirectory))
        {
            throw new ArgumentException(
                "The beatmap directory must not be empty.",
                nameof(beatmapDirectory));
        }
        if (layer.Type != HitSoundType.Custom)
        {
            throw new ArgumentOutOfRangeException(
                nameof(layer.Type),
                layer.Type,
                "Custom sample lookup supports only Custom hitsounds.");
        }
        if (string.IsNullOrWhiteSpace(layer.Filename))
        {
            throw new InvalidOperationException(
                "A custom sample lookup requires a filename.");
        }
        string samplePath = Path.Combine(beatmapDirectory, layer.Filename);
        if (!File.Exists(samplePath))
        {
            return null;
        }
        return samplePath;
    }
    public string? ResolveBeatmapSamplePath(string? beatmapFilename, string beatmapDirectory)
    {
        if (beatmapFilename == null)
        {
            return null;
        }
        if (string.IsNullOrWhiteSpace(beatmapDirectory))
        {
            throw new ArgumentException(
                "The beatmap directory must not be empty.",
                nameof(beatmapDirectory));
        }
        if (string.IsNullOrWhiteSpace(beatmapFilename))
        {
            throw new ArgumentException(
                "The beatmap filename must not be empty.",
                nameof(beatmapFilename));
        }

        string wavFilename = Path.ChangeExtension(beatmapFilename, ".wav");
        string wavPath = Path.Combine(beatmapDirectory, wavFilename);

        if (File.Exists(wavPath))
        {
            return wavPath;
        }

        string mp3Filename = Path.ChangeExtension(beatmapFilename, ".mp3");
        string mp3Path = Path.Combine(beatmapDirectory, mp3Filename);

        if (File.Exists(mp3Path))
        {
            return mp3Path;
        }

        string oggFilename = Path.ChangeExtension(beatmapFilename, ".ogg");
        string oggPath = Path.Combine(beatmapDirectory, oggFilename);
        if (File.Exists(oggPath))
        {
            return oggPath;
        }
        return null;
    }
    //SAMPLE RESOLUTION
    public SampleResolutionResult ResolveSample(HitSoundLayer layer, string beatmapDirectory)
    {
        ArgumentNullException.ThrowIfNull(layer);
        if (layer.Type == HitSoundType.Custom)
        {
            string? customPath = ResolveCustomSamplePath(layer, beatmapDirectory);

            if (customPath != null)
            {
                return new SampleResolutionResult
                {
                    Outcome = SampleResolutionOutcome.CustomSampleFound,
                    ResolvedPath = customPath
                };
            }
            return new SampleResolutionResult
            {
                Outcome = SampleResolutionOutcome.CustomSampleMissing
            };
        }

        (string? BeatmapFilename, string FallbackFilename) lookup;

        switch (layer.Type)
        {
            case HitSoundType.Normal or
                HitSoundType.Whistle or
                HitSoundType.Finish or
                HitSoundType.Clap:
                lookup = GetStandardSampleLookup(layer);
                break;
            case HitSoundType.SliderSlide:
                lookup = GetSliderBodySampleLookup(layer);
                break;
            case HitSoundType.SliderWhistle:
                lookup = GetSliderBodySampleLookup(layer);
                break;
            case HitSoundType.SliderTick:
                lookup = GetSliderTickSampleLookup(layer);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(layer.Type),
                    layer.Type,
                    "The hitsound type is not supported for sample resolution.");
        }
        string? resolvedPath = ResolveBeatmapSamplePath(lookup.BeatmapFilename, beatmapDirectory);
        if (resolvedPath != null)
        {
            return new SampleResolutionResult
            {
                Outcome = SampleResolutionOutcome.BeatmapSampleFound,
                ResolvedPath = resolvedPath,
                FallbackFilename = lookup.FallbackFilename
            };
        }
        return new SampleResolutionResult
        {
            Outcome = SampleResolutionOutcome.ExternalFallbackRequired,
            ResolvedPath = resolvedPath,
            FallbackFilename = lookup.FallbackFilename
        };
    }
}
