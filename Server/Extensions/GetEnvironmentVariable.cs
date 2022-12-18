namespace Concerto.Server.Extensions;

public static class EnvironmentHelper
{
	public static string GetVariable(string variableName)
	{
		var variable = Environment.GetEnvironmentVariable(variableName);
		if (variable == null) return string.Empty;

		return variable;
	}
}

