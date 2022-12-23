using Concerto.Client.Extensions;
using Concerto.Shared.Models.Dto;
using MudBlazor;

namespace Concerto.Client.Extensions;

public static class Validation
{
	public static string? NotEmpty(string str)
	{
		if (string.IsNullOrWhiteSpace(str)) return "Cannot be empty";
		return null;
	}
	
}



