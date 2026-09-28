using OsuHitsoundEditor;

namespace OsuHitsoundEditor.Tests;

public class BeatmapLoaderTests
{
    private static string GetTestBeatmapPath()
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "basic-beatmap.osu"
        );
    }

    [Fact]
    public void Load_ReadsBasicBeatmapInformation()
    {
        BeatmapLoader loader = new BeatmapLoader();

        Beatmap beatmap = loader.Load(GetTestBeatmapPath());

        Assert.Equal("Test Song", beatmap.Title);
        Assert.Equal("Test Artist", beatmap.Artist);
        Assert.Equal("Test Creator", beatmap.Creator);
        Assert.Equal("Test Difficulty", beatmap.Difficulty);
        Assert.Equal("test.mp3", beatmap.AudioFilename);
        Assert.Equal(0, beatmap.Mode);
    }

    [Fact]
    public void Load_ReadsTimingPoints()
    {
        BeatmapLoader loader = new BeatmapLoader();

        Beatmap beatmap = loader.Load(GetTestBeatmapPath());

        Assert.Equal(2, beatmap.TimingPoints.Count);

        TimingPoint first = beatmap.TimingPoints[0];

        Assert.Equal(1000, first.Time);
        Assert.Equal(500, first.BeatLength);
        Assert.True(first.IsUninherited);
        Assert.Equal(2, first.SampleSet);
        Assert.Equal(3, first.SampleIndex);
        Assert.Equal(70, first.Volume);

        TimingPoint second = beatmap.TimingPoints[1];

        Assert.False(second.IsUninherited);
        Assert.Equal(3, second.SampleSet);
        Assert.Equal(4, second.SampleIndex);
        Assert.Equal(60, second.Volume);
    }

    [Fact]
    public void Load_ReadsHitObjectTypes()
    {
        BeatmapLoader loader = new BeatmapLoader();

        Beatmap beatmap = loader.Load(GetTestBeatmapPath());

        Assert.Equal(3, beatmap.HitObjects.Count);

        Assert.True(beatmap.HitObjects[0].IsHitCircle);
        Assert.True(beatmap.HitObjects[1].IsSlider);
        Assert.True(beatmap.HitObjects[2].IsSpinner);
    }

    [Fact]
    public void Load_ReadsCircleHitSample()
    {
        BeatmapLoader loader = new BeatmapLoader();

        Beatmap beatmap = loader.Load(GetTestBeatmapPath());

        HitSample sample = beatmap.HitObjects[0].HitSample;

        Assert.Equal(1, sample.NormalSet);
        Assert.Equal(2, sample.AdditionSet);
        Assert.Equal(5, sample.Index);
        Assert.Equal(80, sample.Volume);
        Assert.Equal("circle.wav", sample.Filename);
    }

    [Fact]
    public void Load_ReadsSliderHitSample()
    {
        BeatmapLoader loader = new BeatmapLoader();

        Beatmap beatmap = loader.Load(GetTestBeatmapPath());

        HitSample sample = beatmap.HitObjects[1].HitSample;

        Assert.Equal(3, sample.NormalSet);
        Assert.Equal(1, sample.AdditionSet);
        Assert.Equal(6, sample.Index);
        Assert.Equal(90, sample.Volume);
        Assert.Equal("slider.wav", sample.Filename);
    }

    [Fact]
    public void Load_ReadsSpinnerEndTimeAndHitSample()
    {
        BeatmapLoader loader = new BeatmapLoader();

        Beatmap beatmap = loader.Load(GetTestBeatmapPath());

        HitObject spinner = beatmap.HitObjects[2];

        Assert.Equal(3500, spinner.EndTime);
        Assert.Equal(2, spinner.HitSample.NormalSet);
        Assert.Equal(3, spinner.HitSample.AdditionSet);
        Assert.Equal(7, spinner.HitSample.Index);
        Assert.Equal(75, spinner.HitSample.Volume);
        Assert.Equal("spinner.wav", spinner.HitSample.Filename);
    }
    [Fact]
    public void Load_Slider_ParsesSliderEdges()
    {
        // Arrange
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "basic-beatmap.osu"
        );

        BeatmapLoader loader = new BeatmapLoader();

        // Act
        Beatmap beatmap = loader.Load(path);

        HitObject slider = beatmap.HitObjects.First(
            hitObject => hitObject.IsSlider
        );

        // Assert
        Assert.Equal(3, slider.SliderEdges.Count);

        Assert.Equal(4, slider.SliderEdges[0].HitSound);
        Assert.Equal(1, slider.SliderEdges[0].NormalSet);
        Assert.Equal(2, slider.SliderEdges[0].AdditionSet);

        Assert.Equal(8, slider.SliderEdges[1].HitSound);
        Assert.Equal(2, slider.SliderEdges[1].NormalSet);
        Assert.Equal(3, slider.SliderEdges[1].AdditionSet);

        Assert.Equal(2, slider.SliderEdges[2].HitSound);
        Assert.Equal(3, slider.SliderEdges[2].NormalSet);
        Assert.Equal(1, slider.SliderEdges[2].AdditionSet);
    }
    [Fact]
    public void Load_ReadsSliderProperties()
    {
        BeatmapLoader loader = new BeatmapLoader();

        Beatmap beatmap = loader.Load(GetTestBeatmapPath());

        HitObject slider = beatmap.HitObjects.First(
            hitObject => hitObject.IsSlider
        );

        Assert.Equal(2, slider.Slides);
        Assert.Equal(120, slider.Length);
    }
}