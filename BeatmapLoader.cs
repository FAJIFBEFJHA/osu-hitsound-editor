using System.Runtime.CompilerServices;

namespace osuproyecto;

public class BeatmapLoader
{
    public Beatmap Load(string path)
    {
        string currentSection =  string.Empty;
        string[] lines = File.ReadAllLines(path);
        
        Beatmap beatmap = new Beatmap();

        foreach (string line in lines)
        {
             
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentSection = line.Trim('[',']');
                
            }    
            if (currentSection == "Metadata" && line.StartsWith("Title:"))
            {
                string[] parts = line.Split(":", 2);
                beatmap.Title = parts[1];
            }
            if (currentSection == "Metadata" && line.StartsWith("Artist:"))
            {
                string[] parts = line.Split(":", 2);
                beatmap.Artist = parts[1];
                
            }
            if (currentSection == "Metadata" && line.StartsWith("Creator:"))
            {
                string[] parts = line.Split(":", 2);
                beatmap.Creator = parts[1];
                
            }
            if (currentSection == "Metadata" && line.StartsWith("Version:"))
            {
                string[] parts = line.Split(":", 2);
                beatmap.Difficulty = parts[1];
                
            }
            if (currentSection == "General" && line.StartsWith("AudioFilename:"))
            {
                string[] parts = line.Split(":",2);
                beatmap.AudioFilename = parts[1];
            }
            if (currentSection == "General" && line.StartsWith("Mode:"))
            {
                string[] parts = line.Split(":",2);
                beatmap.Mode = int.Parse(parts[1]);
            }
            if (currentSection == "TimingPoints" && line.Any(char.IsDigit))
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
                beatmap.TimingPoints.Add(timingPoint);
            }
            if (currentSection == "HitObjects" && line.Any(char.IsDigit))
            {
                HitObject hitObject = new HitObject();
                string[] parts = line.Split(',');
                hitObject.X = int.Parse(parts[0]);
                hitObject.Y = int.Parse(parts[1]);
                hitObject.Time = int.Parse(parts[2]);
                hitObject.ObjectType = int.Parse(parts[3]);
                hitObject.HitSound = int.Parse(parts[4]);
                beatmap.HitObjects.Add(hitObject);
            }
        }
        return beatmap;
    }
    
}
