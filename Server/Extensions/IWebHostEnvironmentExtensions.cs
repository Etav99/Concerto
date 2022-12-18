using Concerto.Server.Settings;

namespace Concerto.Server.Extensions;

public static class IWebHostEnvironmentExtensions
{
	public static bool IsDocker(this IWebHostEnvironment environment)
	{
		return AppSettings.Environment.Docker.Equals("true");
	}

	public static bool IsRemote(this IWebHostEnvironment environment)
	{
		return AppSettings.Environment.Remote.Equals("true");
	}
}

