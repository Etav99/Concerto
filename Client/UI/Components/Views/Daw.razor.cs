using Concerto.Client.Extensions;
using Concerto.Client.Services;
using Concerto.Client.UI.Layout;
using Concerto.Shared.Constants;
using Concerto.Shared.Models.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;

namespace Concerto.Client.UI.Components.Views;

public partial class Daw : IAsyncDisposable
{
    [Inject] IJSRuntime JS { get; set; } = null!;
    [Inject] DawService DawService { get; set; } = null!;
    [Inject] NavigationManager Navigation { get; set; } = null!;
    [Inject] IDialogService DialogService { get; set; } = null!;

    [CascadingParameter] LayoutState LayoutState { get; set; } = new();

    [Parameter] public EventCallback<string> OnListenTogether { get; set; }
    [Parameter] public EventCallback OnRequestStopSharing { get; set; }

    public const string dawId = "daw";

    bool DawInitialized { get; set; } = false;
    DawInterop DawInterop { get; set; } = null!;

    private bool _selectActive = true;
    private bool _shiftActive = false;

    private HubConnection? _dawHub = null;
    private HubConnection DawHub
    {
        get => _dawHub ?? throw new NullReferenceException("DawHub is not initialized");
        set => _dawHub = value;
    }

    [Parameter]
    public long? SessionId { get; set; }
    private long _sessionId;

    [Parameter]
    public long? CourseId { get; set; }

    private Track? UploadingTrack { get; set; }

    private Task _updateProjectTask = Task.CompletedTask;
    private CancellationTokenSource _updateProjectCancellationTokenSource = new();

    private DawProject? _project;
    private DawProject Project
    {
        get => _project ?? throw new NullReferenceException("Project is not initialized");
        set => _project = value;
    }
    public Guid ProjectToken => Project.Token;

    private bool ShouldUpdate {get; set; }

    private bool IsPlaying { get; set; } = false;
    private bool IsRecording { get; set; } = false;
    private bool IsRecordingPending { get; set; } = false;
    private bool ListenTogetherPending { get; set; } = false;
    private TrackRecording? TrackRecording { get; set; }


    protected override void OnInitialized()
    {
        DawInterop = new(this);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (SessionId == _sessionId || SessionId is null) return;
        _sessionId = SessionId.Value;

        if (_dawHub != null) await _dawHub.DisposeAsync();
        DawHub = DawService.CreateHubConnection();
        await DawHub.StartAsync();
        DawHub.On<long>(DawHubMethods.Client.OnProjectChanged, OnProjectChanged);
        DawHub.On(DawHubMethods.Client.OnRequestStopSharingVideo, OnRequestStopSharing.InvokeAsync);

        await DawHub.InvokeAsync(DawHubMethods.Server.JoinDawProject, _sessionId);

        if (!DawInitialized)
            await DawInterop.Initialize(JS, dawId);
        else
            await DawInterop.ClearProject();

        Project = await DawService.GetProjectAsync(_sessionId);
        await DawInterop.LoadProject(Project);

        if (!DawInitialized)
            DawInitialized = true;
    }

    public async Task OnProjectChanged(long sessionId)
    {
        if(sessionId != _sessionId) return;

        if(IsPlaying || IsRecording || IsRecordingPending)
            ShouldUpdate = true;
        else
            await UpdateProject();
    }

    public async Task UpdateProject()
    {
        ShouldUpdate = false;

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

        _updateProjectTask = UpdateProjectTask(_updateProjectCancellationTokenSource.Token);

        try
        {
            await _updateProjectTask;
        }
        catch (OperationCanceledException) { }
    }

    public async Task UpdateProjectTask(CancellationToken cancellation)
    {
        var oldProjectState = Project;
        DawProject newProjectState;
        try
        {
            newProjectState = await DawService.GetProjectAsync(_sessionId, cancellation);
        }
        catch (OperationCanceledException) { throw; }


        bool shouldRestartPlay = false;
        foreach (var newTrack in newProjectState.Tracks)
        {
            var oldTrack = oldProjectState.Tracks.FirstOrDefault(t => t.Id == newTrack.Id);
            if (oldTrack != null) oldProjectState.Tracks.Remove(oldTrack);

            if (oldTrack is null)
            {
                await DawInterop.AddTrack(newTrack);
            }
            else
            {
                if(oldTrack.ShouldBeUpdated(newTrack))
                {
                    shouldRestartPlay = shouldRestartPlay || oldTrack.ShouldBeRestarted(newTrack);
                    newTrack.CopyLocalState(oldTrack);
                    await DawInterop.UpdateTrack(newTrack, oldTrack.SourceId != newTrack.SourceId);
                }
            }
        }

        foreach (var track in oldProjectState.Tracks)
            await DawInterop.RemoveTrack(track);

        if (newProjectState.Tracks.Any())
            await DawInterop.ReorderTracks(newProjectState.Tracks.Select(t => t.Id));

        if (shouldRestartPlay)
            await DawInterop.RestartPlay();

        await DawInterop.ReRender();

        Project = newProjectState;
        StateHasChanged();
    }

    public async Task Play()
    {
        if(IsRecording) return;
        IsPlaying = true;
        await DawInterop.Play();
    }

    public async Task Stop()
    {
        IsPlaying = false;
        await DawInterop.Stop();

        if(ShouldUpdate) await UpdateProject();
    }

    [JSInvokable]
    public async Task OnPlayingFinished()
    {
        IsPlaying = false;
        if(ShouldUpdate) await UpdateProject();
        StateHasChanged();
    }
    private async Task StartRecording(Track track)
    {
        if (IsRecording) return;
        IsRecording = true;
        TrackRecording = new TrackRecording(track);
        await DawInterop.RecordTrack(track);
    }

    private async Task DiscardRecording()
    {
        if (!IsRecordingPending) return;
        if (TrackRecording is null) throw new InvalidOperationException("TrackRecording is null");

        var originalTrack = Project.TracksById[TrackRecording.Id];
        await DawInterop.UpdateTrack(originalTrack, true);
        await DawInterop.ReRender();

        TrackRecording = null;
        IsRecordingPending = false;

        if(ShouldUpdate) await UpdateProject();
    }

    private async Task AcceptRecording()
    {
        if (!IsRecordingPending) return;
        if (TrackRecording is null) throw new InvalidOperationException("TrackRecording is null");
        if (TrackRecording.Blob is null) throw new InvalidOperationException("TrackRecording.Blob is null");


        Project.TracksById[TrackRecording.Id].StartTime = TrackRecording.StartTime;
        await DawService.SetTrackSourceAsync(_sessionId, TrackRecording.Id, TrackRecording.Blob, TrackRecording.StartTime, TrackRecording.Volume);

        TrackRecording = null;
        IsRecordingPending = false;

        if(ShouldUpdate) await UpdateProject();
    }

    [JSInvokable]
    public void OnRecordingFinished(long trackId, IJSStreamReference blob, float startTime)
    {
        if (!IsRecording || TrackRecording is null)
            throw new InvalidOperationException("Recording finished but no recording was started");

        if (TrackRecording.Id != trackId)
            throw new InvalidOperationException("Recording finished but for different track");



        TrackRecording.Blob = blob;
        TrackRecording.StartTime = startTime;
        IsRecordingPending = true;
        IsRecording = false;

        StateHasChanged();
    }

    private async Task AddTrack()
    {
        await DawService.AddTrackAsync(_sessionId, string.Empty);
    }

    private async Task SetTrackName(Track track, string name)
    {
        await DawService.SetTrackNameAsync(_sessionId, track.Id, name);
    }

    private async Task UploadTrackSource(Track track, IBrowserFile file)
    {
        UploadingTrack = track;
        await DawService.SetTrackSourceAsync(_sessionId, track.Id, file, 1);
        UploadingTrack = null;
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
        if(track.Id == TrackRecording?.Id)
        {
            TrackRecording.Volume = volume;
            return;
        }
        track.Volume = volume;
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

    [JSInvokable]
    public async Task OnShift(long trackId, float newStartTime) {
        if(trackId == TrackRecording?.Id)
        {
            TrackRecording.StartTime = newStartTime;
            return;
        }
        Project.TracksById[trackId].StartTime = newStartTime;
        await SetTrackStartTime(trackId, newStartTime);
    }

    public async Task SetSelectState()
    {
        await DawInterop.SetSelectState();
        _selectActive = true;
        _shiftActive = false;
    }

    public async Task SetShiftState()
    {
        await DawInterop.SetShiftState();
        _selectActive = false;
        _shiftActive = true;
    }

    private bool ListenTogetherDisabled => IsRecording || IsRecordingPending || ListenTogetherPending;
    public async Task ListenTogether()
    {
        if (ListenTogetherDisabled) return;

        ListenTogetherPending = true;
        await DawHub.InvokeAsync(DawHubMethods.Server.RequestStopSharingVideo, _sessionId);
        await DawService.GenerateProjectSourceAsync(_sessionId);
        await Task.Delay(200);

        var url = await DawService.GetProjectSourceUrl(_sessionId);
        url = Navigation.ToAbsoluteUri(url).ToString();
        await OnListenTogether.InvokeAsync(url);
        ListenTogetherPending = false;
    }

    private bool SaveProjectOutputDisabled => IsRecording || IsRecordingPending || ListenTogetherPending || !(_project?.Tracks.Any(t => t.SourceId is not null) ?? true);
    public async Task SaveProjectOutput()
    {
        if(SaveProjectOutputDisabled) return;
        var name = await DialogService.ShowInputStringDialog("Input file name", "File name");
        if (name is null) return;
        var folder = await DialogService.ShowSelectFolderDialog("Select folder", "Save here", CourseId);
        if (folder is null) return;
        await DawService.GenerateProjectSourceAsync(_sessionId);
        await DawService.SaveProjectSourceAsync(_sessionId, folder.Id, name);
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
    private DotNetObjectReference<Daw> _dawJsRef;
    private Daw _daw;

    private Task _setVolumeTask = Task.CompletedTask;
    private CancellationTokenSource _setVolumeCancellationTokenSource = new();

    public DawInterop(Daw daw)
    {
        _daw = daw;
        _dawJsRef = DotNetObjectReference.Create(_daw);
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


    private TrackJs CreateTrackJs(Track track) => TrackJs.Create(track, _daw.ProjectToken);

    public async Task Initialize(IJSRuntime js, string dawId)
    {
        _playlist = await js.InvokeAsync<IJSObjectReference>("initializeDaw", dawId, new PlaylistOptionsJs(), _dawJsRef);
        _ee = await _playlist.InvokeAsync<IJSObjectReference>("getEventEmitter");
    }

    public async Task LoadProject(DawProject project)
    {
        var tracks = project.Tracks.Select(CreateTrackJs);
        await Playlist.InvokeVoidAsync("loadTrackList", tracks);
    }

    public async Task ClearProject()
    {
        await Playlist.InvokeVoidAsync("clearTrackList");
    }

    public async Task AddTrack(Track track)
    {
        await Playlist.InvokeVoidAsync("addTrack", CreateTrackJs(track));
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
        await Playlist.InvokeVoidAsync("updateTrack", CreateTrackJs(track), sourceChanged);
    }
    public async Task RecordTrack(Track track)
    {
        await Playlist.InvokeVoidAsync("startRecordingTrackById", track.Id);
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

    public async Task RestartPlay()
    {
        await Playlist.InvokeVoidAsync("restartPlay");
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

    public async ValueTask DisposeAsync()
    {
        if (_playlist != null) await _playlist.DisposeAsync();
        if (_ee != null) await _ee.DisposeAsync();
        _dawInteropJsRef.Dispose();
        _dawJsRef.Dispose();
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

        public static TrackJs Create(Track track, Guid token)
            => new TrackJs
            {
                id = track.Id,
                name = track.Name,
                src = DawService.GetTrackSourceUrl(track, token),
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

        public int barWidth { get; set; } = 1;

        public bool mono { get; set; } = true;

        public PlaylistColorsJs colors { get; set; } = new();

        public bool timescale { get; set; } = true;

        public string seekStyle { get; set; } = "line";
        public string state {get; set;} = "select";

        public bool isAutomaticScroll {get; set; } = true;

        public int[] zoomLevels { get; set; } = new[] { 100, 250, 500, 1000, 3000, 5000 };
    }

    public record PlaylistColorsJs
    {
        public string waveOutlineColor { get; set; } = "var(--wp-tracks-container-background-color)";
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


public record TrackRecording
{
    public long Id { get; set; }
    public IJSStreamReference? Blob { get; set; }
    public float StartTime { get; set; }
    public float Volume { get; set; }

    public TrackRecording(Track track)
    {
        Id = track.Id;
        StartTime = track.StartTime;
    }
}
