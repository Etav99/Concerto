using Microsoft.Extensions.Caching.Memory;

namespace Concerto.Server.Services;

public class OneTimeTokenStore
{
	public MemoryCache Tokens { get; } = new MemoryCache(
		new MemoryCacheOptions
		{
			SizeLimit = 100_000
		}
	);
}
