namespace OsuHitsoundEditor;

public class HitSoundLayer
{
    public HitSoundType Type {get; set;}
    public SampleSetType SampleSet {get; set;}
    public int SampleIndex {get; set;}
    public int Volume {get; set;}
    public string Filename {get; set;} = string.Empty;
}
