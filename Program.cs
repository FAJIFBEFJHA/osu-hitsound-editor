using System;

namespace osuproyecto;

public class Program
{
    static void Main()
    {
        BeatmapLoader loader = new BeatmapLoader();
        Beatmap beatmap = loader.Load("C://Users//Jose//AppData//Local//osu!//Songs//1215 Linda Yamamoto - Neraiuchi//Linda Yamamoto - Neraiuchi (Kai) [Easy].osu");
        Console.WriteLine(beatmap.Title);
        Console.WriteLine(beatmap.Artist);
        Console.WriteLine(beatmap.Creator);
        Console.WriteLine(beatmap.Difficulty);
        Console.WriteLine(beatmap.Mode);
        Console.WriteLine(beatmap.AudioFilename);
    }
}
