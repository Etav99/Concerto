using Concerto.Client.Services;
using Concerto.Client.UI.Layout;
using Concerto.Shared.Constants;
using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

using System.Text.Json;
using System.Threading;
using static MudBlazor.CategoryTypes;

namespace Concerto.Client.UI.Components.Views;

public partial class Daw : IAsyncDisposable
{
    [Inject] IJSRuntime JS { get; set; } = null!;
    [Inject] DawService DawService { get; set; } = null!;
    [Inject] NavigationManager Navigation { get; set; } = null!;

    [CascadingParameter] LayoutState LayoutState { get; set; } = new();

    [Parameter] public EventCallback<long> OnListenTogether { get; set; }

    public const string dawId = "daw";

    bool DawInitialized { get; set; } = false;
    DawInterop DawInterop { get; } = new DawInterop();

    private HubConnection? _dawHub = null;
    private HubConnection DawHub
    {
        get => _dawHub ?? throw new NullReferenceException("DawHub is not initialized");
        set => _dawHub = value;
    }

    [Parameter]
    public long? SessionId { get; set; }
    private long _sessionId;

    private Task _updateProjectTask = Task.CompletedTask;
    private CancellationTokenSource _updateProjectCancellationTokenSource = new();

    private DawProject? _project;
    private DawProject Project
    {
        get => _project ?? throw new NullReferenceException("Project is not initialized");
        set => _project = value;
    }

    protected override async Task OnInitializedAsync()
    {
    }

    protected override async Task OnParametersSetAsync()
    {
        if (SessionId == _sessionId || SessionId is null) return;
        _sessionId = SessionId.Value;

        if (_dawHub != null) await _dawHub.DisposeAsync();
        DawHub = DawService.CreateHubConnection();
        await DawHub.StartAsync();
        DawHub.On<long>(DawHubMethods.Client.ProjectChanged, OnProjectChanged);
        await DawHub.InvokeAsync(DawHubMethods.Server.JoinDawProject, _sessionId);

        if (!DawInitialized)
            await DawInterop.Initialize(JS, dawId, this);
        else
            await DawInterop.ClearProject();

        Project = await DawService.GetProjectAsync(_sessionId);
        await DawInterop.LoadProject(Project);

        if (!DawInitialized)
            DawInitialized = true;
    }

    public async Task OnProjectChanged(long sessionId)
    {
        // if(sessionId != _sessionId) return;
        // TODO update logic
        await RequestUpdateProject();
    }

    public async Task RequestUpdateProject()
    {
        try
        {
            _updateProjectCancellationTokenSource.Cancel();
            await _updateProjectTask;
        }
        catch (OperationCanceledException) { }
        finally
        {
            _updateProjectCancellationTokenSource.Dispose();
            _updateProjectCancellationTokenSource = new();
        }

        _updateProjectTask = UpdateProject(_updateProjectCancellationTokenSource.Token);

        try
        {
            await _updateProjectTask;
        }
        catch (OperationCanceledException) { }
    }

    public async Task UpdateProject(CancellationToken cancellation)
    {
        var oldProjectState = Project;
        DawProject newProjectState;
        try
        {
            newProjectState = await DawService.GetProjectAsync(_sessionId, cancellation);
        }
        catch (OperationCanceledException) { throw; }

        foreach (var track in newProjectState.Tracks)
        {
            var oldTrack = oldProjectState.Tracks.FirstOrDefault(t => t.Id == track.Id);
            if (oldTrack != null) oldProjectState.Tracks.Remove(oldTrack);

            if (oldTrack is null)
            {
                await DawInterop.AddTrack(track);
            }
            else
            {
                track.IsSolo = oldTrack.IsSolo;
                track.IsMuted = oldTrack.IsMuted;
                await DawInterop.UpdateTrack(track, oldTrack.SourceId != track.SourceId);
            }
        }

        foreach (var track in oldProjectState.Tracks)
            await DawInterop.RemoveTrack(track);

        if (newProjectState.Tracks.Any())
            await DawInterop.ReorderTracks(newProjectState.Tracks.Select(t => t.Id));

        await DawInterop.ReRender();

        Project = newProjectState;
        StateHasChanged();
    }

    private async Task AddTrack()
    {
        await DawService.AddTrackAsync(_sessionId, Guid.NewGuid().ToString());
    }

    private async Task UploadTrackSource(Track track, IBrowserFile file)
    {
        await DawService.SetTrackSourceAsync(_sessionId, track.Id, file);
    }

    private async Task SelectTrack(Track track)
    {
        await DawService.SelectTrackAsync(_sessionId, track.Id);
    }

    private async Task DeleteTrack(Track track)
    {
        await DawService.DeleteTrackAsync(_sessionId, track.Id);
    }

    private async Task UnselectTrack(Track track)
    {
        await DawService.UnselectTrackAsync(_sessionId, track.Id);
    }

    private async Task SetTrackVolume(Track track, float volume)
    {
        await DawService.SetTrackVolumeAsync(_sessionId, track.Id, volume);
    }

    private async Task SetTrackStartTime(Track track, float startTime)
    {
        await DawService.SetTrackStartTimeAsync(_sessionId, track.Id, startTime);
    }

    public async Task SetTrackStartTime(long trackId, float startTime)
    {
        await DawService.SetTrackStartTimeAsync(_sessionId, trackId, startTime);
    }

    public async Task ListenTogether()
    {
        await DawService.GenerateProjectSourceAsync(_sessionId);
        await OnListenTogether.InvokeAsync(_sessionId);
    }

    public async ValueTask DisposeAsync()
    {
        await DawInterop.DisposeAsync();
        if (_dawHub != null) await _dawHub.DisposeAsync();
    }

}

public class DawInterop : IAsyncDisposable
{
    private IJSObjectReference? _playlist;
    private IJSObjectReference? _ee;
    private readonly DotNetObjectReference<DawInterop> _dawInteropJsRef;
    private Daw? _daw;

    private Task _setVolumeTask = Task.CompletedTask;
    private CancellationTokenSource _setVolumeCancellationTokenSource = new();

    public DawInterop()
    {
        _dawInteropJsRef = DotNetObjectReference.Create(this);
    }

    private IJSObjectReference Playlist
    {
        get
        {
            if (_playlist is null) throw new NullReferenceException("Daw is not initialized");
            return _playlist;
        }
    }

    private IJSObjectReference Ee
    {
        get
        {
            if (_ee is null) throw new NullReferenceException("Daw is not initialized");
            return _ee;
        }
    }


    public async Task Initialize(IJSRuntime js, string dawId, Daw? daw = null)
    {
        _playlist = await js.InvokeAsync<IJSObjectReference>("initializeDaw", dawId, new PlaylistOptionsJs(), _dawInteropJsRef);
        _ee = await _playlist.InvokeAsync<IJSObjectReference>("getEventEmitter");
        _daw = daw;
    }

    public async Task LoadProject(DawProject project)
    {
        await Playlist.InvokeVoidAsync("loadTrackList", project.Tracks.Select(TrackJs.Create));
    }

    public async Task ClearProject()
    {
        await Playlist.InvokeVoidAsync("clearTrackList");
    }

    public async Task AddTrack(Track track)
    {
        await Playlist.InvokeVoidAsync("addTrack", TrackJs.Create(track));
    }

    public async Task RemoveTrack(Track track)
    {
        await Playlist.InvokeVoidAsync("removeTrackById", track.Id);
    }

    public async Task ReorderTracks(IEnumerable<long> trackOrderIds)
    {
        await Playlist.InvokeVoidAsync("reorderTracks", trackOrderIds);
    }
    public async Task UpdateTrack(Track track, bool sourceChanged)
    {
        await Playlist.InvokeVoidAsync("updateTrack", TrackJs.Create(track), sourceChanged);
    }
    public async Task ReRender()
    {
        await Playlist.InvokeVoidAsync("reRender");
    }

    public async Task SetVolume(long trackId, float volume)
    {
        _setVolumeCancellationTokenSource.Cancel();
        await _setVolumeTask;
        _setVolumeCancellationTokenSource.Dispose();
        _setVolumeCancellationTokenSource = new();
        _setVolumeTask = SetVolumeTask(trackId, volume, _setVolumeCancellationTokenSource.Token);
        await _setVolumeTask;
    }

    public async Task SetVolumeTask(long trackId, float volume, CancellationToken cancellation)
    {
        if(cancellation.IsCancellationRequested) return;
        await Playlist.InvokeVoidAsync("setTrackVolumeById", trackId, volume);
    }


    public async Task ToggleSolo(Track track)
    {
        await Playlist.InvokeVoidAsync("toggleTrackSoloById", track.Id);
        track.IsSolo = !track.IsSolo;
    }

    public async Task ToggleMute(Track track)
    {
        await Playlist.InvokeVoidAsync("toggleTrackMuteById", track.Id);
        track.IsMuted = !track.IsMuted;
    }


    public Task Play() => EmitEvent(PlaylistEventsJs.PLAY).AsTask();
    public Task Pause() => EmitEvent(PlaylistEventsJs.PAUSE).AsTask();
    public Task Stop() => EmitEvent(PlaylistEventsJs.STOP).AsTask();
    public Task Rewind() => EmitEvent(PlaylistEventsJs.REWIND).AsTask();
    public Task FastForward() => EmitEvent(PlaylistEventsJs.FAST_FORWARD).AsTask();
    public Task ZoomIn() => EmitEvent(PlaylistEventsJs.ZOOM_IN).AsTask();
    public Task ZoomOut() => EmitEvent(PlaylistEventsJs.ZOOM_OUT).AsTask();
    public Task SetCursorState() => EmitEvent(PlaylistEventsJs.STATE_CHANGE, "cursor").AsTask();
    public Task SetSelectState() => EmitEvent(PlaylistEventsJs.STATE_CHANGE, "select").AsTask();
    public Task SetShiftState() => EmitEvent(PlaylistEventsJs.STATE_CHANGE, "shift").AsTask();



    public ValueTask EmitEvent(params string[] emitParams) => Ee.InvokeVoidAsync("emit", emitParams);


    [JSInvokable]
    public async Task OnShift(long trackId, float newStartTime) {
        if(_daw is null) return;
        await _daw.SetTrackStartTime(trackId, newStartTime);
    }

    [JSInvokable]
    public async Task OnVolumeChange(long trackId, float newVolume) { }


    public async ValueTask DisposeAsync()
    {
        if (_playlist != null) await _playlist.DisposeAsync();
        if (_ee != null) await _ee.DisposeAsync();
        _dawInteropJsRef.Dispose();
    }


    public record TrackJs
    {
        public long id { get; set; }
        public string name { get; set; } = string.Empty;
        public string? src { get; set; } = null;
        public float gain { get; set; }
        public float start { get; set; }
        public bool locked { get; set; } = true;
        public bool muted { get; set; } = false;
        public bool soloed { get; set; } = false;

        public string customClass { get; set; } = string.Empty;
        public string waveOutlineColor { get; set; } = string.Empty;

        public static TrackJs Create(Track track)
            => new TrackJs
            {
                id = track.Id,
                name = track.Name,
                src = DawService.GetTrackSourceUrl(track),
                gain = track.Volume,
                start = track.StartTime,
                locked = track.SelectionState is not TrackSelectionState.Self,
                muted = track.IsMuted,
                soloed = track.IsSolo,

                waveOutlineColor = DawTrackColors.Background,
                customClass = track.SelectionState switch
                {
                    TrackSelectionState.Self => "daw-track-selected-self",
                    TrackSelectionState.Other => "daw-track-selected-other",
                    TrackSelectionState.None => "daw-track-selected-none",
                    _ => string.Empty
                }
            };
    }

    public record PlaylistOptionsJs
    {
        public int samplesPerPixel { get; set; } = 3000;
        public int waveHeight { get; set; } = 162;

        public bool mono { get; set; } = true;

        public PlaylistColorsJs colors { get; set; } = new();

        public bool timescale { get; set; } = true;

        public string seekStyle { get; set; } = "line";

        public int[] zoomLevels { get; set; } = new[] { 100, 250, 500, 1000, 3000, 5000 };
    }

    public record PlaylistColorsJs
    {
        public string waveOutlineColor { get; set; } = "#005BBB";
        public string timeColor { get; set; } = "grey";
        public string fadeColor { get; set; } = "black";
    }


    public static class PlaylistEventsJs
    {
        public const string PLAY = "play";
        public const string PAUSE = "pause";
        public const string STOP = "stop";
        public const string REWIND = "rewind";
        public const string FAST_FORWARD = "fastforward";
        public const string ZOOM_IN = "zoomin";
        public const string ZOOM_OUT = "zoomout";
        public const string STATE_CHANGE = "statechange";
        public const string VOLUME_CHANGE = "volumechange";
    }

}

public static class DawTrackColors
{
    public const string Background = "#c0dce0";

    public static class Selection
    {
        public const string Self = "#f5a831";
        public const string SelfProgress = "#ffd632";
        public const string Other = "#782391";
        public const string OtherProgress = "#521763";
        public const string None = "#808080";
        public const string NoneProgress = "#5c5c5c";
    }
}
