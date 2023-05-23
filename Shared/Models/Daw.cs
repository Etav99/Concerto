namespace Concerto.Shared.Models.Dto;

public record DawProject
{
    public List<Track> Tracks { get; set; } = new();
}

public record Track
{
    public string Name { get; set; }
    public string? Source { get; set; }
    public float StartTime { get; set; }
    public float Volume { get; set; }


    public string SelectedByName { get; set; } = string.Empty;
    public TrackSelectionState SelectionState { get; set; }
}

public enum TrackSelectionState
{
    None,
    Other,
    Self,
}
