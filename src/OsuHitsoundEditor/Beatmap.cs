namespace OsuHitsoundEditor;

public class Beatmap
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int Mode { get; set; }
    public string AudioFilename { get; set; } = string.Empty;
    public List<TimingPoint> TimingPoints { get; set; } = new List<TimingPoint>();
    public List<HitObject> HitObjects { get; set; } = new List<HitObject>();
    private int ResolveInheritedValue(int ownValue, int? inheritedValue)
    {
        if (ownValue != 0)
        {
            return ownValue;
        }
        if (inheritedValue != null)
        {
            return inheritedValue.Value;
        }
        else
        {
            return 0;
        }
    }
    public TimingPoint? GetActiveTimingPoint(int time)
    {
        TimingPoint? activeTimingPoint = null;
        foreach (var timingPoint in TimingPoints)
        {
            if (timingPoint.Time <= time)
            {
                activeTimingPoint = timingPoint;
            }
        }
        return activeTimingPoint;
    }
    public int GetEffectiveSampleIndex(HitObject hitObject)
    {
        TimingPoint? activeTimingPoint = GetActiveTimingPoint(hitObject.Time);
        int? inheritedSampleIndex = null;
        if (activeTimingPoint != null)
        {
            inheritedSampleIndex = activeTimingPoint.SampleIndex;
        }
        int result = ResolveInheritedValue(hitObject.HitSample.Index, inheritedSampleIndex);
        return result;
    }
    public int GetEffectiveVolume(HitObject hitObject)
    {
        TimingPoint? activeTimingPoint = GetActiveTimingPoint(hitObject.Time);
        int? inheritedVolume = null;
        if (activeTimingPoint != null)
        {
            inheritedVolume = activeTimingPoint.Volume;
        }
        int result = ResolveInheritedValue(hitObject.HitSample.Volume, inheritedVolume);
        return result;
    }
    public int GetEffectiveNormalSet(HitObject hitObject)
    {
        TimingPoint? activeTimingPoint = GetActiveTimingPoint(hitObject.Time);
        int? inheritedSampleSet = null;
        if (activeTimingPoint != null)
        {
            inheritedSampleSet = activeTimingPoint.SampleSet;
        }
        int result = ResolveInheritedValue(hitObject.HitSample.NormalSet, inheritedSampleSet);
        return result;
    }
    public int GetEffectiveAdditionSet(HitObject hitObject)
    {
        if (hitObject.HitSample.AdditionSet != 0)
        {
            return hitObject.HitSample.AdditionSet;
        }
        else
        {
            int inheritedAddition = GetEffectiveNormalSet(hitObject);
            return inheritedAddition;
        }
    }
    public SampleSetType GetSampleSetType(int sampleSet)
    {
        switch (sampleSet)
        {
            case 1:
                return SampleSetType.Normal;
            case 2:
                return SampleSetType.Soft;
            case 3:
                return SampleSetType.Drum;
            default:
                return SampleSetType.Default;
        }
    }

    public SampleSetType GetEffectiveNormalSetType(HitObject hitObject)
    {
        int effectiveNormalSet = GetEffectiveNormalSet(hitObject);
        SampleSetType sampleSetType = GetSampleSetType(effectiveNormalSet);
        return sampleSetType;
    }

    public SampleSetType GetEffectiveAdditionSetType(HitObject hitObject)
    {
        int effectiveAdditionSet = GetEffectiveAdditionSet(hitObject);
        SampleSetType sampleSetType = GetSampleSetType(effectiveAdditionSet);
        return sampleSetType;
    }

    public List<HitSoundType> GetHitSoundTypes(HitObject hitObject)
    {
        List<HitSoundType> hitSoundTypes = new List<HitSoundType>();

        hitSoundTypes.Add(HitSoundType.Normal);

        if (hitObject.HasWhistle)
        {
            hitSoundTypes.Add(HitSoundType.Whistle);
        }

        if (hitObject.HasFinish)
        {
            hitSoundTypes.Add(HitSoundType.Finish);
        }

        if (hitObject.HasClap)
        {
            hitSoundTypes.Add(HitSoundType.Clap);
        }

        return hitSoundTypes;
    }
}
