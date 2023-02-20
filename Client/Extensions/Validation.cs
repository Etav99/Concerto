namespace Concerto.Client.Extensions;

public static class Validation
{
    public static Func<string, string?> NotEmpty = (string str) =>
    {
        return string.IsNullOrWhiteSpace(str) ? "Cannot be empty" : null;
    };

}



