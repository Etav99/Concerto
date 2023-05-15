using Concerto.Client.Services;
using Concerto.Client.UI.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Web;

namespace Concerto.Client.UI.Components.Views;

public partial class Meeting : IDisposable
{
    [Inject] IJSRuntime JS { get; set; } = null!;
    [Inject] IAppSettingsService AppSettingsService { get; set; } = null!;
    [Inject] ISessionService SessionService { get; set; } = null!;


    [CascadingParameter]
    public LayoutState LayoutState { get; set; } = LayoutState.Default;

    [Parameter]
    public string? MeetingName { get; set; }
    [Parameter]
    public Guid? Guid { get; set; }
    string _meetingPath = string.Empty;
    CancellationTokenSource startMeetingCancellation = new();

    DotNetObjectReference<Meeting> _dotNetObjectReference = null!;

    private bool _dawInitialized = false;
    private bool _dawVisible = false;

    bool _disposed = false;
    bool _loading = true;



    private const string StyleBase = "display: grid; height: 100%; max-height: 100%;";
    private string Style => _dawVisible
                            ? $"{StyleBase} grid-template-columns: 1fr min-content 1fr;"
                            : $"{StyleBase} grid-template-columns: 1fr min-content min-content;";

    private string DawStyle => _dawVisible ? "" : "display: none;";

    private void ToggleDaw()
    {
        if (!_dawInitialized) _dawInitialized = true;
        _dawVisible = !_dawVisible;
    }

    protected override void OnInitialized()
    {
        _dotNetObjectReference = DotNetObjectReference.Create(this);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (MeetingName == null || Guid == null) return;
        var guid = Guid.ToString();
        var meetingPath = HttpUtility.UrlPathEncode($"{guid}");
        if (meetingPath == _meetingPath) return;
        _meetingPath = HttpUtility.UrlPathEncode($"{guid}");
        //if (LayoutState.IsMobile) return;
        await StartMeeting();
    }

    [JSInvokable]
    public async Task StartMeeting()
    {
        if (_disposed || string.IsNullOrEmpty(_meetingPath)) return;
        _loading = true;
        StateHasChanged();
        var token = await SessionService.GetMeetingTokenAsync(Guid, startMeetingCancellation.Token);
        try
        {
            await JS.InvokeAsync<string>("startMeeting", startMeetingCancellation.Token, "jitsi", AppSettingsService.AppSettings.JitsiUrl.Host, _meetingPath, token, _dotNetObjectReference);
        }
        catch (ObjectDisposedException) { }
        _loading = false;
    }


    public void Dispose()
    {
        startMeetingCancellation.Cancel();
        _dotNetObjectReference?.Dispose();
        _disposed = true;
    }
}