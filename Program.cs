using System;
using System.Drawing;

namespace osuproyecto;

public class Program
{
    static void Main()
    {
        BeatmapLoader loader = new BeatmapLoader();
        Beatmap beatmap = loader.Load("C://Users//Jose//AppData//Local//osu!//Songs//2440864 DECO_27 - Monitoring (Best Friend Remix) feat Hatsune Miku//DECO27 - Monitoring (Best Friend Remix) feat. Hatsune Miku (FAJIFBEFJHA) [Best Frenzy].osu");
        Console.WriteLine(beatmap.Title);
        Console.WriteLine(beatmap.Artist);
        Console.WriteLine(beatmap.Creator);
        Console.WriteLine(beatmap.Difficulty);
        Console.WriteLine(beatmap.Mode);
        Console.WriteLine(beatmap.AudioFilename);
        Console.WriteLine("Timing points Encontrados: " + beatmap.TimingPoints.Count);
        Console.WriteLine("HitObjects Encontrados: " + beatmap.HitObjects.Count);
        int counterTiming = 0;
        int counterObjects = 0;
        {
            foreach (var timingPoint in beatmap.TimingPoints)
            {
                Console.WriteLine("Time: " + timingPoint.Time);
                Console.WriteLine("Beatmaplength: " + timingPoint.BeatLength);
                Console.WriteLine("IsUninherited: " + timingPoint.IsUninherited);
                Console.WriteLine("BPM: " + timingPoint.Bpm);
                Console.WriteLine("Sample Set: " + timingPoint.SampleSet);
                Console.WriteLine("Sample index: " + timingPoint.SampleIndex);
                Console.WriteLine("Volume: " + timingPoint.Volume);
                counterTiming++;
                if (counterTiming >= 5)
                {
                    break;
                }

            }
            foreach (var hitObject in beatmap.HitObjects)
            {
                Console.WriteLine("X: " + hitObject.X);
                Console.WriteLine("Y: " + hitObject.Y);
                Console.WriteLine("Time: " + hitObject.Time);
                TimingPoint? activeTimingPoint = beatmap.GetActiveTimingPoint(hitObject.Time);
                if (activeTimingPoint != null)
                {
                    Console.WriteLine("Active Timing Point Time: " + activeTimingPoint.Time);
                    Console.WriteLine("Active Sample Set: " + activeTimingPoint.SampleSet);
                    Console.WriteLine("Active Sample Index: " + activeTimingPoint.SampleIndex);
                    Console.WriteLine("Active Volume: " + activeTimingPoint.Volume);
                }
                Console.WriteLine("ObjectType: " + hitObject.ObjectType);
                Console.WriteLine("Circle: " + hitObject.IsHitCircle);
                Console.WriteLine("Slider: " + hitObject.IsSlider);
                Console.WriteLine("Spiner: " + hitObject.IsSpinner);
                Console.WriteLine("New Combo: " + hitObject.StarstNewCombo);
                Console.WriteLine("HitSound: " + hitObject.HitSound);
                Console.WriteLine("NormalSet: " + hitObject.HitSample.NormalSet);
                Console.WriteLine("AdditionSet: " + hitObject.HitSample.AdditionSet);
                Console.WriteLine("Index: " + hitObject.HitSample.Index);
                Console.WriteLine("Volume: " + hitObject.HitSample.Volume);
                Console.WriteLine("Filename: " + hitObject.HitSample.Filename);
                Console.WriteLine("Normal Only: " + hitObject.IsNormalOnly);
                Console.WriteLine("Whistle: " + hitObject.HasWhistle);
                Console.WriteLine("Finish: " + hitObject.HasFinish);
                Console.WriteLine("Clap: " + hitObject.HasClap);
                Console.WriteLine("Original Index: " + hitObject.HitSample.Index);
                Console.WriteLine("Effective Index: " + beatmap.GetEffectiveSampleIndex(hitObject));
                Console.WriteLine("Original Volume: " + hitObject.HitSample.Volume);
                Console.WriteLine("Effective Volume: " + beatmap.GetEffectiveVolume(hitObject));
                Console.WriteLine("Original Normal Set: " + hitObject.HitSample.NormalSet);
                Console.WriteLine("Effective Normal Set: "+ beatmap.GetEffectiveNormalSet(hitObject));
                Console.WriteLine("Original Addition Set: " + hitObject.HitSample.AdditionSet);
                Console.WriteLine("Effective Addition Set: "+ beatmap.GetEffectiveAdditionSet(hitObject));
                counterObjects++;
                if (counterObjects >= 5)
                {
                    break;
                }

            }

        }
        Console.ReadLine();
    }
}
