using Concerto.Shared.Models.Dto;
using System.Reflection.Metadata.Ecma335;

namespace Concerto.Client.Services;

public interface IAppSettingsService : IAppSettingsClient
{ 
	ClientAppSettings AppSettings {get; }
}

public class AppSettingsService : AppSettingsClient, IAppSettingsService
{

	private ClientAppSettings? _appSettings;
	public ClientAppSettings AppSettings
	{
		get
		{
			if (_appSettings is null) throw new NullReferenceException("AppSettings not fetched");
			return _appSettings;
		}
	}

	public AppSettingsService(HttpClient httpClient) : base(httpClient) { }

	public async Task FetchAppSettings()
	{
		_appSettings ??= await GetClientAppSettingsAsync();
	}

}
