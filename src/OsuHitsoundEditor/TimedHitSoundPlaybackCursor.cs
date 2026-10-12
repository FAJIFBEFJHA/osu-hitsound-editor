namespace OsuHitsoundEditor;

public class TimedHitSoundPlaybackCursor
{
    private double lastProcessedMilliseconds = -1;
    public List<TimedHitSoundEvent> AdvanceTo(Beatmap beatmap, double currentPositionMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        if (!double.IsFinite(currentPositionMilliseconds) || currentPositionMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentPositionMilliseconds),
                currentPositionMilliseconds,
                "The timeline position must be finite and non-negative.");
        }
        if(currentPositionMilliseconds < lastProcessedMilliseconds)
        {
            throw new InvalidOperationException(
                "The timeline position moved backwards. Reset the playback cursor after seeking.");
        }
        List<TimedHitSoundEvent> pendingEvents = beatmap.GetTimedHitSoundEventsInRange(lastProcessedMilliseconds, currentPositionMilliseconds);
        lastProcessedMilliseconds = currentPositionMilliseconds;
        return pendingEvents;
    }
    public void SeekTo(double positionMilliseconds)
    {
        if (!double.IsFinite(positionMilliseconds) || positionMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(positionMilliseconds),
                positionMilliseconds,
                "The timeline position must be finite and non-negative.");
        }
        if ( positionMilliseconds == 0)
        {
            lastProcessedMilliseconds = -1;
            return;
        }
        lastProcessedMilliseconds = positionMilliseconds;
    }
}
