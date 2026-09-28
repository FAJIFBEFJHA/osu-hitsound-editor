using System.ComponentModel.Design;

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
        return GetHitSoundTypes(hitObject.HitSound);
    }
    public List<HitSoundLayer> GetHitSoundLayers(HitObject hitObject)
    {
        List<HitSoundType> hitSoundTypes = GetHitSoundTypes(hitObject);
        int sampleIndex = GetEffectiveSampleIndex(hitObject);
        int volume = GetEffectiveVolume(hitObject);
        SampleSetType normalSetType = GetEffectiveNormalSetType(hitObject);
        SampleSetType additionSetType = GetEffectiveAdditionSetType(hitObject);
        string filename = hitObject.HitSample.Filename;

        List<HitSoundLayer> hitSoundLayers = new List<HitSoundLayer>();


        foreach (var hitSoundType in hitSoundTypes)
        {

            SampleSetType sampleSet;
            if (hitSoundType == HitSoundType.Normal)
            {
                HitSoundLayer layer = new HitSoundLayer();
                sampleSet = normalSetType;
                layer.Type = hitSoundType;
                layer.SampleSet = sampleSet;
                layer.SampleIndex = sampleIndex;
                layer.Volume = volume;
                hitSoundLayers.Add(layer);
            }
            if (hitSoundType != HitSoundType.Normal && string.IsNullOrEmpty(filename))
            {
                HitSoundLayer layer = new HitSoundLayer();
                sampleSet = additionSetType;
                layer.Type = hitSoundType;
                layer.SampleSet = sampleSet;
                layer.SampleIndex = sampleIndex;
                layer.Volume = volume;
                hitSoundLayers.Add(layer);
            }
        }
        if (!string.IsNullOrEmpty(filename))
        {
            HitSoundLayer layer = new HitSoundLayer();

            layer.Type = HitSoundType.Custom;
            layer.Filename = filename;
            layer.Volume = volume;
            hitSoundLayers.Add(layer);
        }
        return hitSoundLayers;
    }
    public List<HitSoundType> GetHitSoundTypes(int hitSound)
    {
        List<HitSoundType> hitSoundTypes = new List<HitSoundType>();

        hitSoundTypes.Add(HitSoundType.Normal);
        if ((hitSound & 2) != 0)
        {
            hitSoundTypes.Add(HitSoundType.Whistle);
        }
        if ((hitSound & 4) != 0)
        {
            hitSoundTypes.Add(HitSoundType.Finish);
        }
        if ((hitSound & 8) != 0)
        {
            hitSoundTypes.Add(HitSoundType.Clap);
        }
        return hitSoundTypes;
    }
    public int GetEffectiveNormalSet(SliderEdge sliderEdge, int time)
    {
        TimingPoint? activePoint = GetActiveTimingPoint(time);
        return ResolveInheritedValue(sliderEdge.NormalSet, activePoint?.SampleSet);
    }
    public int GetEffectiveAdditionSet(SliderEdge sliderEdge, int time)
    {
        return ResolveInheritedValue(sliderEdge.AdditionSet, GetEffectiveNormalSet(sliderEdge, time));
    }
    public SampleSetType GetEffectiveNormalSetType(SliderEdge sliderEdge, int time)
    {
        int normalSet = GetEffectiveNormalSet(sliderEdge, time);
        return GetSampleSetType(normalSet);
    }
    public SampleSetType GetEffectiveAdditionSetType(SliderEdge sliderEdge, int time)
    {
        int additionSet = GetEffectiveAdditionSet(sliderEdge, time);
        return GetSampleSetType(additionSet);
    }
    public int GetEffectiveSampleIndex(HitSample hitSample, int time)
    {
        TimingPoint? activePoint = GetActiveTimingPoint(time);
        return ResolveInheritedValue(hitSample.Index, activePoint?.SampleIndex);
    }
    public int GetEffectiveVolume(HitSample hitSample, int time)
    {
        TimingPoint? activePoint = GetActiveTimingPoint(time);
        return ResolveInheritedValue(hitSample.Volume, activePoint?.Volume);
    }
    public List<HitSoundLayer> GetHitSoundLayers(SliderEdge sliderEdge, HitSample hitSample, int time)
    {
        List<HitSoundLayer> hitSoundLayers = new List<HitSoundLayer>();

        List<HitSoundType> hitSoundTypes = GetHitSoundTypes(sliderEdge.HitSound);
        SampleSetType normalSetType = GetEffectiveNormalSetType(sliderEdge, time);
        SampleSetType additionSetType = GetEffectiveAdditionSetType(sliderEdge, time);
        int sampleIndex = GetEffectiveSampleIndex(hitSample, time);
        int volume = GetEffectiveVolume(hitSample, time);

        foreach (var hitSoundType in hitSoundTypes)
        {
            SampleSetType sampleSet;
            if (hitSoundType == HitSoundType.Normal)
            {
                sampleSet = normalSetType;
            }
            else
            {
                sampleSet = additionSetType;
                
            }
            HitSoundLayer layer = new HitSoundLayer();
            layer.Type = hitSoundType;
            layer.SampleSet = sampleSet;
            layer.SampleIndex = sampleIndex;
            layer.Volume = volume;

            hitSoundLayers.Add(layer);
        }
        return hitSoundLayers;
    }
}

