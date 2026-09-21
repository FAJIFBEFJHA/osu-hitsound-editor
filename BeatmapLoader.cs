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
        }
        return beatmap;
    }
    
}
