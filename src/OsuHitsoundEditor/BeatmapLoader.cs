namespace OsuHitsoundEditor;

public class BeatmapLoader
{
    public Beatmap Load(string path)
    {
        string currentSection = string.Empty;
        string[] lines = File.ReadAllLines(path);

        Beatmap beatmap = new Beatmap();

        foreach (string line in lines)
        {
            if (line.StartsWith("osu file format v"))
            {
                string[] versionParts = line.Split('v', 2);
                beatmap.BeatmapVersion = int.Parse(versionParts[1]);
                continue;
            }
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentSection = line.Trim('[', ']');
                continue;

            }
            switch (currentSection)
            {
                case "General":
                    ParseGeneralLine(beatmap, line);
                    break;
                case "Editor":
                    ParseEditorLine(beatmap, line);
                    break;
                case "Metadata":
                    ParseMetadataLine(beatmap, line);
                    break;
                case "Difficulty":
                    ParseDifficultyLine(beatmap, line);
                    break;
                case "TimingPoints":
                    ParseTimingPointLine(beatmap, line);
                    break;
                case "HitObjects":
                    ParseHitObjectLine(beatmap, line);
                    break;
            }
        }
        return beatmap;
    }
    private void ParseGeneralLine(Beatmap beatmap, string line)
    {
        if (line.StartsWith("AudioFilename:"))
        {
            string[] parts = line.Split(":", 2);
            beatmap.AudioFilename = parts[1];
        }
        if (line.StartsWith("Mode:"))
        {
            string[] parts = line.Split(":", 2);
            beatmap.Mode = int.Parse(parts[1]);
        }
        if (line.StartsWith("SampleSet:"))
        {
            string[] parts = line.Split(':', 2);
            string sampleSet = parts[1].Trim();
            switch (sampleSet)
            {
                case "Normal":
                    beatmap.DefaultSampleSet = SampleSetType.Normal;
                    break;
                case "Soft":
                    beatmap.DefaultSampleSet = SampleSetType.Soft;
                    break;
                case "Drum":
                    beatmap.DefaultSampleSet = SampleSetType.Drum;
                    break;
            }
        }
        if (line.StartsWith("AudioLeadIn:"))
        {
            string[] parts = line.Split(':', 2);
            beatmap.AudioLeadIn = int.Parse(parts[1]);
        }
        if (line.StartsWith("PreviewTime:"))
        {
            string[] parts = line.Split(':', 2);
            beatmap.PreviewTime = int.Parse(parts[1]);
        }
        if (line.StartsWith("Countdown:"))
        {
            string[] parts = line.Split(':', 2);
            beatmap.Countdown = int.Parse(parts[1]);
        }
    }
    private void ParseEditorLine(Beatmap beatmap, string line)
    {
        if (line.StartsWith("BeatDivisor:"))
        {
            string[] parts = line.Split(':', 2);
            beatmap.BeatDivisor = int.Parse(parts[1]);
        }
        if (line.StartsWith("DistanceSpacing:"))
        {
            string[] parts = line.Split(':', 2);
            beatmap.DistanceSpacing = double.Parse(parts[1]);
        }
        if (line.StartsWith("TimelineZoom:"))
        {
            string[] parts = line.Split(':', 2);
            beatmap.TimelineZoom = double.Parse(parts[1]);
        }
        if (line.StartsWith("Bookmarks:"))
        {
            string[] parts = line.Split(':', 2);
            string[] bookmarks = parts[1].Split(',');
            for (int i = 0; bookmarks.Length > i; i++)
            {
                beatmap.Bookmarks.Add(int.Parse(bookmarks[i]));
            }
        }
        if (line.StartsWith("GridSize:"))
        {
            string[] parts = line.Split(':', 2);
            beatmap.GridSize = int.Parse(parts[1]);
        }
    }
    private void ParseMetadataLine(Beatmap beatmap, string line)
    {
        if (line.StartsWith("Title:"))
        {
            string[] parts = line.Split(":", 2);
            beatmap.Title = parts[1];
        }
        if (line.StartsWith("Artist:"))
        {
            string[] parts = line.Split(":", 2);
            beatmap.Artist = parts[1];

        }
        if (line.StartsWith("Creator:"))
        {
            string[] parts = line.Split(":", 2);
            beatmap.Creator = parts[1];

        }
        if (line.StartsWith("Version:"))
        {
            string[] parts = line.Split(":", 2);
            beatmap.Difficulty = parts[1];
        }
    }
    private void ParseDifficultyLine(Beatmap beatmap, string line)
    {
        if (line.StartsWith("SliderMultiplier:"))
        {
            string[] parts = line.Split(':', 2);
            beatmap.SliderMultiplier = double.Parse(parts[1]);
        }
        if (line.StartsWith("SliderTickRate:"))
        {
            string[] parts = line.Split(':', 2);
            beatmap.SliderTickRate = double.Parse(parts[1]);
        }
        if (line.StartsWith("HPDrainRate:"))
        {
            string[] parts = line.Split(':', 2);
            beatmap.HPDrainRate = double.Parse(parts[1]);
        }
        if (line.StartsWith("CircleSize:"))
        {
            string[] parts = line.Split(':', 2);
            beatmap.CircleSize = double.Parse(parts[1]);
        }
        if (line.StartsWith("OverallDifficulty:"))
        {
            string[] parts = line.Split(':', 2);
            beatmap.OverallDifficulty = double.Parse(parts[1]);
        }
        if (line.StartsWith("ApproachRate:"))
        {
            string[] parts = line.Split(':', 2);
            beatmap.ApproachRate = double.Parse(parts[1]);
        }
    }
    private void ParseTimingPointLine(Beatmap beatmap, string line)
    {
        if (line.Any(char.IsDigit))
        {
            TimingPoint timingPoint = new TimingPoint();
            string[] parts = line.Split(',');
            timingPoint.Time = double.Parse(parts[0]);
            timingPoint.BeatLength = double.Parse(parts[1]);
            if (parts.Length > 6)
            {
                timingPoint.IsUninherited = parts[6] == "1";
            }
            else
            {
                timingPoint.IsUninherited = timingPoint.BeatLength > 0;
            }
            timingPoint.SampleSet = int.Parse(parts[3]);
            timingPoint.SampleIndex = int.Parse(parts[4]);
            timingPoint.Volume = int.Parse(parts[5]);
            beatmap.TimingPoints.Add(timingPoint);
        }
    }

    private void ParseHitObjectLine(Beatmap beatmap, string line)
    {
        if (line.Any(char.IsDigit))
        {
            HitObject hitObject = new HitObject();
            string[] parts = line.Split(',');
            hitObject.X = int.Parse(parts[0]);
            hitObject.Y = int.Parse(parts[1]);
            hitObject.Time = int.Parse(parts[2]);
            hitObject.ObjectType = int.Parse(parts[3]);
            hitObject.HitSound = int.Parse(parts[4]);
            beatmap.HitObjects.Add(hitObject);
            if (hitObject.IsHitCircle && parts.Length > 5)
            {

                hitObject.HitSample = ParseHitSample(parts[5]);

            }
            if (hitObject.IsSpinner)
            {
                if (parts.Length > 5)
                {
                    hitObject.EndTime = int.Parse(parts[5]);
                }
                if (parts.Length > 6)
                {
                    hitObject.HitSample = ParseHitSample(parts[6]);

                }
            }
            if (hitObject.IsSlider && parts.Length > 10)
            {
                hitObject.Slides = int.Parse(parts[6]);
                hitObject.Length = double.Parse(parts[7]);
                string[] edgeSounds = parts[8].Split('|');
                string[] edgeSets = parts[9].Split('|');
                for (int i = 0; i < edgeSounds.Length; i++)
                {
                    SliderEdge sliderEdge = new SliderEdge();
                    sliderEdge.HitSound = int.Parse(edgeSounds[i]);
                    string[] edgeSet = edgeSets[i].Split(':');
                    sliderEdge.NormalSet = int.Parse(edgeSet[0]);
                    sliderEdge.AdditionSet = int.Parse(edgeSet[1]);
                    hitObject.SliderEdges.Add(sliderEdge);
                }

                hitObject.HitSample = ParseHitSample(parts[10]);
            }
        }
    }
    private HitSample ParseHitSample(string sampleText)
    {
        string[] sampleParts = sampleText.Split(':');
        HitSample hitSample = new HitSample();
        if (sampleParts.Length > 4)
        {
            hitSample.NormalSet = int.Parse(sampleParts[0]);
            hitSample.AdditionSet = int.Parse(sampleParts[1]);
            hitSample.Index = int.Parse(sampleParts[2]);
            hitSample.Volume = int.Parse(sampleParts[3]);
            hitSample.Filename = sampleParts[4];
        }
        return hitSample;
    }
}
