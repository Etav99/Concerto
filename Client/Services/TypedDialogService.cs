using Concerto.Client.Components.Dialogs;
using Concerto.Shared.Models.Dto;
using MudBlazor;

namespace Concerto.Client.Services
{
	public static class DialogServiceExtensions
	{
		public static async Task <bool> ShowDeleteConfirmationDialog(this IDialogService dialogService, string title, string itemType, string itemName, bool additionalConfirmation = false)
		{
			var parameters = new DialogParameters { ["ItemName"] = $"{itemType} {itemName}", ["Confirmation"] = additionalConfirmation };
			var name = title;
			DialogResult result = await dialogService.Show<DeleteConfirmationDialog>(name, parameters).Result;
			if (result.Cancelled) return false;
			return true;
		}


	}
}
