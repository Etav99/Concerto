using Microsoft.Extensions.Caching.Memory;

namespace Concerto.Server.Services;

public class TokenStore
{
	public MemoryCache Tokens { get; } = new MemoryCache(
		new MemoryCacheOptions
		{
			SizeLimit = 100_000
		}
	);

	public static MemoryCacheEntryOptions DefaultEntryOptions = new()
	{
		Size = 1,
		SlidingExpiration = TimeSpan.FromMinutes(5),
	};
	public Guid GenerateToken(long resourceId, TokenType tokenType)
	{
		var token = Guid.NewGuid();
		Tokens.Set(token, (resourceId, tokenType), DefaultEntryOptions);
		return token;
	}

	internal bool ValidateToken(Guid token, long resourceId, TokenType tokenType)
	{
		Tokens.TryGetValue(token, out (long, TokenType) val);
		if (val.Item1 != resourceId || val.Item2 != tokenType) return false;
		return true;
	}

	public enum TokenType
	{
		File,
		DawProject
	}

}
