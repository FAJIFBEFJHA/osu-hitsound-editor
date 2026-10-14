namespace OsuHitsoundEditor;

public class HitSoundPlaybackService
{
    public SampleResolutionResult PlayHitSoundLayer(HitSoundLayer layer, string beatmapDirectory, SampleResolver resolver, AudioPlaybackEngine engine, double delayMilliseconds = 0)
    {
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(engine);
        if (string.IsNullOrWhiteSpace(beatmapDirectory))
        {
            throw new ArgumentException(
                "The beatmap directory must not be empty.",
                nameof(beatmapDirectory));
        }
        if (!double.IsFinite(delayMilliseconds) || delayMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(delayMilliseconds),
                delayMilliseconds,
                "The audio delay must be finite and non-negative.");
        }
        SampleResolutionResult result = resolver.ResolveSample(layer, beatmapDirectory);
        if (result.Outcome == SampleResolutionOutcome.CustomSampleMissing)
        {
            return result;
        }
        if (result.Outcome == SampleResolutionOutcome.ExternalFallbackRequired)
        {
            return result;
        }
        if (result.Outcome == SampleResolutionOutcome.BeatmapSampleFound || result.Outcome == SampleResolutionOutcome.CustomSampleFound)
        {
            if (result.ResolvedPath == null)
            {
                throw new InvalidOperationException(
                    "A successful sample resolution must include a playable file path.");
            }
            engine.AddAudioSource(result.ResolvedPath, layer.Volume, delayMilliseconds);
            return result;
        }
        throw new InvalidOperationException(
            $"The sample resolution outcome '{result.Outcome}' is not supported for playback.");
    }
    public List<SampleResolutionResult> PlayHitSoundLayers(IReadOnlyList<HitSoundLayer> layers, string beatmapDirectory, SampleResolver resolver, AudioPlaybackEngine engine, double delayMilliseconds = 0)
    {
        ArgumentNullException.ThrowIfNull(layers);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(engine);
        if (string.IsNullOrWhiteSpace(beatmapDirectory))
        {
            throw new ArgumentException(
                "The beatmap directory must not be empty.",
                nameof(beatmapDirectory));
        }
        if (!double.IsFinite(delayMilliseconds) || delayMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(delayMilliseconds),
                delayMilliseconds,
                "The audio delay must be finite and non-negative.");
        }
        List<SampleResolutionResult> results = new List<SampleResolutionResult>();
        SampleResolutionResult result;
        foreach (var layer in layers)
        {
            result = PlayHitSoundLayer(layer, beatmapDirectory, resolver, engine, delayMilliseconds);
            results.Add(result);
        }
        return results;
    }
    public List<SampleResolutionResult> PlayHitCircle(Beatmap beatmap, HitObject hitObject, string beatmapDirectory, SampleResolver resolver, AudioPlaybackEngine engine, double delayMilliseconds = 0)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(hitObject);
        if (hitObject.IsHitCircle == false)
        {
            throw new ArgumentException(
                "The hit object must be a hit circle.",
                nameof(hitObject));
        }
        if (!double.IsFinite(delayMilliseconds) || delayMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(delayMilliseconds),
                delayMilliseconds,
                "The audio delay must be finite and non-negative.");
        }
        List<HitSoundLayer> layers = beatmap.GetHitSoundLayers(hitObject);
        List<SampleResolutionResult> results = PlayHitSoundLayers(layers, beatmapDirectory, resolver, engine, delayMilliseconds);
        return results;
    }
    public List<SampleResolutionResult> PlaySliderEdge(Beatmap beatmap, HitObject slider, int edgeIndex, string beatmapDirectory, SampleResolver resolver, AudioPlaybackEngine engine, double delayMilliseconds = 0)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(slider);
        if (slider.IsSlider == false)
        {
            throw new ArgumentException(
                "The hit object must be a slider.",
                nameof(slider));
        }
        if (!double.IsFinite(delayMilliseconds) || delayMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(delayMilliseconds),
                delayMilliseconds,
                "The audio delay must be finite and non-negative.");
        }
        List<HitSoundLayer> layers = beatmap.GetSliderEdgeHitSoundLayers(slider, edgeIndex);
        List<SampleResolutionResult> results = PlayHitSoundLayers(layers, beatmapDirectory, resolver, engine, delayMilliseconds);
        return results;
    }
    public List<(int EdgeIndex, double EdgeTime, List<SampleResolutionResult> Results)> PlaySliderEdgesInTimeRange(Beatmap beatmap, HitObject slider, double startExclusiveMilliseconds, double endInclusiveMilliseconds, string beatmapDirectory, SampleResolver resolver, AudioPlaybackEngine engine)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(slider);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(engine);
        if (string.IsNullOrWhiteSpace(beatmapDirectory))
        {
            throw new ArgumentException(
                "The beatmap directory must not be empty.",
                nameof(beatmapDirectory));
        }
        List<(int EdgeIndex, double EdgeTime)> matchingEdges = beatmap.GetSliderEdgeTimesInRange(slider, startExclusiveMilliseconds, endInclusiveMilliseconds);
        List<(int EdgeIndex, double EdgeTime, List<SampleResolutionResult> Results)> results = new List<(int EdgeIndex, double EdgeTime, List<SampleResolutionResult> Results)>();
        foreach (var edge in matchingEdges)
        {
            int edgeIndex = edge.EdgeIndex;
            double edgeTime = edge.EdgeTime;
            List<SampleResolutionResult> edgeResults = PlaySliderEdge(beatmap, slider, edgeIndex, beatmapDirectory, resolver, engine);
            results.Add((edgeIndex, edgeTime, edgeResults));
        }
        return results;
    }
    public List<SampleResolutionResult> PlaySliderBody(Beatmap beatmap, HitObject slider, string beatmapDirectory, SampleResolver resolver, AudioPlaybackEngine engine, double delayMilliseconds = 0)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(slider);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(engine);
        if (string.IsNullOrWhiteSpace(beatmapDirectory))
        {
            throw new ArgumentException(
                "The beatmap directory must not be empty.",
                nameof(beatmapDirectory));
        }
        if (!slider.IsSlider)
        {
            throw new ArgumentException(
                "The hit object must be a slider.",
                nameof(slider));
        }
        if (!double.IsFinite(delayMilliseconds) || delayMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(delayMilliseconds),
                delayMilliseconds,
                "The audio delay must be finite and non-negative.");
        }
        double durationMilliseconds = beatmap.GetSliderDuration(slider);
        if (!double.IsFinite(durationMilliseconds) || durationMilliseconds <= 0)
        {
            throw new InvalidOperationException(
                "The slider body duration must be finite and greater than zero.");
        }
        List<HitSoundLayer> layers = beatmap.GetSliderBodyHitSoundLayers(slider);
        List<SampleResolutionResult> results = new List<SampleResolutionResult>();
        foreach (var layer in layers)
        {
            SampleResolutionResult result = resolver.ResolveSample(layer, beatmapDirectory);
            if (result.Outcome == SampleResolutionOutcome.CustomSampleMissing || result.Outcome == SampleResolutionOutcome.ExternalFallbackRequired)
            {
                results.Add(result);
                continue;
            }
            if (result.Outcome == SampleResolutionOutcome.BeatmapSampleFound || result.Outcome == SampleResolutionOutcome.CustomSampleFound)
            {
                if (result.ResolvedPath == null)
                {
                    throw new InvalidOperationException(
                        "A successful sample resolution must include a playable file path.");
                }

                engine.AddLoopingAudioSource(result.ResolvedPath, durationMilliseconds, layer.Volume, delayMilliseconds);
                results.Add(result);
                continue;
            }
            throw new InvalidOperationException(
                $"The sample resolution outcome '{result.Outcome}' is not supported for playback.");
        }
        return results;
    }
    public List<SampleResolutionResult> PlaySliderTick(Beatmap beatmap, HitObject slider, string beatmapDirectory, SampleResolver resolver, AudioPlaybackEngine engine, double delayMilliseconds = 0)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(slider);
        if (slider.IsSlider == false)
        {
            throw new ArgumentException(
                "The hit object must be a slider.",
                nameof(slider));
        }
        if (!double.IsFinite(delayMilliseconds) || delayMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(delayMilliseconds),
                delayMilliseconds,
                "The audio delay must be finite and non-negative.");
        }
        List<HitSoundLayer> layers = beatmap.GetSliderTickHitSoundLayers(slider);
        List<SampleResolutionResult> results = PlayHitSoundLayers(layers, beatmapDirectory, resolver, engine, delayMilliseconds);
        return results;
    }
    public List<(double TickTime, List<SampleResolutionResult> Results)> PlaySliderTicksInTimeRange(Beatmap beatmap, HitObject slider, double startExclusiveMilliseconds, double endInclusiveMilliseconds, string beatmapDirectory, SampleResolver resolver, AudioPlaybackEngine engine)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(slider);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(engine);
        if (string.IsNullOrWhiteSpace(beatmapDirectory))
        {
            throw new ArgumentException(
                "The beatmap directory must not be empty.",
                nameof(beatmapDirectory));
        }

        List<double> tickTimes = beatmap.GetSliderTickTimesInRange(slider, startExclusiveMilliseconds, endInclusiveMilliseconds);
        List<(double TickTime, List<SampleResolutionResult> Results)> results = new List<(double TickTime, List<SampleResolutionResult> Results)>();
        foreach (var tickTime in tickTimes)
        {
            List<SampleResolutionResult> tickResults = PlaySliderTick(beatmap, slider, beatmapDirectory, resolver, engine);
            results.Add((tickTime, tickResults));
        }
        return results;

    }
    public List<SampleResolutionResult> PlayTimedHitSoundEvent(TimedHitSoundEvent soundEvent, Beatmap beatmap, string beatmapDirectory, SampleResolver resolver, AudioPlaybackEngine engine, double delayMilliseconds = 0)
    {
        ArgumentNullException.ThrowIfNull(soundEvent);
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(engine);
        if (string.IsNullOrWhiteSpace(beatmapDirectory))
        {
            throw new ArgumentException(
                "The beatmap directory must not be empty.",
                nameof(beatmapDirectory));
        }
        if (!double.IsFinite(delayMilliseconds) || delayMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(delayMilliseconds),
                delayMilliseconds,
                "The audio delay must be finite and non-negative.");
        }
        switch (soundEvent.EventType)
        {
            case HitSoundEventType.HitCircle:
                return PlayHitCircle(beatmap, soundEvent.HitObject, beatmapDirectory, resolver, engine, delayMilliseconds);
            case HitSoundEventType.SliderEdge:
                if (soundEvent.EdgeIndex == null)
                {
                    throw new ArgumentException(
                        "A slider edge event requires an edge index.",
                        nameof(soundEvent));
                }
                return PlaySliderEdge(beatmap, soundEvent.HitObject, soundEvent.EdgeIndex.Value, beatmapDirectory, resolver, engine, delayMilliseconds);
            case HitSoundEventType.SliderTick:
                return PlaySliderTick(beatmap, soundEvent.HitObject, beatmapDirectory, resolver, engine, delayMilliseconds);
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(soundEvent.EventType),
                    soundEvent.EventType,
                    "The hitsound event type is not supported for playback.");
        }
    }
    public List<(HitObject HitObject, List<SampleResolutionResult> Results)> PlayHitCirclesInTimeRange(Beatmap beatmap, double startExclusiveMilliseconds, double endInclusiveMilliseconds, string beatmapDirectory, SampleResolver resolver, AudioPlaybackEngine engine)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(engine);
        if (string.IsNullOrWhiteSpace(beatmapDirectory))
        {
            throw new ArgumentException(
                "The beatmap directory must not be empty.",
                nameof(beatmapDirectory));
        }
        List<HitObject> hitCircles = beatmap.GetHitCirclesInTimeRange(startExclusiveMilliseconds, endInclusiveMilliseconds);
        List<(HitObject HitObject, List<SampleResolutionResult> Results)> results = new List<(HitObject HitObject, List<SampleResolutionResult> Results)>();
        foreach (var hitCircle in hitCircles)
        {
            List<SampleResolutionResult> layerResults = PlayHitCircle(beatmap, hitCircle, beatmapDirectory, resolver, engine);
            results.Add((hitCircle, layerResults)); // odio trabajar con tuplas
        }
        return results;
    }
    public List<(HitObject HitObject, List<SampleResolutionResult> Results)> PlayPendingHitCircles(Beatmap beatmap, HitCirclePlaybackCursor cursor, double currentPositionMilliseconds, string beatmapDirectory, SampleResolver resolver, AudioPlaybackEngine engine)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(cursor);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(engine);
        if (string.IsNullOrWhiteSpace(beatmapDirectory))
        {
            throw new ArgumentException(
                "The beatmap directory must not be empty.",
                nameof(beatmapDirectory));
        }
        List<HitObject> pendingCircles = cursor.AdvanceTo(beatmap, currentPositionMilliseconds);
        List<(HitObject HitObject, List<SampleResolutionResult> Results)> results = new List<(HitObject HitObject, List<SampleResolutionResult> Results)>();
        foreach (var hitCircle in pendingCircles)
        {
            List<SampleResolutionResult> layerResults = PlayHitCircle(beatmap, hitCircle, beatmapDirectory, resolver, engine);
            results.Add((hitCircle, layerResults));
        }
        return results;
    }
    public List<(TimedHitSoundEvent Event, List<SampleResolutionResult> Results)> PlayPendingTimedHitSoundEvents(Beatmap beatmap, TimedHitSoundPlaybackCursor cursor, double currentPositionMilliseconds, string beatmapDirectory, SampleResolver resolver, AudioPlaybackEngine engine)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(cursor);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(engine);
        if (string.IsNullOrWhiteSpace(beatmapDirectory))
        {
            throw new ArgumentException(
                "The beatmap directory must not be empty.",
                nameof(beatmapDirectory));
        }
        List<TimedHitSoundEvent> pendingEvents = cursor.AdvanceTo(beatmap, currentPositionMilliseconds);
        List<(TimedHitSoundEvent Event, List<SampleResolutionResult> Results)> results = new List<(TimedHitSoundEvent Event, List<SampleResolutionResult> Results)>();
        foreach (var soundEvent in pendingEvents)
        {
            List<SampleResolutionResult> eventResults = PlayTimedHitSoundEvent(soundEvent, beatmap, beatmapDirectory, resolver, engine);
            results.Add((soundEvent, eventResults));
        }
        return results;
    }
    public List<(TimedHitSoundEvent Event, List<SampleResolutionResult> Results)> QueueTimedHitSoundEventsInRange(Beatmap beatmap, double startMilliseconds, double endMilliseconds, string beatmapDirectory, SampleResolver resolver, AudioPlaybackEngine engine, bool includeStart = false)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(engine);
        if (string.IsNullOrWhiteSpace(beatmapDirectory))
        {
            throw new ArgumentException(
                "The beatmap directory must not be empty.",
                nameof(beatmapDirectory));
        }
        if (!double.IsFinite(startMilliseconds) || startMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startMilliseconds),
                startMilliseconds,
                "The interval start must be finite and non-negative.");
        }
        if (!double.IsFinite(endMilliseconds) || endMilliseconds < startMilliseconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endMilliseconds),
                endMilliseconds,
                "The interval end must be finite and greater than or equal to the start.");
        }
        double startExclusiveMilliseconds;
        if (startMilliseconds == 0)
        {
            startExclusiveMilliseconds = -1;
        }
        else if (includeStart == true)
        {
            startExclusiveMilliseconds = Math.BitDecrement(startMilliseconds);
        }
        else
        {
            startExclusiveMilliseconds = startMilliseconds;
        }
        List<TimedHitSoundEvent> events = beatmap.GetTimedHitSoundEventsInRange(startExclusiveMilliseconds, endMilliseconds);
        List<(TimedHitSoundEvent Event, List<SampleResolutionResult> Results)> results = new List<(TimedHitSoundEvent Event, List<SampleResolutionResult> Result)>();
        foreach (var soundEvent in events)
        {
            double delayMilliseconds = soundEvent.Time - startMilliseconds;
            List<SampleResolutionResult> eventResults = PlayTimedHitSoundEvent(soundEvent, beatmap, beatmapDirectory, resolver, engine, delayMilliseconds);
            if (soundEvent.EventType == HitSoundEventType.SliderEdge && soundEvent.EdgeIndex == 0)
            {
                List<SampleResolutionResult> bodyResults = PlaySliderBody(beatmap, soundEvent.HitObject, beatmapDirectory, resolver, engine, delayMilliseconds);
                eventResults.AddRange(bodyResults);
            }
            results.Add((soundEvent, eventResults));
        }
        return results;
    }
}
