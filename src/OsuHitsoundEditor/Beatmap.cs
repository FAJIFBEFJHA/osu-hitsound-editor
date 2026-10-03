namespace OsuHitsoundEditor;

public class Beatmap
{
    public int BeatmapVersion { get; set; }
    // GENERAL
    public string AudioFilename { get; set; } = string.Empty;
    public int AudioLeadIn { get; set; }
    public int PreviewTime { get; set; } = -1;
    public int Countdown { get; set; } = 1;
    public SampleSetType DefaultSampleSet { get; set; } = SampleSetType.Normal;
    public int Mode { get; set; }
    // EDITOR
    public List<int> Bookmarks { get; set; } = new List<int>();
    public double DistanceSpacing { get; set; }
    public int BeatDivisor { get; set; }
    public int GridSize { get; set; }
    public double TimelineZoom { get; set; }
    // METADATA
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    // DIFFICULTY
    public double HPDrainRate { get; set; }
    public double CircleSize { get; set; }
    public double OverallDifficulty { get; set; }
    public double ApproachRate { get; set; }
    public double SliderMultiplier { get; set; }
    public double SliderTickRate { get; set; }
    // BEATMAP DATA
    public List<TimingPoint> TimingPoints { get; set; } = new List<TimingPoint>();
    public List<HitObject> HitObjects { get; set; } = new List<HitObject>();
    // RESOLUTION HELPERS
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
    // TIMING
    public TimingPoint? GetActiveTimingPoint(double time)
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
    public TimingPoint? GetActiveUninheritedTimingPoint(double time)
    {
        TimingPoint? activeTimingPoint = null;
        foreach (var timingPoint in TimingPoints)
        {
            if (timingPoint.Time <= time && timingPoint.IsUninherited)
            {
                activeTimingPoint = timingPoint;
            }
        }
        return activeTimingPoint;
    }
    public TimingPoint? GetActiveInheritedTimingPoint(double time)
    {
        TimingPoint? activeTimingPoint = GetActiveTimingPoint(time);

        if (activeTimingPoint == null)
        {
            return null;
        }
        if (!activeTimingPoint.IsUninherited)
        {
            return activeTimingPoint;
        }
        return null;
    }
    public double GetEffectiveSliderVelocityMultiplier(double time)
    {
        TimingPoint? timingPoint = GetActiveInheritedTimingPoint(time);
        if (timingPoint == null)
        {
            return 1.0;
        }
        else
        {
            return 100 / -timingPoint.BeatLength;
        }
    }
    //SLIDERS — TIMING
    public double GetSliderSpanDuration(HitObject slider)
    {
        TimingPoint? sliderTiming = GetActiveUninheritedTimingPoint(slider.Time);
        if (SliderMultiplier <= 0)
        {
            throw new InvalidOperationException(
                "It is not possible to calculate slider span duration because SliderMultiplier must be greater than zero.");
        }
        if (sliderTiming == null)
        {
            throw new InvalidOperationException("It is not possible to calculate the slider span duration without an active uninherited timing point.");
        }
        double sliderVelocity = GetEffectiveSliderVelocityMultiplier(slider.Time);
        return slider.Length / (SliderMultiplier * 100 * sliderVelocity) * sliderTiming.BeatLength;
    }
    public double GetSliderDuration(HitObject slider)
    {
        if (slider.Slides <= 0)
        {
            throw new InvalidOperationException("It is not possible to calculate the slider duration because the slider must have at least one span.");
        }
        return GetSliderSpanDuration(slider) * slider.Slides;
    }
    public double GetSliderEdgeTime(HitObject slider, int edgeIndex)
    {
        if (slider.Slides <= 0)
        {
            throw new InvalidOperationException("It is not possible to calculate a slider edge time because the slider must have at least one span.");
        }
        if (edgeIndex < 0 || edgeIndex > slider.Slides)
        {
            throw new ArgumentOutOfRangeException(nameof(edgeIndex), edgeIndex, $"The slider edge index must be between 0 and {slider.Slides}.");
        }
        return slider.Time + GetSliderSpanDuration(slider) * edgeIndex;
    }
    public double GetSliderTickDistance(HitObject slider)
    {
        if (SliderTickRate <= 0)
        {
            throw new InvalidOperationException("It is not possible to calculate slider tick distance because SliderTickRate must be greater than zero.");
        }
        if (SliderMultiplier <= 0)
        {
            throw new InvalidOperationException("It is not possible to calculate slider tick distance because SliderMultiplier must be greater than zero.");
        }
        double baseTickDistance = SliderMultiplier * 100 / SliderTickRate;
        if (BeatmapVersion < 8)
        {
            return baseTickDistance;
        }
        double sliderVelocity = GetEffectiveSliderVelocityMultiplier(slider.Time);
        return baseTickDistance * sliderVelocity;
    }
    public List<double> GetSliderTickTimes(HitObject slider)
    {
        if (slider.Slides <= 0)
        {
            throw new InvalidOperationException("It is not possible to calculate slider tick times because the slider must have at least one span.");
        }
        double spanDuration = GetSliderSpanDuration(slider);
        double tickDistance = GetSliderTickDistance(slider);
        double sliderVelocity = slider.Length / spanDuration;
        double minDistanceFromEnd = sliderVelocity * 10;
        List<double> tickTimes = new List<double>();
        for (int span = 0; span < slider.Slides; span++)
        {
            double spanStartTime = slider.Time + spanDuration * span;
            bool reversed = span % 2 != 0;
            List<double> spanTickTimes = new List<double>();
            for (double distance = tickDistance; distance <= slider.Length; distance = distance + tickDistance)
            {

                if (distance >= slider.Length - minDistanceFromEnd)
                {
                    break;
                }
                double pathProgress = distance / slider.Length;
                double timeProgress;
                if (reversed)
                {
                    timeProgress = 1 - pathProgress;
                }
                else
                {
                    timeProgress = pathProgress;
                }
                double tickTime = spanStartTime + timeProgress * spanDuration;
                spanTickTimes.Add(tickTime);
            }
            if (reversed)
            {
                spanTickTimes.Reverse();
            }
            tickTimes.AddRange(spanTickTimes);
        }
        return tickTimes;
    }
    // SAMPLE SETS
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
    public SampleSetType GetResolvedSampleSetType(int sampleSet)
    {
        SampleSetType sampleSetType = GetSampleSetType(sampleSet);
        if (sampleSetType == SampleSetType.Default)
        {
            return DefaultSampleSet;
        }
        else
        {
            return sampleSetType;
        }
    }
    // HITOBJECT — EFFECTIVE VALUES
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
    public SampleSetType GetEffectiveNormalSetType(HitObject hitObject)
    {
        int effectiveNormalSet = GetEffectiveNormalSet(hitObject);
        SampleSetType sampleSetType = GetResolvedSampleSetType(effectiveNormalSet);
        return sampleSetType;
    }
    public SampleSetType GetEffectiveAdditionSetType(HitObject hitObject)
    {
        int effectiveAdditionSet = GetEffectiveAdditionSet(hitObject);
        SampleSetType sampleSetType = GetResolvedSampleSetType(effectiveAdditionSet);
        return sampleSetType;
    }
    //SLIDER EDGE — EFFECTIVE VALUES
    public int GetEffectiveNormalSet(SliderEdge sliderEdge, double time)
    {
        TimingPoint? activePoint = GetActiveTimingPoint(time);
        return ResolveInheritedValue(sliderEdge.NormalSet, activePoint?.SampleSet);
    }
    public int GetEffectiveAdditionSet(SliderEdge sliderEdge, double time)
    {
        return ResolveInheritedValue(sliderEdge.AdditionSet, GetEffectiveNormalSet(sliderEdge, time));
    }
    public SampleSetType GetEffectiveNormalSetType(SliderEdge sliderEdge, double time)
    {
        int normalSet = GetEffectiveNormalSet(sliderEdge, time);
        return GetResolvedSampleSetType(normalSet);
    }
    public SampleSetType GetEffectiveAdditionSetType(SliderEdge sliderEdge, double time)
    {
        int additionSet = GetEffectiveAdditionSet(sliderEdge, time);
        return GetResolvedSampleSetType(additionSet);
    }
    public int GetEffectiveSampleIndex(HitSample hitSample, double time)
    {
        TimingPoint? activePoint = GetActiveTimingPoint(time);
        return ResolveInheritedValue(hitSample.Index, activePoint?.SampleIndex);
    }
    public int GetEffectiveVolume(HitSample hitSample, double time)
    {
        TimingPoint? activePoint = GetActiveTimingPoint(time);
        return ResolveInheritedValue(hitSample.Volume, activePoint?.Volume);
    }
    //HITSOUND TYPES
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
    public List<HitSoundType> GetHitSoundTypes(HitObject hitObject)
    {
        return GetHitSoundTypes(hitObject.HitSound);
    }
    //HITSOUND LAYERS
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
    public List<HitSoundLayer> GetHitSoundLayers(SliderEdge sliderEdge, HitSample hitSample, double time)
    {
        List<HitSoundLayer> hitSoundLayers = new List<HitSoundLayer>();

        List<HitSoundType> hitSoundTypes = GetHitSoundTypes(sliderEdge.HitSound);
        SampleSetType normalSetType = GetEffectiveNormalSetType(sliderEdge, time);
        SampleSetType additionSetType = GetEffectiveAdditionSetType(sliderEdge, time);
        int sampleIndex = GetEffectiveSampleIndex(hitSample, time);
        int volume = GetEffectiveVolume(hitSample, time);
        string filename = hitSample.Filename;

        foreach (var hitSoundType in hitSoundTypes)
        {
            if (hitSoundType == HitSoundType.Normal)
            {
                HitSoundLayer layer = new HitSoundLayer();
                layer.Type = hitSoundType;
                layer.SampleSet = normalSetType;
                layer.SampleIndex = sampleIndex;
                layer.Volume = volume;
                hitSoundLayers.Add(layer);
            }
            if (hitSoundType != HitSoundType.Normal && string.IsNullOrEmpty(filename))
            {
                HitSoundLayer layer = new HitSoundLayer();
                layer.Type = hitSoundType;
                layer.SampleSet = additionSetType;
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
    public List<HitSoundLayer> GetSliderEdgeHitSoundLayers(HitObject slider, int edgeIndex)
    {
        double edgeTime = GetSliderEdgeTime(slider, edgeIndex);
        if (slider.SliderEdges.Count != slider.Slides + 1)
        {
            throw new InvalidOperationException($"It is not possible to resolve slider edge hitsounds because the slider has {slider.SliderEdges.Count} edges but requires {slider.Slides + 1}.");
        }
        SliderEdge sliderEdge = slider.SliderEdges[edgeIndex];
        return GetHitSoundLayers(sliderEdge, slider.HitSample, edgeTime);
    }
    public List<HitSoundLayer> GetSliderBodyHitSoundLayers(HitObject slider)
    {
        int sampleIndex = GetEffectiveSampleIndex(slider);
        int volume = GetEffectiveVolume(slider);
        SampleSetType normalSet = GetEffectiveNormalSetType(slider);
        SampleSetType additionSet = GetEffectiveAdditionSetType(slider);
        List<HitSoundLayer> hitSoundLayers = new List<HitSoundLayer>();
        HitSoundLayer sliderSlide = new HitSoundLayer();
        sliderSlide.Type = HitSoundType.SliderSlide;
        sliderSlide.SampleSet = normalSet;
        sliderSlide.SampleIndex = sampleIndex;
        sliderSlide.Volume = volume;
        hitSoundLayers.Add(sliderSlide);
        if (slider.HasWhistle)
        {
            HitSoundLayer sliderWhistle = new HitSoundLayer();
            sliderWhistle.Type = HitSoundType.SliderWhistle;
            sliderWhistle.SampleSet = additionSet;
            sliderWhistle.SampleIndex = sampleIndex;
            sliderWhistle.Volume = volume;
            hitSoundLayers.Add(sliderWhistle);
        }
        return hitSoundLayers;
    }
    public List<HitSoundLayer> GetSliderTickHitSoundLayers(HitObject slider)
    {
        int sampleIndex = GetEffectiveSampleIndex(slider);
        int volume = GetEffectiveVolume(slider);
        SampleSetType normalSet = GetEffectiveNormalSetType(slider);
        List<HitSoundLayer> hitSoundLayers = new List<HitSoundLayer>();
        HitSoundLayer sliderTick = new HitSoundLayer();
        sliderTick.Type = HitSoundType.SliderTick;
        sliderTick.SampleSet = normalSet;
        sliderTick.SampleIndex = sampleIndex;
        sliderTick.Volume = volume;
        hitSoundLayers.Add(sliderTick);
        return hitSoundLayers;
    }
}