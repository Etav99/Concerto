using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Concerto.Client.UI.Components.Views;

public partial class Daw : IAsyncDisposable
{
    [Inject] IJSRuntime JS { get; set; } = null!;

    public const string DawId = "daw";

    IJSObjectReference? _daw;

    private List<Track> Tracks {get; set;} = new List<Track>();

    protected override async Task OnInitializedAsync()
    {
        _daw = await JS.InvokeAsync<IJSObjectReference>("initializeDaw", DawId);

        Tracks = new List<Track>
        {
            new Track("Track 1", "assets/BassDrums30.mp3", 0),
            new Track("Track 2", "assets/Guitar30.mp3", 4),
        };

    }

    public async Task UpdateTrack(Track track)
    {
        if (_daw == null) return;
        await _daw.InvokeVoidAsync("updateTrack", track);
    }

    public async Task Play()
    {
        if (_daw == null) return;
        await _daw.InvokeVoidAsync("play");
    }
    public async Task Pause()
    {
        if (_daw == null) return;
        await _daw.InvokeVoidAsync("pause");
    }
    public async Task Stop()
    {
        if (_daw == null) return;
        await _daw.InvokeVoidAsync("stop");
    }
    public async Task Rewind()
    {
        if (_daw == null) return;
        await _daw.InvokeVoidAsync("rewind");
    }
    public async Task FastForward()
    {
        if (_daw == null) return;
        await _daw.InvokeVoidAsync("fastForward");
    }
    public async Task ZoomIn()
    {
        if (_daw == null) return;
        await _daw.InvokeVoidAsync("zoomIn");
    }
    public async Task ZoomOut()
    {
        if (_daw == null) return;
        await _daw.InvokeVoidAsync("zoomOut");
    }
    public async Task SetCursorState()
    {
        if (_daw == null) return;
        await _daw.InvokeVoidAsync("setCursorState");
    }

    public async Task SetSelectState()
    {
        if (_daw == null) return;
        await _daw.InvokeVoidAsync("setSelectState");
    }

    public async Task SetShiftState()
    {
        if (_daw == null) return;
        await _daw.InvokeVoidAsync("setShiftState");
    }



    public async ValueTask DisposeAsync()
    {
        if(_daw != null) await _daw.DisposeAsync();
    }
}


public record Track(string Name, string Source, float startTime);
