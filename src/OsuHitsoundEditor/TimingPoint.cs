namespace OsuHitsoundEditor;

public class TimingPoint
{
    public double Time { get; set; }
    public double BeatLength { get; set; }
    public bool IsUninherited { get; set; }
    public double? Bpm
    {
        get
        {
            if (IsUninherited)
            {
                return 60000 / BeatLength;
            }
            else
            {
                return null;
            }
        }
    }
    public int SampleSet { get; set; }
    public int SampleIndex { get; set; }
    public int Volume { get; set; }
}
