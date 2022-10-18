namespace Concerto.Server.Extensions;

public static class EnvironmentHelper
{
	public static string GetVariable(string variableName)
	{
		string? variable = Environment.GetEnvironmentVariable(variableName);
		if (variable == null)
		{
			throw new KeyNotFoundException($"Environment variable {variableName} not found");
		}

		return variable;
	}
}
