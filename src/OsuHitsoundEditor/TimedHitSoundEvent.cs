using System;

namespace OsuHitsoundEditor;

public class TimedHitSoundEvent
{
    public required double Time {get; init;}
    public required HitObject HitObject {get; init;}
    public required HitSoundEventType EventType {get; init;}
    public int? EdgeIndex {get; init;} = null;
}
