using MudBlazor.Charts.SVG.Models;

namespace Concerto.Client.Extensions;
public static class CustomIcons
{
    private static string SvgTag(params string[] content)
    {
        return $"<svg style=\"width:24px;height:24px\" viewBox=\"0 0 16 16\">{string.Join("", content)}</svg>";
    }

    public static string CursorText { get; } = "fa-solid fa-i-cursor fa-force-size";
    public static string Shift { get; } = "fa-solid fa-arrows-alt-h";
    public static string Share { get; } = "fa-solid fa-share";

    public static string Play { get; } = "fa-solid fa-play";
    public static string Pause { get; } = "fa-solid fa-pause";
    public static string Stop { get; } = "fa-solid fa-stop";

    public static string Record { get; } = "fa-solid fa-circle";
    public static string RecordStop { get; } = "fa-solid fa-stop-circle";

    public static string Mic { get; } = "fa-solid fa-microphone";
    public static string MicSlash { get; } = "fa-solid fa-microphone-slash";

    public static string Headphones { get; } = "fa-solid fa-headphones";

    public static string Volume { get; } = "fa-solid fa-volume";

    public static string Backward { get; } = "fa-solid fa-backward";
    public static string Forward { get; } = "fa-solid fa-forward";

    public static string FastBackward { get; } = "fa-solid fa-backward-fast";
    public static string FastForward { get; } = "fa-solid fa-forward-fast";

    public static string ZoomIn { get; } = "fa-solid fa-magnifying-glass-plus";
    public static string ZoomOut { get; } = "fa-solid fa-magnifying-glass-minus";

    public static string CloudArrowUp { get; } = "fa-solid fa-cloud-arrow-up";

    public static string CloudArrowDown { get; } = "fa-solid fa-cloud-arrow-down";
}