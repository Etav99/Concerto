using System.Text.Json.Serialization;

namespace Concerto.Shared.Models.Dto;

public record DawProject
{
    public List<Track> Tracks { get; set; } = new();
    public Guid Token { get; set; }

    [JsonIgnore]
    private Dictionary<long, Track>? _tracksById = null;
    public Dictionary<long, Track> TracksById
    {
        get
        {
            if (_tracksById is null)
            {
                _tracksById = Tracks.ToDictionary(t => t.Id);
            }
            return _tracksById;
        }
    }

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

    public bool ShouldBeUpdated(Track newState)
    {
        return SourceId != newState.SourceId
               || StartTime != newState.StartTime
               || Volume != newState.Volume
               || SelectionState != newState.SelectionState;
    }

    public bool ShouldBeRestarted(Track newState)
    {
        return SourceId != newState.SourceId
               || StartTime != newState.StartTime;
    }

    public void CopyLocalState(Track oldState)
    {
        IsMuted = oldState.IsMuted;
        IsSolo = oldState.IsSolo;
    }
}

public enum TrackSelectionState
{
    None,
    Other,
    Self,
}
