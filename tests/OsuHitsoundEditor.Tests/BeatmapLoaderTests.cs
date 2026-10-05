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
    [Fact]
    public void Load_ReadsDefaultSampleSet()
    {
        // Arrange
        BeatmapLoader loader = new BeatmapLoader();

        // Act
        Beatmap beatmap = loader.Load(GetTestBeatmapPath());

        // Assert
        Assert.Equal(SampleSetType.Soft, beatmap.DefaultSampleSet);
    }
    [Fact]
    public void Load_ReadsGeneralSettings()
    {
        BeatmapLoader loader = new BeatmapLoader();

        Beatmap beatmap = loader.Load(GetTestBeatmapPath());

        Assert.Equal(500, beatmap.AudioLeadIn);
        Assert.Equal(15000, beatmap.PreviewTime);
        Assert.Equal(1, beatmap.Countdown);
        Assert.Equal(SampleSetType.Soft, beatmap.DefaultSampleSet);
    }

    [Fact]
    public void Load_ReadsEditorSettings()
    {
        BeatmapLoader loader = new BeatmapLoader();

        Beatmap beatmap = loader.Load(GetTestBeatmapPath());

        Assert.Equal(4, beatmap.BeatDivisor);
        Assert.Equal(1.5, beatmap.DistanceSpacing);
        Assert.Equal(8, beatmap.GridSize);
        Assert.Equal(2, beatmap.TimelineZoom);

        Assert.Equal(3, beatmap.Bookmarks.Count);
        Assert.Equal(1200, beatmap.Bookmarks[0]);
        Assert.Equal(2400, beatmap.Bookmarks[1]);
        Assert.Equal(3600, beatmap.Bookmarks[2]);
    }

    [Fact]
    public void Load_ReadsDifficultySettings()
    {
        BeatmapLoader loader = new BeatmapLoader();

        Beatmap beatmap = loader.Load(GetTestBeatmapPath());

        Assert.Equal(5, beatmap.HPDrainRate);
        Assert.Equal(4, beatmap.CircleSize);
        Assert.Equal(7, beatmap.OverallDifficulty);
        Assert.Equal(8, beatmap.ApproachRate);
        Assert.Equal(1.4, beatmap.SliderMultiplier);
        Assert.Equal(1, beatmap.SliderTickRate);
    }
    [Fact]
    public void Load_BeatmapVersion_ParsesFileFormatVersion()
    {
        // Arrange
        BeatmapLoader loader = new BeatmapLoader();

        // Act
        Beatmap beatmap = loader.Load("TestData/basic-beatmap.osu");

        // Assert
        Assert.Equal(14, beatmap.BeatmapVersion);
    }
    [Fact]
    public void Load_ExplicitFilenameCompatibilityFixture_ParsesExpectedCases()
    {
        // Arrange
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "ExplicitFilenameCompatibility",
            "explicit-filename-compatibility.osu"
        );

        BeatmapLoader loader = new BeatmapLoader();

        // Act
        Beatmap beatmap = loader.Load(path);

        // Assert
        Assert.Equal(7, beatmap.HitObjects.Count);

        Assert.Equal(0, beatmap.HitObjects[0].HitSound);
        Assert.Equal(string.Empty, beatmap.HitObjects[0].HitSample.Filename);

        Assert.Equal(0, beatmap.HitObjects[1].HitSound);
        Assert.Equal("custom.wav", beatmap.HitObjects[1].HitSample.Filename);

        Assert.Equal(2, beatmap.HitObjects[2].HitSound);
        Assert.Equal("custom.wav", beatmap.HitObjects[2].HitSample.Filename);

        Assert.Equal(4, beatmap.HitObjects[3].HitSound);
        Assert.Equal("custom.wav", beatmap.HitObjects[3].HitSample.Filename);

        Assert.Equal(8, beatmap.HitObjects[4].HitSound);
        Assert.Equal("custom.wav", beatmap.HitObjects[4].HitSample.Filename);

        Assert.Equal(14, beatmap.HitObjects[5].HitSound);
        Assert.Equal("custom.wav", beatmap.HitObjects[5].HitSample.Filename);

        Assert.Equal(8, beatmap.HitObjects[6].HitSound);
        Assert.Equal(string.Empty, beatmap.HitObjects[6].HitSample.Filename);
    }
    [Fact]
    public void Load_ExplicitFilenameRemainingComponentsFixture_PreservesRawValues()
    {
        // Arrange
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "ExplicitFilenameRemainingComponents",
            "explicit-filename-remaining-components.osu"
        );

        BeatmapLoader loader = new BeatmapLoader();

        // Act
        Beatmap beatmap = loader.Load(path);

        // Assert
        Assert.Equal(2, beatmap.HitObjects.Count);

        HitObject spinner = beatmap.HitObjects[0];

        Assert.True(spinner.IsSpinner);
        Assert.Equal(14, spinner.HitSound);
        Assert.Equal(1, spinner.HitSample.NormalSet);
        Assert.Equal(1, spinner.HitSample.AdditionSet);
        Assert.Equal(1, spinner.HitSample.Index);
        Assert.Equal(100, spinner.HitSample.Volume);
        Assert.Equal("custom.wav", spinner.HitSample.Filename);

        HitObject slider = beatmap.HitObjects[1];

        Assert.True(slider.IsSlider);
        Assert.Equal(2, slider.HitSound);

        Assert.Equal(1, slider.HitSample.NormalSet);
        Assert.Equal(1, slider.HitSample.AdditionSet);
        Assert.Equal(1, slider.HitSample.Index);
        Assert.Equal(100, slider.HitSample.Volume);
        Assert.Equal("custom.wav", slider.HitSample.Filename);

        Assert.Equal(3, slider.SliderEdges.Count);
        Assert.Equal(8, slider.SliderEdges[0].HitSound);
        Assert.Equal(4, slider.SliderEdges[1].HitSound);
        Assert.Equal(2, slider.SliderEdges[2].HitSound);
    }
}