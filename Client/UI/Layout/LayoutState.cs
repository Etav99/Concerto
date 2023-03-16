using MudBlazor;

namespace Concerto.Client.UI.Layout;

public record LayoutState
{
    public LayoutState()
    {
        Breakpoint = Breakpoint.Always;
        DarkMode = false;
    }

    public bool IsMobile { get; set; } = false;
    public bool IsIos { get; set; } = false;

    public Breakpoint Breakpoint { get; set; }
    public bool DarkMode { get; set; }

    public bool Xs => Breakpoint == Breakpoint.Xs;
    public bool Sm => Breakpoint == Breakpoint.Sm;
    public bool Md => Breakpoint == Breakpoint.Md;
    public bool Lg => Breakpoint == Breakpoint.Lg;
    public bool Xl => Breakpoint == Breakpoint.Xl;
    public bool Xxl => Breakpoint == Breakpoint.Xxl;
    public bool SmAndDown => Breakpoint <= Breakpoint.Sm;
    public bool SmAndUp => Breakpoint >= Breakpoint.Sm;
    public bool MdAndDown => Breakpoint <= Breakpoint.Md;
    public bool MdAndUp => Breakpoint >= Breakpoint.Md;
    public bool LgAndDown => Breakpoint <= Breakpoint.Lg;
    public bool LgAndUp => Breakpoint >= Breakpoint.Lg;
    public bool XlAndDown => Breakpoint <= Breakpoint.Xl;
    public bool XlAndUp => Breakpoint >= Breakpoint.Xl;
    public static LayoutState Default => new();
}
