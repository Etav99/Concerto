using System.Text.Json.Serialization;

namespace Concerto.Shared.Models.Dto;

public record DawProject
{
    public List<Track> Tracks { get; set; } = new();
}

public record Track
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string Name { get; set; }
    public Guid? SourceId { get; set; }
    public float StartTime { get; set; }
    public float Volume { get; set; }
    public string SelectedByName { get; set; } = string.Empty;
    public TrackSelectionState SelectionState { get; set; }

    [JsonIgnore]
    public bool IsMuted { get; set; } = false;
    [JsonIgnore]
    public bool IsSolo { get; set; } = false;

    public bool IsSelfSelected => SelectionState == TrackSelectionState.Self;
    public bool IsOtherSelected => SelectionState == TrackSelectionState.Other;
    public bool IsSelected => SelectionState != TrackSelectionState.None;
}

public enum TrackSelectionState
{
    None,
    Other,
    Self,
}
