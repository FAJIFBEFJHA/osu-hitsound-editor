using System;
using System.Drawing;

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
        Console.WriteLine("Timing points Encontrados: " + beatmap.TimingPoints.Count);
        Console.WriteLine("HitObjects Encontrados: " + beatmap.HitObjects.Count);
        {
            foreach (var timingPoint in beatmap.TimingPoints)
            {
                Console.WriteLine("Time: " + timingPoint.Time);
                Console.WriteLine("Beatmaplength: " + timingPoint.BeatLength);
                Console.WriteLine("IsUninherited: " + timingPoint.IsUninherited);
                Console.WriteLine("BPM: " + timingPoint.Bpm);
            }
            foreach (var hitObject in beatmap.HitObjects)
            {
                Console.WriteLine("X: " + hitObject.X);
                Console.WriteLine("Y: " + hitObject.Y);
                Console.WriteLine("Time: " + hitObject.Time);
                Console.WriteLine("ObjectType: " + hitObject.ObjectType);
                Console.WriteLine("HitSound: " + hitObject.HitSound);
            }

        }
    }
}
