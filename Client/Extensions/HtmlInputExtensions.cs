using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace Concerto.Client.Extensions;

public static class HtmlInputExtensions
{

    public static float GetFloatValue(this ChangeEventArgs args)
    {
        return float.Parse(args.Value?.ToString() ?? "0", CultureInfo.InvariantCulture.NumberFormat);
    }
    public static int GetIntValue(this ChangeEventArgs args)
    {
        return int.Parse(args.Value?.ToString() ?? "0");
    }
    public static string GetStringValue(this ChangeEventArgs args)
    {
        return args.Value?.ToString() ?? string.Empty;
    }

}
