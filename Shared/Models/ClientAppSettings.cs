namespace Concerto.Shared.Models.Dto;

public class ClientAppSettings
{
	public string AuthorityUrl { get; set; } = string.Empty;
	public string AccountManagementUrl { get; set; } = string.Empty;
	public string PostLogoutUrl { get; set; } = string.Empty;

	public long FileSizeLimit {get; set; }
	public int MaxAllowedFiles {get; set; }
}



