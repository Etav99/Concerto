using System.Net;

namespace Concerto.Client.Extensions;

public static class HttpExtensions
{
    public static void EnsureSuccessOrNotFoundStatusCode(this HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.NotFound)
            response.EnsureSuccessStatusCode();
    }
}
