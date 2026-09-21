using System;

namespace osuproyecto;

public class Beatmap
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int Mode { get; set; }
    public string AudioFilename { get; set; } = string.Empty;
}
